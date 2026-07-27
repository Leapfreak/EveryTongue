/* Bucket-1 verification harness: drives the REAL web client (served by a
   locally running Lite on :5981) through headless Edge, one scenario at a
   time, via an injecting proxy on :5985. The proxy pipes every request to
   Lite untouched except HTML, into which it injects a driver script that
   samples DOM state (and optionally performs one scripted action), then
   POSTs reports back to the proxy.
     node tools/verify-bucket1.js */
const http = require("http");
const path = require("path");
const fs = require("fs");
const { spawn } = require("child_process");

const LITE = { host: "localhost", port: 5981 };
const PROXY_PORT = 5985;
const EDGE = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const PROFILES = path.join(process.env.TEMP, "et-verify-profiles");

let current = null; // {name, action, reports:[], resolve}

const WATCH_IDS = ["host-tools-gate", "host-tools-form", "creator-code-input", "creator-code-msg",
    "creator-tools", "room-list", "rooms-section", "btn-dictate", "langPicker", "lpAdminToggle",
    "capBadge", "loginCard", "loginPin", "loginMsg", "adminBody", "setStt", "lines", "container",
    "qr-overlay", "btn-create-conference"];

function driverJs(scenario) {
    return `<script>
(function(){
  var SC=${JSON.stringify({ name: scenario.name, action: scenario.action || null, actionPage: scenario.actionPage || null })};
  function vis(id){var el=document.getElementById(id);if(!el)return{exists:false};
    var s=getComputedStyle(el);var r=el.getBoundingClientRect();
    return{exists:true,visible:s.display!=='none'&&s.visibility!=='hidden'&&r.width>0&&r.height>0,
      text:(el.textContent||'').trim().slice(0,120),
      value:el.value!==undefined?String(el.value).slice(0,40):undefined,
      children:el.children?el.children.length:0};}
  function report(phase){
    var ids={};${JSON.stringify(WATCH_IDS)}.forEach(function(i){ids[i]=vis(i)});
    var payload={scenario:SC.name,phase:phase,href:location.pathname+location.search,
      docLang:document.documentElement.lang||'',
      ids:ids,bodyText:(document.body?document.body.innerText:'').slice(0,6000),
      lsCreator:localStorage.getItem('creatorCode')||''};
    try{fetch('/__report',{method:'POST',body:JSON.stringify(payload)})}catch(e){}
  }
  setTimeout(function(){report('initial')},1800);
  setTimeout(function(){
    if(SC.action&&(!SC.actionPage||location.pathname===SC.actionPage)&&!sessionStorage.getItem('__acted_'+SC.name)){
      sessionStorage.setItem('__acted_'+SC.name,'1');
      try{eval(SC.action)}catch(e){
        fetch('/__report',{method:'POST',body:JSON.stringify({scenario:SC.name,phase:'action-error',err:String(e)})});
      }
    }
  },2400);
  setTimeout(function(){report('final')},5200);
})();
</script>`;
}

const proxy = http.createServer((req, res) => {
    if (req.url === "/__report") {
        let b = ""; req.on("data", c => b += c);
        req.on("end", () => {
            res.end("ok");
            try { const r = JSON.parse(b); if (current) { current.reports.push(r); } } catch (e) { }
        });
        return;
    }
    const opts = { host: LITE.host, port: LITE.port, path: req.url, method: req.method, headers: { ...req.headers, host: LITE.host + ":" + LITE.port } };
    delete opts.headers["accept-encoding"]; // keep upstream uncompressed so we can inject
    const up = http.request(opts, ur => {
        const ct = ur.headers["content-type"] || "";
        if (ct.includes("text/html") && current) {
            let body = [];
            ur.on("data", c => body.push(c));
            ur.on("end", () => {
                let html = Buffer.concat(body).toString("utf8");
                html = html.replace(/<script/, driverJs(current) + "\n<script");
                const h = { ...ur.headers }; delete h["content-length"]; delete h["content-encoding"];
                res.writeHead(ur.statusCode, h);
                res.end(html);
            });
        } else {
            res.writeHead(ur.statusCode, ur.headers);
            ur.pipe(res);
        }
    });
    up.on("error", () => { res.statusCode = 502; res.end("upstream error"); });
    req.pipe(up);
});

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
            const r = current.reports; current = null;
            resolve(r);
        }, sc.waitMs || 8000);
    });
}

function get(reports, phase, page) {
    const list = reports.filter(r => r.phase === phase && (!page || (r.href || "").startsWith(page)));
    return list[list.length - 1];
}

const results = [];
function verdict(num, name, pass, detail) {
    results.push({ num, name, pass, detail });
    console.log((pass ? "PASS" : "FAIL") + "  #" + num + " " + name + (detail ? "  — " + detail : ""));
}

(async () => {
    await new Promise(r => proxy.listen(PROXY_PORT, r));
    fs.rmSync(PROFILES, { recursive: true, force: true });
    console.log("proxy on :" + PROXY_PORT + " → lite :" + LITE.port + "\n");

    // ── #16 bare URL as a fresh guest ──
    let r = await runScenario({ name: "bare-url", profile: "guest", url: "/" });
    let f = get(r, "final");
    if (!f) verdict(16, "bare URL locked", false, "no report");
    else {
        const roomListLeak = f.ids["room-list"].exists && f.ids["room-list"].visible && f.ids["room-list"].children > 0;
        const gated = (f.ids["host-tools-gate"].exists && f.ids["host-tools-gate"].visible) ||
            (f.ids["langPicker"].exists && f.ids["langPicker"].visible);
        verdict(16, "bare URL → locked, no room list", gated && !roomListLeak,
            "landed " + f.href + ", gate/picker=" + gated + ", roomListLeak=" + roomListLeak);
    }

    // ── #17 diag off by default / on with ?diag=1 ──
    r = await runScenario({ name: "diag-off", profile: "guest", url: "/?room=fakeroom" });
    f = get(r, "final");
    const diagOff = f && (!f.ids["capBadge"].exists || !f.ids["capBadge"].visible);
    r = await runScenario({ name: "diag-on", profile: "guest", url: "/?room=fakeroom&diag=1" });
    f = get(r, "final");
    const diagOn = f && f.ids["capBadge"].exists;
    verdict(17, "clean client / ?diag=1", !!(diagOff && diagOn), "off-hidden=" + diagOff + ", on-present=" + diagOn);

    // ── #18a wrong creator code rejected ──
    r = await runScenario({
        name: "lobby-wrong", profile: "vol", url: "/lobby.html",
        actionPage: "/lobby.html",
        action: "document.getElementById('creator-code-input').value='definitelywrong';document.getElementById('creator-code-go').click();"
    });
    f = get(r, "final");
    const wrongRejected = f && !f.ids["creator-tools"].visible && f.lsCreator === "";
    const wrongMsg = f ? f.ids["creator-code-msg"].text : "";
    // ── #18b right code unlocks ──
    r = await runScenario({
        name: "lobby-right", profile: "vol", url: "/lobby.html",
        actionPage: "/lobby.html",
        action: "document.getElementById('creator-code-input').value='creator77';document.getElementById('creator-code-go').click();"
    });
    f = get(r, "final");
    const unlocked = f && f.ids["creator-tools"].visible && f.lsCreator === "creator77";
    // ── #18c persists across browser restart (same profile, no action) ──
    r = await runScenario({ name: "lobby-persist", profile: "vol", url: "/lobby.html" });
    f = get(r, "final");
    const persisted = f && f.ids["creator-tools"].visible && f.lsCreator === "creator77";
    verdict(18, "creator gate: wrong/right/persist", !!(wrongRejected && unlocked && persisted),
        "wrongRejected=" + wrongRejected + " (msg='" + wrongMsg + "'), unlocked=" + unlocked + ", persisted=" + persisted);

    // ── #19 volunteer home → lobby ──
    r = await runScenario({
        name: "vol-home", profile: "vol", url: "/?room=fakeroom",
        actionPage: "/", action: "goHome();", waitMs: 10000
    });
    const lobbyArrival = r.find(x => (x.href || "").startsWith("/lobby.html"));
    verdict(19, "volunteer home → lobby", !!lobbyArrival, lobbyArrival ? "navigated to " + lobbyArrival.href : "no lobby arrival");

    // ── #15 guest home → picker (fresh guest profile has no creatorCode) ──
    r = await runScenario({
        name: "guest-home", profile: "guest", url: "/?room=fakeroom",
        actionPage: "/", action: "goHome();", waitMs: 10000
    });
    f = get(r, "final", "/");
    const pickerShown = f && f.ids["langPicker"].visible && f.href.startsWith("/?") || (f && f.href === "/" && f.ids["langPicker"].visible);
    verdict(15, "guest home → language picker", !!(f && f.ids["langPicker"].visible && !f.href.startsWith("/lobby")),
        f ? "href=" + f.href + ", pickerVisible=" + f.ids["langPicker"].visible : "no report");

    // ── #21 dictation gated (locked lobby, guest profile) ──
    r = await runScenario({ name: "dict-gate", profile: "guest", url: "/lobby.html" });
    f = get(r, "final");
    const dictHidden = f && (!f.ids["btn-dictate"].visible) && (!f.ids["creator-tools"].visible);
    verdict(21, "dictation gated behind creator code", !!dictHidden,
        f ? "btn-dictate visible=" + f.ids["btn-dictate"].visible + ", creator-tools visible=" + f.ids["creator-tools"].visible : "no report");

    // ── #22 admin PIN login + browser-language UI (Catalan) ──
    r = await runScenario({
        name: "admin-login", profile: "admin", url: "/admin.html", lang: "ca",
        actionPage: "/admin.html",
        action: "document.getElementById('loginPin').value='7777';document.getElementById('btnLogin').click();",
        waitMs: 10000
    });
    let ini = get(r, "initial"); f = get(r, "final");
    const loginShownFirst = ini && ini.ids["loginCard"].visible && !ini.ids["adminBody"].visible;
    const adminIn = f && f.ids["adminBody"].visible && f.ids["setStt"].exists;
    const caText = f ? f.bodyText : "";
    verdict(22, "admin PIN login works", !!(loginShownFirst && adminIn),
        "loginFirst=" + loginShownFirst + ", adminBody+setStt=" + adminIn);
    fs.writeFileSync(path.join(process.env.TEMP, "et-admin-ca.txt"), caText, "utf8");

    // ── #24 wrong PIN → visible rejection, no fake success ──
    r = await runScenario({
        name: "admin-wrongpin", profile: "guest", url: "/admin.html",
        actionPage: "/admin.html",
        action: "document.getElementById('loginPin').value='0000';document.getElementById('btnLogin').click();",
        waitMs: 9000
    });
    f = get(r, "final");
    const rejected = f && !f.ids["adminBody"].visible && (f.ids["loginMsg"].text || "").length > 0;
    verdict(24, "wrong PIN rejected visibly", !!rejected,
        f ? "adminBody=" + f.ids["adminBody"].visible + ", msg='" + f.ids["loginMsg"].text + "'" : "no report");

    // ── #26 picker Administrator link → /admin.html ──
    r = await runScenario({
        name: "admin-link", profile: "guest", url: "/?room=fakeroom",
        actionPage: "/", action: "openAdminPage();", waitMs: 10000
    });
    const adminArrival = r.find(x => (x.href || "").startsWith("/admin.html"));
    verdict(26, "picker Administrator link → admin.html", !!adminArrival,
        adminArrival ? "navigated" : "no admin.html arrival");

    // ── #32 localization sweep: render pages in ca and es, hunt EN stragglers ──
    const en = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "locales", "en.json"), "utf8"));
    for (const langCode of ["ca", "es"]) {
        const loc = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "locales", langCode + ".json"), "utf8"));
        let allText = "";
        for (const page of [{ u: "/", p: "guest-" + langCode }, { u: "/lobby.html", p: "vol" }, { u: "/admin.html", p: "admin-" + langCode, act: "document.getElementById('loginPin').value='7777';document.getElementById('btnLogin').click();" }]) {
            r = await runScenario({
                name: "loc-" + langCode + page.u.replace(/\W/g, ""), profile: page.p, url: page.u, lang: langCode,
                actionPage: page.act ? page.u : null, action: page.act || null, waitMs: 9000
            });
            f = get(r, "final"); if (f) allText += "\n" + f.bodyText;
        }
        const stragglers = [];
        for (const k of Object.keys(en)) {
            if (!k.startsWith("web.")) continue;
            const ev = en[k], lv = loc[k];
            if (typeof ev !== "string" || ev.length < 5 || !lv || lv === ev) continue; // untranslatable/identical
            if (/\{|%/.test(ev)) continue;
            if (allText.includes(ev) && !allText.includes(lv)) stragglers.push(k + " ('" + ev + "')");
        }
        console.log("  [#32/" + langCode + "] EN stragglers on rendered pages: " + (stragglers.length ? stragglers.join("; ") : "(none)"));
        verdict(32, "localization sweep " + langCode, stragglers.length === 0, stragglers.length + " stragglers");
    }

    console.log("\n== SUMMARY ==");
    results.forEach(x => console.log((x.pass ? "PASS" : "FAIL") + "  #" + x.num + " " + x.name));
    proxy.close();
    process.exit(results.every(x => x.pass) ? 0 : 1);
})();
