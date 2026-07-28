/* Web-mic end-to-end: drive the REAL client in headless Edge with a FAKE
   MICROPHONE fed from a WAV of Sunday's sermon (Catalan). The driver seeds the
   host token, picks Spanish output, clicks Broadcast, and reports what
   rendered. Server-side proof lands in the Lite log (broadcaster started,
   Speechmatics commits).
     node tools/verify-webmic.js <roomId> <hostToken> */
const http = require("http");
const path = require("path");
const fs = require("fs");
const { spawn, execSync } = require("child_process");

const ROOM = process.argv[2], TOKEN = process.argv[3];
if (!ROOM || !TOKEN) { console.error("usage: node verify-webmic.js <roomId> <hostToken>"); process.exit(2); }
const LITE = { host: "localhost", port: 5981 };
const PROXY_PORT = 5985;
const EDGE = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const PROFILE = path.join(process.env.TEMP, "et-webmic-profile");
const WAV = "C:\\tmp\\et-verify\\voice.wav";

let reports = [];
const proxy = http.createServer((req, res) => {
    if (req.url === "/__report") {
        let b = ""; req.on("data", c => b += c);
        req.on("end", () => { res.end("ok"); try { reports.push(JSON.parse(b)); } catch (e) { } });
        return;
    }
    const opts = { host: LITE.host, port: LITE.port, path: req.url, method: req.method, headers: { ...req.headers, host: LITE.host + ":" + LITE.port } };
    delete opts.headers["accept-encoding"];
    const up = http.request(opts, ur => {
        const ct = ur.headers["content-type"] || "";
        if (ct.includes("text/html")) {
            let body = [];
            ur.on("data", c => body.push(c));
            ur.on("end", () => {
                const inject = `<script>
(function(){
  /* Seed host token BEFORE app.js runs -> auto host-claim on welcome */
  localStorage.setItem('myRooms',JSON.stringify([{id:'${ROOM}',hostToken:'${TOKEN}',name:'VerifyTpl',type:'conference'}]));
  var INST=Math.floor(Math.random()*100000); /* page-instance marker */
  function rep(phase,extra){
    var lines=document.getElementById('lines');
    var bc=document.getElementById('hcBroadcast');
    var meter=document.getElementById('hcBcMeter');
    fetch('/__report',{method:'POST',body:JSON.stringify({phase:phase,extra:extra||'',
      inst:INST,
      cid:(typeof myClientId!=='undefined')?myClientId.slice(0,8):'(undef)',
      href:location.pathname+location.search,
      bcLabel:bc?bc.textContent:'(none)',
      meterW:meter?meter.style.width:'(none)',
      lineCount:lines?lines.children.length:0,
      gear:!!document.getElementById('hostGearBtn'),
      toolbar:!!document.getElementById('toolbar'),
      isHostVar:(typeof isHost!=='undefined')?isHost:'(undef)',
      pickerOpen:(function(){var lp=document.getElementById('langPicker');return lp?lp.classList.contains('open'):'(none)'})(),
      lastLines:lines?lines.innerText.slice(-500):''})});
  }
  setTimeout(function(){ if(typeof pickLang==='function'){try{pickLang('spa_Latn')}catch(e){rep('picklang-err',String(e))}} },4000);
  /* A headless prerender page can win the first host claim and die holding the
     slot; keep re-claiming until the server reaps its socket. */
  [8000,15000,22000,30000,38000,50000].forEach(function(ms){
    setTimeout(function(){
      if(typeof isHost!=='undefined'&&!isHost&&typeof tryClaimHost==='function'){try{tryClaimHost()}catch(e){}}
      rep('claimtick-'+ms);
    },ms);
  });
  /* hcBroadcast lives inside the host panel behind the gear button */
  var opened=false,clicked=false;
  var t=setInterval(function(){
    if(clicked){clearInterval(t);return}
    var bc=document.getElementById('hcBroadcast');
    if(bc){clicked=true;bc.click();rep('clicked-broadcast');return}
    var gear=document.getElementById('hostGearBtn');
    if(gear&&!opened){opened=true;gear.click();rep('opened-host-panel')}
  },1500);
  setTimeout(function(){rep('t20')},20000);
  setTimeout(function(){rep('t45')},45000);
  setTimeout(function(){rep('final')},70000);
})();
</script>`;
                let html = Buffer.concat(body).toString("utf8").replace(/<script/, inject + "\n<script");
                const h = { ...ur.headers }; delete h["content-length"]; delete h["content-encoding"];
                res.writeHead(ur.statusCode, h); res.end(html);
            });
        } else { res.writeHead(ur.statusCode, ur.headers); ur.pipe(res); }
    });
    up.on("error", () => { res.statusCode = 502; res.end("up err"); });
    req.pipe(up);
});

/* WebSocket tunnel — without this the client's /ws (welcome, host claim,
   commits, and the binary mic frames) never reaches Lite. */
const net = require("net");
proxy.on("upgrade", (req, socket, head) => {
    const up = net.connect(LITE.port, LITE.host, () => {
        let h = req.method + " " + req.url + " HTTP/1.1\r\n";
        for (let i = 0; i < req.rawHeaders.length; i += 2) {
            const k = req.rawHeaders[i];
            h += k + ": " + (k.toLowerCase() === "host" ? LITE.host + ":" + LITE.port : req.rawHeaders[i + 1]) + "\r\n";
        }
        h += "\r\n";
        up.write(h);
        if (head && head.length) up.write(head);
        socket.pipe(up); up.pipe(socket);
    });
    up.on("error", () => socket.destroy());
    socket.on("error", () => up.destroy());
});

proxy.listen(PROXY_PORT, () => {
    try { execSync(`powershell -Command "Get-CimInstance Win32_Process -Filter \\"Name='msedge.exe'\\" | Where-Object { $_.CommandLine -like '*et-webmic-profile*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: "ignore" }); } catch (e) { }
    fs.rmSync(PROFILE, { recursive: true, force: true });
    console.log("driving room " + ROOM + " with fake mic from " + WAV);
    const edge = spawn(EDGE, ["--headless=new", "--disable-gpu", "--no-first-run", "--mute-audio",
        "--use-fake-ui-for-media-stream",
        "--use-fake-device-for-media-stream",
        "--use-file-for-fake-audio-capture=" + WAV,
        "--autoplay-policy=no-user-gesture-required",
        "--user-data-dir=" + PROFILE,
        "http://localhost:" + PROXY_PORT + "/?room=" + ROOM], { stdio: "ignore" });
    setTimeout(() => {
        try { edge.kill(); } catch (e) { }
        try { execSync(`powershell -Command "Get-CimInstance Win32_Process -Filter \\"Name='msedge.exe'\\" | Where-Object { $_.CommandLine -like '*et-webmic-profile*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: "ignore" }); } catch (e) { }
        reports.forEach(r => console.log("[" + r.phase + "] inst=" + r.inst + " cid=" + r.cid + " bc='" + r.bcLabel + "' meter=" + r.meterW + " lines=" + r.lineCount +
            " gear=" + r.gear + " isHost=" + r.isHostVar + " picker=" + r.pickerOpen +
            (r.extra ? " extra=" + r.extra : "") + (r.lastLines ? "\n  text: " + r.lastLines.replace(/\n/g, " | ").slice(-400) : "")));
        proxy.close(); process.exit(0);
    }, 75000);
});
