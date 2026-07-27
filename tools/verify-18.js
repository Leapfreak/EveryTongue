/* Focused #18: fresh profile — unlock with the right creator code, then a
   second launch of the same profile to prove persistence.
     node tools/verify-18.js */
const http = require("http");
const path = require("path");
const fs = require("fs");
const { spawn, execSync } = require("child_process");

const LITE = { host: "localhost", port: 5981 };
const PROXY_PORT = 5985;
const EDGE = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const PROFILE = path.join(process.env.TEMP, "et-verify18-profile");

let current = null;
const proxy = http.createServer((req, res) => {
    if (req.url === "/__report") {
        let b = ""; req.on("data", c => b += c);
        req.on("end", () => { res.end("ok"); try { if (current) current.reports.push(JSON.parse(b)); } catch (e) { } });
        return;
    }
    const opts = { host: LITE.host, port: LITE.port, path: req.url, method: req.method, headers: { ...req.headers, host: LITE.host + ":" + LITE.port } };
    delete opts.headers["accept-encoding"];
    const up = http.request(opts, ur => {
        const ct = ur.headers["content-type"] || "";
        if (ct.includes("text/html") && current) {
            let body = [];
            ur.on("data", c => body.push(c));
            ur.on("end", () => {
                const inject = `<script>
(function(){
  var ACT=${JSON.stringify(current.act || null)};
  function rep(phase,extra){
    var ct=document.getElementById('creator-tools'),msg=document.getElementById('creator-code-msg');
    var vis=function(el){if(!el)return false;var s=getComputedStyle(el);var r=el.getBoundingClientRect();return s.display!=='none'&&r.width>0};
    fetch('/__report',{method:'POST',body:JSON.stringify({phase:phase,extra:extra||'',
      href:location.pathname,tools:vis(ct),msg:msg?msg.textContent:'?',
      ls:localStorage.getItem('creatorCode')||''})});
  }
  setTimeout(function(){rep('initial')},2000);
  if(ACT){setTimeout(function(){
    try{
      var inp=document.getElementById('creator-code-input');
      var go=document.getElementById('creator-code-go');
      if(!inp||!go){rep('act-missing');return}
      inp.value='creator77';
      go.click();
      rep('clicked');
    }catch(e){rep('act-error',String(e))}
  },3000)}
  setTimeout(function(){rep('final')},7500);
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

function launch(act) {
    return new Promise(resolve => {
        current = { act, reports: [] };
        const edge = spawn(EDGE, ["--headless=new", "--disable-gpu", "--no-first-run", "--mute-audio",
            "--user-data-dir=" + PROFILE, "http://localhost:" + PROXY_PORT + "/lobby.html"], { stdio: "ignore" });
        setTimeout(() => {
            try { edge.kill(); } catch (e) { }
            try { execSync(`powershell -Command "Get-CimInstance Win32_Process -Filter \\"Name='msedge.exe'\\" | Where-Object { $_.CommandLine -like '*et-verify18-profile*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: "ignore" }); } catch (e) { }
            const r = current.reports; current = null;
            setTimeout(() => resolve(r), 1000);
        }, 10000);
    });
}

(async () => {
    await new Promise(r => proxy.listen(PROXY_PORT, r));
    fs.rmSync(PROFILE, { recursive: true, force: true });
    console.log("run 1: unlock with right code");
    let r = await launch(true);
    r.forEach(x => console.log(" ", JSON.stringify(x)));
    console.log("run 2: same profile, no action (persistence)");
    r = await launch(null);
    r.forEach(x => console.log(" ", JSON.stringify(x)));
    proxy.close(); process.exit(0);
})();
