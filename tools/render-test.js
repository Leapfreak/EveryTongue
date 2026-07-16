/* Client-render test: serve the REAL wwwroot with index.html patched to stub
   WebSocket, open it in headless Edge as a room client, feed scripted
   welcome/roomStatus/update/commit messages, and dump the DOM to verify the
   commit text actually renders into #lines. Isolates "server sent it" from
   "client displayed it".
     node tools/render-test.js */
const fs = require("fs");
const path = require("path");
const http = require("http");
const { execFileSync } = require("child_process");

const ROOT = path.join(__dirname, "..", "EveryTongue.Core", "wwwroot");
const PORT = 5980;

const STUB = `<script>
/* Fake WS: deliver a scripted room session to the real client code. */
(function(){
  function FakeWS(url){
    var self=this;
    self.url=url; self.readyState=0; self.sent=[];
    setTimeout(function(){
      self.readyState=1;
      if(self.onopen)self.onopen();
      function deliver(obj,delay){setTimeout(function(){
        if(self.onmessage)self.onmessage({data:JSON.stringify(obj)});},delay)}
      deliver({type:'welcome',clientId:'render-test-client'},50);
      deliver({type:'roomStatus',scope:'stt',state:'ready'},100);
      deliver({type:'update',text:'PARTIAL_TEXT_MARKER'},300);
      deliver({type:'commit',id:1,text:'HELLO_SUBTITLE_MARKER',lang:'en',sourceLang:'eng_Latn',time:'12:00:00'},600);
      deliver({type:'commit',id:2,text:'SECOND_SUBTITLE_MARKER',lang:'en',sourceLang:'eng_Latn',time:'12:00:05'},900);
    },30);
    self.send=function(d){self.sent.push(d)};
    self.close=function(){self.readyState=3};
  }
  window.__realWS=window.WebSocket;
  window.WebSocket=FakeWS;
  window.__jsErrors=[];
  window.addEventListener('error',function(e){window.__jsErrors.push(String(e.message)+' @'+e.filename+':'+e.lineno)});
  /* phone results home — headless dump-dom is flaky, a beacon isn't */
  setTimeout(function(){
    var lines=document.getElementById('lines');
    /* Is the caption area actually VISIBLE, or is an overlay (picker) on top? */
    function coversCenter(el){
      if(!el)return false;
      var s=getComputedStyle(el);
      if(s.display==='none'||s.visibility==='hidden'||parseFloat(s.opacity)===0)return false;
      var r=el.getBoundingClientRect();
      return r.width>window.innerWidth*0.5 && r.height>window.innerHeight*0.5;
    }
    var picker=document.getElementById('langPicker');
    var elAtCenter=document.elementFromPoint(window.innerWidth/2,window.innerHeight/2);
    var report={
      errors:window.__jsErrors,
      linesHtml:lines?lines.textContent.slice(0,400):'(no #lines)',
      hasCommit1:document.body.textContent.indexOf('HELLO_SUBTITLE_MARKER')>=0,
      hasCommit2:document.body.textContent.indexOf('SECOND_SUBTITLE_MARKER')>=0,
      hasPartial:document.body.textContent.indexOf('PARTIAL_TEXT_MARKER')>=0,
      pickerOpen:picker?picker.classList.contains('open'):false,
      pickerCovers:coversCenter(picker),
      centerElement:elAtCenter?(elAtCenter.id||elAtCenter.className||elAtCenter.tagName):'(none)',
      capBadgeText:(document.getElementById('capBadge')||{}).textContent||'(no badge)',
      /* The doubled-div bug: text present in the DOM but nested inside a
         display:none ancestor → zero-size rect. Assert PHYSICAL visibility. */
      lastLineVisible:(function(){
        if(!lines||lines.children.length<2)return false;
        var r=lines.children[lines.children.length-1].getBoundingClientRect();
        return r.width>0&&r.height>0&&r.bottom>0&&r.top<window.innerHeight;
      })(),
      containerParent:(function(){
        var c=document.getElementById('container');
        return c&&c.parentElement?(c.parentElement.id||c.parentElement.tagName):'(none)';
      })()
    };
    fetch('/report',{method:'POST',body:JSON.stringify(report)});
  },3000);
})();
</script>`;

let reportResolve;
const reportPromise = new Promise(r => { reportResolve = r; });

const server = http.createServer((req, res) => {
    let urlPath = req.url.split("?")[0];
    if (urlPath === "/") urlPath = "/index.html";
    if (urlPath === "/report") {
        let b = "";
        req.on("data", c => b += c);
        req.on("end", () => { res.end("ok"); reportResolve(JSON.parse(b)); });
        return;
    }
    const file = path.join(ROOT, urlPath.replace(/^\//, "").replace(/\.\./g, ""));
    // Minimal API stubs the page fetches at load
    if (urlPath.startsWith("/api/")) {
        res.setHeader("Content-Type", "application/json");
        if (urlPath === "/api/config") { res.end(JSON.stringify({ hasAdminPin: true, hasLiveSession: false, publicHost: "" })); return; }
        if (urlPath === "/api/languages") { res.end(JSON.stringify([["eng_Latn", "English", "English", "en"], ["spa_Latn", "Spanish", "Español", "es"]])); return; }
        if (urlPath === "/api/locale") { res.end("{}"); return; }
        if (urlPath.match(/^\/api\/rooms\/[^/]+$/)) { res.end(JSON.stringify({ id: "test", name: "Render Test", type: "conference", isHost: false, audioSource: "web", mode: "online", display: null, members: [] })); return; }
        res.statusCode = 404; res.end("{}"); return;
    }
    fs.readFile(file, (err, data) => {
        if (err) { res.statusCode = 404; res.end("nf"); return; }
        if (urlPath === "/index.html") {
            // inject the WS stub BEFORE any script runs
            data = Buffer.from(String(data).replace("<script", STUB + "\n<script"));
        }
        const ext = path.extname(file);
        res.setHeader("Content-Type", ext === ".js" ? "text/javascript" : ext === ".css" ? "text/css" : "text/html");
        res.end(data);
    });
});

server.listen(PORT, async () => {
    console.log("serving on :" + PORT);
    const { spawn } = require("child_process");
    const edge = spawn(
        "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
        ["--headless=new", "--disable-gpu", "--no-first-run", "--mute-audio",
            "--user-data-dir=" + path.join(process.env.TEMP, "et-render-test-profile"),
            "http://localhost:" + PORT + "/index.html?room=test"],
        { stdio: "ignore" });
    const timeout = new Promise(r => setTimeout(() => r(null), 30000));
    const report = await Promise.race([reportPromise, timeout]);
    try { edge.kill(); } catch (e) { }
    if (!report) { console.log("NO REPORT — page never ran the beacon"); process.exit(1); }
    console.log("js errors:", report.errors.length ? report.errors.join(" | ") : "(none)");
    console.log("commit 1 rendered:", report.hasCommit1);
    console.log("commit 2 rendered:", report.hasCommit2);
    console.log("partial rendered:", report.hasPartial);
    console.log("#lines content:", JSON.stringify(report.linesHtml));
    console.log("picker open:", report.pickerOpen, "| picker covers screen:", report.pickerCovers);
    console.log("element at screen center:", report.centerElement);
    console.log("caption badge:", JSON.stringify(report.capBadgeText), "(expect '● 2' — 2 commits delivered)");
    console.log("container parent:", report.containerParent, "(must be BODY)");
    console.log("last line physically visible:", report.lastLineVisible);
    const pass = report.hasCommit1 && report.hasCommit2 && report.lastLineVisible && report.containerParent === "BODY";
    console.log("\nVERDICT:", pass ? "CAPTIONS RENDER AND ARE VISIBLE" : "FAILURE — in DOM but NOT visible (or missing)");
    process.exitCode = pass ? 0 : 1;
    server.close();
    process.exit(0);
});
