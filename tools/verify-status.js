/* One-off: admin Status card after the h.status + rooms?code fixes.
     node tools/verify-status.js */
const http = require("http");
const path = require("path");
const fs = require("fs");
const { spawn, execSync } = require("child_process");

const LITE = { host: "localhost", port: 5981 };
const PROXY_PORT = 5985;
const EDGE = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const PROFILE = path.join(process.env.TEMP, "et-verify-status-profile");

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
  setTimeout(function(){
    var p=document.getElementById('loginPin'),b=document.getElementById('btnLogin');
    if(p&&b){p.value='7777';b.click();}
  },2500);
  setTimeout(function(){
    fetch('/__report',{method:'POST',body:JSON.stringify({
      statLine:(document.getElementById('statLine')||{}).textContent||'?',
      statDetail:(document.getElementById('statDetail')||{}).textContent||'?',
      adminVisible:(function(){var a=document.getElementById('adminBody');return a&&getComputedStyle(a).display!=='none'})()
    })});
  },9000);
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

proxy.listen(PROXY_PORT, () => {
    fs.rmSync(PROFILE, { recursive: true, force: true });
    const edge = spawn(EDGE, ["--headless=new", "--disable-gpu", "--no-first-run", "--mute-audio",
        "--user-data-dir=" + PROFILE, "http://localhost:" + PROXY_PORT + "/admin.html"], { stdio: "ignore" });
    setTimeout(() => {
        try { edge.kill(); } catch (e) { }
        try { execSync(`powershell -Command "Get-CimInstance Win32_Process -Filter \\"Name='msedge.exe'\\" | Where-Object { $_.CommandLine -like '*et-verify-status-profile*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: "ignore" }); } catch (e) { }
        console.log(JSON.stringify(reports, null, 1));
        proxy.close(); process.exit(0);
    }, 12000);
});
