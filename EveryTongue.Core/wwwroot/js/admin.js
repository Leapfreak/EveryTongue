/* Every Tongue — Admin page
   The single home for ALL server administration (engines/keys/PIN/host code,
   conference templates, Bible downloads, raw config, log tail, Live remote).
   Extracted from app.js's settings overlay in the three-page IA restructure:
   admin = this page, volunteers = lobby.html, guests = QR deep links only.
   ES6 is fine here (same rule as lobby.js — app.js stays ES5 for old phones). */

(function () {
    "use strict";

    // ── Localization: inline English defaults + /api/locale overlay,
    //    UI language auto-detected from the browser (no language step). ──
    const LT = {
        admTitle: "Server Administration", admSub: "Every Tongue",
        admEnter: "Enter", admRefresh: "Refresh", admLive: "Live session",
        admBootstrap: "No admin PIN is set — anyone can open this page. Set a PIN below now.",
        admDefaultPin: "The admin PIN is still the default (1234) — change it below.",
        admQrPermanent: "Permanent — print this. It always joins the current room made from this template.",
        admQrBtn: "QR", admLoadFail: "Failed to load",
        adminPin: "PIN", adminBad: "Invalid PIN", cancel: "Cancel", checking: "Checking...",
        liveRun: "Live: RUNNING", stopped: "Status: STOPPED", noServer: "Unable to reach server",
        sending: "Sending...", cmdSent: " command sent", cmdFail: "Failed to send command",
        start: "Start", stop: "Stop", restart: "Restart", clear: "Clear", lbClose: "Close",
        autoDetect: "Auto Detect", noTranslation: "No translation",
        setTitle: "Server Settings",
        setSttEngine: "Speech engine", setTransEngine: "Translation engine",
        setKeysStt: "Speech API keys", setKeysTrans: "Translation API keys",
        setKeySet: "•••• configured — leave blank to keep", setKeyEmpty: "not set — paste key",
        setPinLabel: "Admin PIN", setPinNew: "choose a PIN",
        setPinRequired: "Set an admin PIN to secure the server first",
        setCreatorLabel: "Host tools code",
        setCreatorHint: "Volunteers enter this in the lobby to create rooms. Empty = anyone can create. Enter \"-\" to clear.",
        setCreatorEmpty: "not set — room creation is open",
        setSave: "Save", setSaved: "Saved ✓", setViewLog: "View server log", setBadPin: "Not authorized",
        setTplsBtn: "Conference templates", setTplsNew: "New template", setTplsNone: "No templates yet.",
        setTplsEdit: "Edit", setTplsDelete: "Delete", setTplsDeleteConfirm: "Delete this template?",
        setTplsName: "Name", setTplsHostCode: "Hosting code (volunteers enter this to start the room)",
        setTplsSourceLang: "Speaker language", setTplsAudio: "Microphone",
        setTplsAudioWeb: "Web mic (browser broadcast)", setTplsAudioWebRaw: "Web mic, raw (PA/line feed)",
        setTplsAudioLocal: "Local device (server machine)",
        setTplsVisibility: "Room visibility", setTplsPublic: "Public (listed in lobby)",
        setTplsPrivate: "Private (QR/link only)", setTplsOffered: "Offered languages",
        setTplsOfferedHint: "Comma-separated FLORES codes, e.g. spa_Latn, eng_Latn, cat_Latn. Empty = listeners can pick any language.",
        setTplsServerDefault: "(server default)", setTplsNameReq: "Name and hosting code are required",
        setBiblesBtn: "Bibles (download)",
        setBiblesHint: "Freely-redistributable Bibles from eBible.org. Copyrighted translations must be copied into the Bibles folder manually.",
        setBiblesSearch: "Search by language or name...", setBiblesInstalled: "installed",
        setBiblesDownload: "Download", setBiblesRetry: "Retry",
        setBiblesDownloading: "downloading", setBiblesConverting: "converting", setBiblesVerifying: "verifying",
        setBiblesTypeToSearch: "Type at least 2 letters to search the catalog. Installed Bibles are listed above.",
        setBiblesMore: "+{0} more — refine your search", setBiblesNone: "No matches.",
        setRawBtn: "Advanced: edit raw config",
        setRawHint: "The full server configuration (config.json). Engine, API key, PIN and host-code changes apply immediately; other changes apply after a restart.",
        setRawSave: "Save raw config", setRawInvalid: "Invalid JSON: ", setRawSaved: "Config saved ✓",
        setRawRestart: "Config saved ✓ — some changes apply after a restart", setRawLoadFail: "Failed to load config",
        setRawPinCleared: "Warning: admin PIN cleared — settings are now open",
        netError: "Network error"
    };
    function t(k) { return LT[k] || k; }
    function fmt(k, v) { return t(k).replace("{0}", v); }
    function esc(x) {
        return String(x).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }
    function applyI18n() {
        document.querySelectorAll("[data-i18n]").forEach(el => {
            const v = LT[el.getAttribute("data-i18n")];
            if (v) el.textContent = v;
        });
        document.querySelectorAll("[data-i18n-ph]").forEach(el => {
            const v = LT[el.getAttribute("data-i18n-ph")];
            if (v) el.placeholder = v;
        });
    }
    (function () {
        try {
            const nav = navigator.language || navigator.userLanguage || "";
            const lang = nav.split("-")[0].toLowerCase();
            fetch("/api/locale" + (lang ? "?lang=" + encodeURIComponent(lang) : ""))
                .then(r => r.json()).then(data => {
                    for (const k in data) { if (Object.prototype.hasOwnProperty.call(data, k)) LT[k] = data[k]; }
                    applyI18n();
                }).catch(() => { });
        } catch (e) { /* English defaults remain */ }
    })();
    applyI18n();

    // ── State ──
    let pin = sessionStorage.getItem("adminPin") || "";
    let settings = null;           // last GET /api/settings payload
    let sttLanguages = null;       // /api/stt-languages
    const $ = id => document.getElementById(id);
    function msg(id, text, ok) {
        const el = $(id);
        if (!el) return;
        el.style.color = ok ? "#4f4" : "#f44";
        el.textContent = text;
    }
    function jsonOrStatus(r) {
        return r.json().catch(() => ({ ok: r.status >= 200 && r.status < 300 }));
    }
    function qpin() { return "pin=" + encodeURIComponent(pin); }

    // ── Login ──
    function tryLogin(candidate, silent) {
        return fetch("/api/settings?pin=" + encodeURIComponent(candidate))
            .then(r => r.json()).then(s => {
                if (s.error) {
                    if (!silent) msg("loginMsg", t("adminBad"));
                    return false;
                }
                pin = candidate;
                sessionStorage.setItem("adminPin", pin);
                sessionStorage.setItem("isAdmin", "true");
                settings = s;
                enterAdmin();
                return true;
            }).catch(() => { if (!silent) msg("loginMsg", t("netError")); return false; });
    }
    $("btnLogin").addEventListener("click", () => tryLogin($("loginPin").value, false));
    $("loginPin").addEventListener("keydown", e => { if (e.key === "Enter") tryLogin($("loginPin").value, false); });

    // Auto-enter: stored PIN, or bootstrap-open server (no PIN configured).
    tryLogin(pin, true).then(ok => { if (!ok && pin) { pin = ""; sessionStorage.removeItem("adminPin"); } });

    function enterAdmin() {
        $("loginCard").style.display = "none";
        $("adminBody").style.display = "";
        if (!settings.adminPinSet) {
            $("bootstrapWarn").style.display = "";
            $("bootstrapWarn").textContent = t("admBootstrap");
        }
        renderSettings();
        loadLiveCard();
        loadTemplates();
        loadSttLanguages();
    }

    // ── Live session remote (desktop head only) ──
    let livePoll = null;
    function loadLiveCard() {
        fetch("/api/config").then(r => r.json()).then(cfg => {
            if (!cfg.hasLiveSession) return;
            const card = $("liveCard");
            card.style.display = "";
            card.addEventListener("toggle", () => {
                if (card.open) { pollLive(); livePoll = setInterval(pollLive, 3000); }
                else if (livePoll) { clearInterval(livePoll); livePoll = null; }
            });
        }).catch(() => { });
    }
    function pollLive() {
        fetch("/api/control?action=status").then(r => r.json()).then(d => {
            const el = $("liveStatus");
            if (d.live) { el.textContent = t("liveRun"); el.style.color = "#4f4"; }
            else { el.textContent = t("stopped"); el.style.color = "#f44"; }
        }).catch(() => { const el = $("liveStatus"); el.textContent = t("noServer"); el.style.color = "#888"; });
    }
    document.querySelectorAll("[data-live]").forEach(btn => {
        btn.addEventListener("click", () => {
            const action = btn.getAttribute("data-live");
            $("liveStatus").textContent = t("sending");
            fetch("/api/control?action=" + action + "&" + qpin()).then(r => r.json()).then(d => {
                $("liveStatus").textContent = d.error ? d.error : (action + t("cmdSent"));
                setTimeout(pollLive, 800);
            }).catch(() => { $("liveStatus").textContent = t("cmdFail"); });
        });
    });

    // ── Server settings ──
    function renderSettings() {
        const s = settings;
        const fill = (selId, engines, current) => {
            const sel = $(selId);
            sel.innerHTML = "";
            engines.forEach(e => {
                const o = document.createElement("option");
                o.value = e.key; o.textContent = e.name;
                sel.appendChild(o);
            });
            sel.value = current;
        };
        fill("setStt", s.sttEngines, s.sttBackend);
        fill("setTrans", s.translationEngines, s.translationBackend);

        const keys = (holderId, engines, attr, label) => {
            const holder = $(holderId);
            holder.innerHTML = "<label>" + esc(t(label)) + "</label>";
            engines.filter(e => e.requiresKey).forEach(e => {
                const l = document.createElement("label");
                l.textContent = e.name;
                l.style.cssText = "font-size:11px;margin-top:6px";
                const inp = document.createElement("input");
                inp.type = "password"; inp.autocomplete = "off";
                inp.setAttribute(attr, e.key);
                inp.placeholder = e.keySet ? t("setKeySet") : t("setKeyEmpty");
                inp.style.borderColor = e.keySet ? "#3a5" : "#444";
                holder.appendChild(l); holder.appendChild(inp);
            });
        };
        keys("keysStt", s.sttEngines, "data-stt-key", "setKeysStt");
        keys("keysTrans", s.translationEngines, "data-trans-key", "setKeysTrans");
        $("setNewPin").placeholder = s.adminPinSet ? t("setKeySet") : t("setPinNew");
        $("setCreatorCode").placeholder = s.creatorCodeSet ? t("setKeySet") : t("setCreatorEmpty");
    }
    $("btnSave").addEventListener("click", () => {
        const body = { pin: pin, sttBackend: $("setStt").value, translationBackend: $("setTrans").value, sttKeys: {}, translationKeys: {} };
        document.querySelectorAll("input[data-stt-key]").forEach(i => { if (i.value) body.sttKeys[i.getAttribute("data-stt-key")] = i.value; });
        document.querySelectorAll("input[data-trans-key]").forEach(i => { if (i.value) body.translationKeys[i.getAttribute("data-trans-key")] = i.value; });
        const newPin = $("setNewPin").value;
        if (newPin) body.adminPin = newPin;
        const newCreator = $("setCreatorCode").value;
        if (newCreator) body.creatorCode = newCreator;
        if (!settings.adminPinSet && !newPin) { msg("setMsg", t("setPinRequired")); return; }
        fetch("/api/settings", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) })
            .then(jsonOrStatus).then(res => {
                if (!res.ok) { msg("setMsg", res.error || t("setBadPin")); return; }
                msg("setMsg", t("setSaved"), true);
                if (newPin) { pin = newPin; sessionStorage.setItem("adminPin", pin); }
                // refresh the keySet/pinSet indicators
                fetch("/api/settings?" + qpin()).then(r => r.json()).then(s => {
                    if (!s.error) { settings = s; renderSettings(); $("bootstrapWarn").style.display = s.adminPinSet ? "none" : ""; }
                });
            }).catch(() => msg("setMsg", t("cmdFail")));
    });

    // ── Conference templates ──
    let tplData = null, tplEditingId = "";
    function loadSttLanguages() {
        fetch("/api/stt-languages").then(r => r.json()).then(ls => { sttLanguages = ls || []; fillTplLangs(); }).catch(() => { sttLanguages = []; });
    }
    function fillTplLangs(current) {
        const sel = $("tplLang");
        sel.innerHTML = "";
        const auto = document.createElement("option");
        auto.value = "auto"; auto.textContent = t("autoDetect");
        sel.appendChild(auto);
        (sttLanguages || []).forEach(L => {
            const o = document.createElement("option");
            o.value = L.code; o.textContent = L.name;
            sel.appendChild(o);
        });
        sel.value = current || "auto";
        if (!sel.value) sel.value = "auto";
    }
    function loadTemplates() {
        fetch("/api/settings/templates?" + qpin()).then(r => r.json()).then(res => {
            if (res.error) { $("tplList").innerHTML = '<div class="hint">' + esc(res.error) + "</div>"; return; }
            tplData = res.templates || [];
            renderTemplates();
        }).catch(() => { $("tplList").innerHTML = '<div class="hint">' + t("netError") + "</div>"; });
    }
    function renderTemplates() {
        const lst = $("tplList");
        if (!tplData || !tplData.length) { lst.innerHTML = '<div class="hint">' + t("setTplsNone") + "</div>"; return; }
        lst.innerHTML = tplData.map(tp => {
            const audio = tp.audioSource === "local" ? t("setTplsAudioLocal") : (tp.webMicRaw ? t("setTplsAudioWebRaw") : t("setTplsAudioWeb"));
            let sub = tp.sourceLanguage + " · " + audio;
            if (tp.offeredLanguages && tp.offeredLanguages.length) sub += " · " + tp.offeredLanguages.join(", ");
            return '<div class="row">' +
                '<div class="grow"><div class="title">' + esc(tp.name) + '</div><div class="sub">' + esc(sub) + "</div></div>" +
                '<button data-tpl-qr="' + esc(tp.id) + '" data-tpl-qr-name="' + esc(tp.name) + '">' + t("admQrBtn") + "</button>" +
                '<button data-tpl-edit="' + esc(tp.id) + '">' + t("setTplsEdit") + "</button>" +
                '<button class="outline" data-tpl-del="' + esc(tp.id) + '">' + t("setTplsDelete") + "</button>" +
                "</div>";
        }).join("");
    }
    function fillEngineSelect(selId, engines, current, withDefault) {
        const sel = $(selId);
        sel.innerHTML = "";
        if (withDefault) {
            const d = document.createElement("option");
            d.value = ""; d.textContent = t("setTplsServerDefault");
            sel.appendChild(d);
        }
        engines.forEach(e => {
            const o = document.createElement("option");
            o.value = e.key; o.textContent = e.name;
            sel.appendChild(o);
        });
        sel.value = current;
        if (!sel.value && !withDefault && engines.length) sel.selectedIndex = 0;
    }
    function showTplForm(tp) {
        tplEditingId = tp ? tp.id : "";
        $("tplName").value = tp ? tp.name : "";
        $("tplCode").value = tp ? tp.hostingCode : "";
        fillTplLangs(tp ? tp.sourceLanguage : "auto");
        fillEngineSelect("tplStt", settings.sttEngines, tp && tp.sttBackend ? tp.sttBackend : settings.sttBackend, false);
        fillEngineSelect("tplTrans", settings.translationEngines, tp ? (tp.translationBackend || "") : "", true);
        $("tplAudio").value = tp ? (tp.audioSource === "local" ? "local" : (tp.webMicRaw ? "webraw" : "web")) : "web";
        $("tplVis").value = tp ? tp.visibility : "public";
        $("tplOffered").value = tp && tp.offeredLanguages ? tp.offeredLanguages.join(", ") : "";
        msg("tplMsg", "");
        $("tplForm").style.display = "";
    }
    $("btnTplNew").addEventListener("click", () => showTplForm(null));
    $("btnTplCancel").addEventListener("click", () => { $("tplForm").style.display = "none"; });
    $("tplList").addEventListener("click", ev => {
        const el = ev.target;
        const eid = el.getAttribute && el.getAttribute("data-tpl-edit");
        const did = el.getAttribute && el.getAttribute("data-tpl-del");
        const qid = el.getAttribute && el.getAttribute("data-tpl-qr");
        if (qid) { showTemplateQr(qid, el.getAttribute("data-tpl-qr-name") || ""); return; }
        if (eid) { const tp = tplData.find(x => x.id === eid); if (tp) showTplForm(tp); return; }
        if (did) {
            if (!window.confirm(t("setTplsDeleteConfirm"))) return;
            fetch("/api/settings/templates/delete", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ pin: pin, id: did }) })
                .then(r => r.json()).then(res => { if (res.ok) loadTemplates(); else msg("tplMsg", res.error || t("setBadPin")); });
        }
    });
    $("btnTplSave").addEventListener("click", () => {
        const nm = $("tplName").value.trim(), code = $("tplCode").value.trim();
        if (!nm || !code) { msg("tplMsg", t("setTplsNameReq")); return; }
        const audio = $("tplAudio").value;
        const offered = $("tplOffered").value.split(",").map(x => x.trim()).filter(x => x);
        const body = {
            pin: pin, name: nm, hostingCode: code,
            sourceLanguage: $("tplLang").value,
            sttBackend: $("tplStt").value,
            translationBackend: $("tplTrans").value,
            audioSource: audio === "local" ? "local" : "web",
            webMicRaw: audio === "webraw",
            visibility: $("tplVis").value,
            offeredLanguages: offered
        };
        if (tplEditingId) body.id = tplEditingId;
        fetch("/api/settings/templates", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) })
            .then(jsonOrStatus).then(res => {
                if (res.ok) { $("tplForm").style.display = "none"; loadTemplates(); }
                else msg("tplMsg", res.error || t("setBadPin"));
            }).catch(() => msg("tplMsg", t("cmdFail")));
    });

    // Permanent (template-pointer) QR — the one the church prints once.
    function showTemplateQr(id, name) {
        $("qrTitle").textContent = name;
        $("qrImg").src = "/api/templates/" + encodeURIComponent(id) + "/qr";
        $("qrUrl").textContent = location.origin + "/index.html?join=" + id;
        $("qrOverlay").classList.add("visible");
    }
    $("btnQrClose").addEventListener("click", () => $("qrOverlay").classList.remove("visible"));

    // ── Bibles ──
    let bibData = null, bibPoll = null;
    function bibStopPoll() { if (bibPoll) { clearInterval(bibPoll); bibPoll = null; } }
    function bibHasActive() {
        if (!bibData || !bibData.states) return false;
        return Object.values(bibData.states).some(v => v === "downloading" || v === "converting" || v === "verifying");
    }
    $("bibCard").addEventListener("toggle", () => {
        if ($("bibCard").open) { $("bibList").innerHTML = '<div class="hint">' + t("checking") + "</div>"; bibLoad(); }
        else bibStopPoll();
    });
    $("bibSearch").addEventListener("input", renderBibles);
    function bibLoad() {
        fetch("/api/settings/bibles?" + qpin()).then(r => r.json()).then(b => {
            if (b.error) { $("bibList").innerHTML = '<div class="hint" style="color:#f44">' + esc(b.error) + "</div>"; bibStopPoll(); return; }
            bibData = b;
            renderBibles();
            if (bibHasActive()) { if (!bibPoll) bibPoll = setInterval(bibPollStates, 2500); }
            else bibStopPoll();
        }).catch(() => { $("bibList").innerHTML = '<div class="hint" style="color:#f44">' + t("netError") + "</div>"; });
    }
    function bibPollStates() {
        if (!$("bibCard").open) { bibStopPoll(); return; }
        fetch("/api/settings/bibles/status?" + qpin()).then(r => r.json()).then(st => {
            if (st.error || !bibData) { bibStopPoll(); return; }
            bibData.states = st.states || {};
            renderBibles();
            if (!bibHasActive()) { bibStopPoll(); bibLoad(); }
        }).catch(() => { });
    }
    function renderBibles() {
        if (!bibData) return;
        const stageTxt = { downloading: t("setBiblesDownloading"), converting: t("setBiblesConverting"), verifying: t("setBiblesVerifying") };
        const q = $("bibSearch").value.toLowerCase().trim();
        const rows = [];
        let total = 0;
        for (const c of bibData.catalog) {
            const st = (bibData.states && bibData.states[c.id]) || "";
            let show;
            if (q.length >= 2) {
                show = c.title.toLowerCase().includes(q) || c.langName.toLowerCase().includes(q) ||
                    c.lang.toLowerCase().includes(q) || c.id.toLowerCase().includes(q);
            } else show = c.installed || !!st;
            if (!show) continue;
            total++;
            if (rows.length >= 50) continue;
            const books = c.ot && c.nt ? "" : (c.nt ? " · NT" : (c.ot ? " · OT" : ""));
            const err = st.indexOf("error") === 0 ? st : "";
            let right;
            if (c.installed || st === "done") right = '<span style="color:#4f4;font-size:12px;white-space:nowrap">✓ ' + t("setBiblesInstalled") + "</span>";
            else if (stageTxt[st]) right = '<span style="color:#fa4;font-size:11px;white-space:nowrap">' + stageTxt[st] + "…</span>";
            else right = '<button data-bible-dl="' + esc(c.id) + '">' + (err ? t("setBiblesRetry") : t("setBiblesDownload")) + "</button>";
            rows.push('<div class="row"><div class="grow">' +
                '<div class="title">' + esc(c.title) + "</div>" +
                '<div class="sub">' + esc(c.langName) + " (" + esc(c.lang) + ")" + books + "</div>" +
                (err ? '<div class="err">' + esc(err) + "</div>" : "") +
                "</div>" + right + "</div>");
        }
        if (total > rows.length) rows.push('<div class="hint">' + fmt("setBiblesMore", total - rows.length) + "</div>");
        if (total === 0) rows.push('<div class="hint">' + (q.length >= 2 ? t("setBiblesNone") : t("setBiblesTypeToSearch")) + "</div>");
        $("bibList").innerHTML = rows.join("");
    }
    $("bibList").addEventListener("click", ev => {
        const el = ev.target;
        const id = el.getAttribute && el.getAttribute("data-bible-dl");
        if (!id) return;
        el.disabled = true;
        fetch("/api/settings/bibles/download", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ pin: pin, translationId: id }) })
            .then(r => r.json()).then(res => {
                if (res.error) { el.disabled = false; return; }
                if (!bibData.states) bibData.states = {};
                bibData.states[id] = "downloading";
                renderBibles();
                if (!bibPoll) bibPoll = setInterval(bibPollStates, 2500);
            }).catch(() => { el.disabled = false; });
    });

    // ── Raw config ──
    $("rawCard").addEventListener("toggle", () => {
        if (!$("rawCard").open) return;
        fetch("/api/settings/rawconfig?" + qpin()).then(r => r.json()).then(rc => {
            if (rc.error) { msg("rawMsg", rc.error); return; }
            $("rawText").value = rc.json || "";
        }).catch(() => msg("rawMsg", t("setRawLoadFail")));
    });
    $("btnRawSave").addEventListener("click", () => {
        const txt = $("rawText").value;
        try { JSON.parse(txt); } catch (e) { msg("rawMsg", t("setRawInvalid") + e.message); return; }
        fetch("/api/settings/rawconfig", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ pin: pin, json: txt }) })
            .then(jsonOrStatus).then(res => {
                if (!res.ok) { msg("rawMsg", res.error || t("setBadPin")); return; }
                if (res.pinCleared) { msg("rawMsg", t("setRawPinCleared")); $("bootstrapWarn").style.display = ""; }
                else msg("rawMsg", res.needsRestart ? t("setRawRestart") : t("setRawSaved"), true);
            }).catch(() => msg("rawMsg", t("cmdFail")));
    });

    // ── Log tail ──
    function loadLog() {
        fetch("/api/settings/logtail?" + qpin()).then(r => r.json()).then(lg => {
            const pre = $("logView");
            pre.textContent = (lg.lines || []).join("\n") || "(empty)";
            pre.scrollTop = pre.scrollHeight;
        }).catch(() => { });
    }
    $("logCard").addEventListener("toggle", () => { if ($("logCard").open) loadLog(); });
    $("btnLogRefresh").addEventListener("click", loadLog);

})();
