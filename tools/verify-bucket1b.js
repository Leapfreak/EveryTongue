/* Bucket-1 harness v2 — re-runs the checks that failed in v1 for harness
   reasons: uses a REAL room (arg 1), a unique Edge profile per scenario
   (kills profile-scoped Edge processes between scenarios to stop report
   bleed), and page-filtered report selection.
     node tools/verify-bucket1b.js <roomId> */
const http = require("http");
const path = require("path");
const fs = require("fs");
const { spawn, execSync } = require("child_process");

const ROOM = process.argv[2];
if (!ROOM) { console.error("usage: node verify-bucket1b.js <roomId>"); process.exit(2); }
const LITE = { host: "localhost", port: 5981 };
const PROXY_PORT = 5985;
const EDGE = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const PROFILES = path.join(process.env.TEMP, "et-verify2-profiles");

let current = null;

const WATCH_IDS = ["host-tools-gate", "creator-code-input", "creator-code-msg", "creator-tools",
    "room-list", "btn-dictate", "langPicker", "lpAdminToggle", "capBadge", "loginCard", "loginPin",
    "loginMsg", "adminBody", "setStt", "lines", "container"];

function driverJs(sc) {
    return `<script>
(function(){
  var SC=${JSON.stringify({ name: sc.name, action: sc.action || null, actionPage: sc.actionPage || null, actionDelay: sc.actionDelay || 2600 })};
  function vis(id){var el=document.getElementById(id);if(!el)return{exists:false};
    var s=getComputedStyle(el);var r=el.getBoundingClientRect();
    return{exists:true,visible:s.display!=='none'&&s.visibility!=='hidden'&&r.width>0&&r.height>0,
      text:(el.textContent||'').trim().slice(0,120),children:el.children?el.children.length:0};}
  function report(phase){
    var ids={};${JSON.stringify(WATCH_IDS)}.forEach(function(i){ids[i]=vis(i)});
    try{fetch('/__report',{method:'POST',body:JSON.stringify({scenario:SC.name,phase:phase,
      href:location.pathname+location.search,
      ids:ids,bodyText:(document.body?document.body.innerText:'').slice(0,8000),
      lsCreator:localStorage.getItem('creatorCode')||''})})}catch(e){}
  }
  setTimeout(function(){report('initial')},2000);
  setTimeout(function(){
    if(SC.action&&(!SC.actionPage||location.pathname===SC.actionPage)&&!sessionStorage.getItem('__acted_'+SC.name)){
      sessionStorage.setItem('__acted_'+SC.name,'1');
      try{eval(SC.action)}catch(e){
        fetch('/__report',{method:'POST',body:JSON.stringify({scenario:SC.name,phase:'action-error',href:location.pathname,err:String(e)})});
      }
    }
  },SC.actionDelay);
  setTimeout(function(){report('final')},7000);
})();
</script>`;
}

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
                let html = Buffer.concat(body).toString("utf8").replace(/<script/, driverJs(current) + "\n<script");
                const h = { ...ur.headers }; delete h["content-length"]; delete h["content-encoding"];
                res.writeHead(ur.statusCode, h); res.end(html);
            });
        } else { res.writeHead(ur.statusCode, ur.headers); ur.pipe(res); }
    });
    up.on("error", () => { res.statusCode = 502; res.end("upstream error"); });
    req.pipe(up);
});

function killProfileEdges() {
    try {
        execSync(`powershell -Command "Get-CimInstance Win32_Process -Filter \\"Name='msedge.exe'\\" | Where-Object { $_.CommandLine -like '*et-verify2-profiles*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"`, { stdio: "ignore" });
    } catch (e) { }
}

function runScenario(sc) {
    return new Promise(resolve => {
        current = { ...sc, reports: [] };
        const args = ["--headless=new", "--disable-gpu", "--no-first-run", "--mute-audio",
            "--user-data-dir=" + path.join(PROFILES, sc.profile)];
        if (sc.lang) args.push("--lang=" + sc.lang, "--accept-lang=" + sc.lang);
        args.push("http://localhost:" + PROXY_PORT + sc.url);
        const edge = spawn(EDGE, args, { stdio: "ignore" });
        setTimeout(() => {
            try { edge.kill(); } catch (e) { }
            killProfileEdges();
            const r = current.reports; current = null;
            setTimeout(() => resolve(r), 800);
        }, sc.waitMs || 10000);
    });
}

const last = (r, phase, page) => {
    const l = r.filter(x => x.phase === phase && (!page || (x.href || "").startsWith(page)));
    return l[l.length - 1];
};
const results = [];
const verdict = (num, name, pass, detail) => {
    results.push({ num, name, pass });
    console.log((pass ? "PASS" : "FAIL") + "  #" + num + " " + name + (detail ? "  — " + detail : ""));
};

(async () => {
    await new Promise(r => proxy.listen(PROXY_PORT, r));
    fs.rmSync(PROFILES, { recursive: true, force: true });
    console.log("proxy :" + PROXY_PORT + " → lite :" + LITE.port + " | room " + ROOM + "\n");

    // #17 clean client vs ?diag=1 — REAL room
    let r = await runScenario({ name: "diag-off", profile: "p17a", url: "/?room=" + ROOM });
    let offF = last(r, "final", "/");
    const inRoomOff = offF && offF.ids["lines"].exists;
    const diagOffOk = offF && (!offF.ids["capBadge"].exists || !offF.ids["capBadge"].visible);
    r = await runScenario({ name: "diag-on", profile: "p17b", url: "/?room=" + ROOM + "&diag=1" });
    let onF = last(r, "final", "/");
    const diagOnOk = onF && onF.ids["capBadge"].exists;
    verdict(17, "clean client / ?diag=1", !!(inRoomOff && diagOffOk && diagOnOk),
        "inRoom=" + inRoomOff + " offHidden=" + diagOffOk + " onPresent=" + diagOnOk);

    // #15 guest home → picker (real room, fresh profile)
    r = await runScenario({
        name: "guest-home", profile: "p15", url: "/?room=" + ROOM,
        actionPage: "/", action: "goHome();", actionDelay: 3500, waitMs: 11000
    });
    let f = last(r, "final", "/");
    const stayed = f && !f.href.startsWith("/lobby");
    const picker = f && f.ids["langPicker"].visible;
    verdict(15, "guest home → language picker", !!(stayed && picker),
        f ? "href=" + f.href + " pickerVisible=" + picker : "no report");

    // #26 picker Administrator toggle+link → /admin.html
    r = await runScenario({
        name: "admin-link", profile: "p26", url: "/?room=" + ROOM,
        actionPage: "/", action: "goHome();setTimeout(function(){openAdminPage()},800);", actionDelay: 3500, waitMs: 13000
    });
    const adminArr = r.find(x => (x.href || "").startsWith("/admin.html"));
    verdict(26, "picker Administrator → admin.html", !!adminArr, adminArr ? "navigated" : "no arrival");

    // #18 creator gate: wrong → right → persist (one profile, three launches)
    r = await runScenario({
        name: "lob-wrong", profile: "p18", url: "/lobby.html", actionPage: "/lobby.html",
        action: "document.getElementById('creator-code-input').value='definitelywrong';document.getElementById('creator-code-go').click();",
        waitMs: 11000
    });
    f = last(r, "final", "/lobby.html");
    const wrongRej = f && !f.ids["creator-tools"].visible && f.lsCreator === "";
    const wrongMsg = f ? f.ids["creator-code-msg"].text : "?";
    r = await runScenario({
        name: "lob-right", profile: "p18", url: "/lobby.html", actionPage: "/lobby.html",
        action: "document.getElementById('creator-code-input').value='creator77';document.getElementById('creator-code-go').click();",
        waitMs: 12000
    });
    f = last(r, "final", "/lobby.html");
    const unlocked = f && f.ids["creator-tools"].visible && f.lsCreator === "creator77";
    const unlockDbg = f ? ("ls='" + f.lsCreator + "' tools=" + f.ids["creator-tools"].visible + " msg='" + f.ids["creator-code-msg"].text + "'") : "no report";
    r = await runScenario({ name: "lob-persist", profile: "p18", url: "/lobby.html", waitMs: 10000 });
    f = last(r, "final", "/lobby.html");
    const persisted = f && f.ids["creator-tools"].visible && f.lsCreator === "creator77";
    verdict(18, "creator gate wrong/right/persist", !!(wrongRej && unlocked && persisted),
        "wrongRej=" + wrongRej + "(msg='" + wrongMsg + "') unlocked=" + unlocked + "(" + unlockDbg + ") persisted=" + persisted);

    // #24 wrong PIN → visible rejection (fresh profile, page-filtered)
    r = await runScenario({
        name: "wrongpin", profile: "p24", url: "/admin.html", actionPage: "/admin.html",
        action: "document.getElementById('loginPin').value='0000';document.getElementById('btnLogin').click();",
        waitMs: 11000
    });
    f = last(r, "final", "/admin.html");
    const rej = f && !f.ids["adminBody"].visible && (f.ids["loginMsg"].text || "").length > 0;
    verdict(24, "wrong PIN rejected visibly", !!rej,
        f ? "adminBody=" + f.ids["adminBody"].visible + " msg='" + f.ids["loginMsg"].text + "'" : "no report");

    // #32 straggler triage — same sweep, but print ±60 chars context for each hit
    const en = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "locales", "en.json"), "utf8"));
    for (const langCode of ["ca"]) {
        const loc = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "locales", langCode + ".json"), "utf8"));
        let allText = "";
        for (const page of [
            { u: "/?room=" + ROOM, p: "p32room-" + langCode },
            { u: "/lobby.html", p: "p18" },  // unlocked volunteer lobby
            { u: "/admin.html", p: "p32adm-" + langCode, act: "document.getElementById('loginPin').value='7777';document.getElementById('btnLogin').click();" }]) {
            r = await runScenario({
                name: "loc" + page.u.replace(/\W/g, ""), profile: page.p, url: page.u, lang: langCode,
                actionPage: page.act ? page.u.split("?")[0] : null, action: page.act || null, waitMs: 11000
            });
            f = last(r, "final");
            if (f) allText += "\n" + f.bodyText;
        }
        const hits = [];
        for (const k of Object.keys(en)) {
            if (!k.startsWith("web.")) continue;
            const ev = en[k], lv = loc[k];
            if (typeof ev !== "string" || ev.length < 5 || !lv || lv === ev || /\{|%/.test(ev)) continue;
            const idx = allText.indexOf(ev);
            if (idx >= 0 && !allText.includes(lv)) {
                hits.push({ k, ev, ctx: allText.slice(Math.max(0, idx - 60), idx + ev.length + 60).replace(/\n/g, " | ") });
            }
        }
        console.log("\n[#32/" + langCode + "] " + hits.length + " suspected stragglers:");
        hits.forEach(h => console.log("  " + h.k + " ('" + h.ev + "')\n    ..." + h.ctx + "..."));
        verdict(32, "localization sweep " + langCode, hits.length === 0, hits.length + " suspects (see context above)");
    }

    console.log("\n== SUMMARY ==");
    results.forEach(x => console.log((x.pass ? "PASS" : "FAIL") + "  #" + x.num + " " + x.name));
    proxy.close();
    process.exit(0);
})();
