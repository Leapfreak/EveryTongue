/* Reproduce the "room receives no subtitles" report end-to-end with scripted
   WS clients: create a web-mic conference room (Speechmatics + cloud
   translation), stream real speech PCM as the host, and log every message a
   guest receives. Decisive for the server half of the pipeline.
     EVERYTONGUE_CONFIG_DIR=<dir with keys> dotnet EveryTongue.Lite.dll
     node tools/repro-room-subtitles.js */
const BASE = "http://localhost:5080";
const WSBASE = "ws://localhost:5080";
const PIN = "1234";
const fs = require("fs");
const path = require("path");

const speechRaw = fs.readFileSync(path.join(process.env.TEMP, "et-repro", "speech.raw"));

async function j(method, url, body) {
    const res = await fetch(BASE + url, {
        method,
        headers: body ? { "Content-Type": "application/json" } : {},
        body: body ? JSON.stringify(body) : undefined
    });
    let data = null;
    try { data = await res.json(); } catch (e) { }
    return { status: res.status, data };
}

function wsClient(name, roomId, language) {
    const ws = new WebSocket(WSBASE + "/ws?room=" + roomId);
    ws.binaryType = "arraybuffer";
    const state = { ws, name, clientId: "", counts: {}, samples: [] };
    ws.onmessage = ev => {
        if (typeof ev.data !== "string") return;
        let msg; try { msg = JSON.parse(ev.data); } catch (e) { return; }
        state.counts[msg.type] = (state.counts[msg.type] || 0) + 1;
        if (msg.type === "welcome") {
            state.clientId = msg.clientId;
            ws.send(JSON.stringify({ type: "setLanguage", language: language, lastId: 0 }));
        }
        if ((msg.type === "commit" || msg.type === "update") && state.samples.length < 8) {
            state.samples.push(msg.type + ": [" + (msg.lang || "") + "] " + String(msg.text || "").slice(0, 80));
        }
    };
    ws.onerror = e => console.log(name + " WS error");
    return state;
}

function waitFor(cond, timeoutMs, label) {
    return new Promise((resolve, reject) => {
        const t0 = Date.now();
        const iv = setInterval(() => {
            if (cond()) { clearInterval(iv); resolve(); }
            else if (Date.now() - t0 > timeoutMs) { clearInterval(iv); reject(new Error("timeout: " + label)); }
        }, 200);
    });
}

(async () => {
    // 1. Template (web-mic, speechmatics, deepl) — mirrors the field setup.
    const tpl = await j("POST", "/api/settings/templates", {
        pin: PIN, name: "Repro Svc", hostingCode: "repro1", sourceLanguage: "en",
        sttBackend: "speechmatics", translationBackend: "deepl", audioSource: "web", visibility: "public"
    });
    console.log("template:", tpl.status, JSON.stringify(tpl.data));
    const tplId = tpl.data.id;

    // 2. Room from template.
    const room = await j("POST", "/api/rooms/from-template", { templateId: tplId, hostingCode: "repro1", hostClientId: "" });
    console.log("room:", room.status, JSON.stringify(room.data));
    const roomId = room.data.id, hostToken = room.data.hostToken;

    // 3. Guest first (Spanish), then host.
    const guest = wsClient("guest", roomId, "spa_Latn");
    const host = wsClient("host", roomId, "");
    await waitFor(() => guest.clientId && host.clientId, 10000, "welcome");
    console.log("clients connected: guest=" + guest.clientId.slice(0, 8) + " host=" + host.clientId.slice(0, 8));

    // 4. Claim host, start broadcast.
    const claim = await j("POST", "/api/rooms/" + roomId + "/claim-host", { hostToken, clientId: host.clientId });
    console.log("claim-host:", claim.status, JSON.stringify(claim.data));
    host.ws.send(JSON.stringify({ type: "broadcastStart" }));
    await new Promise(r => setTimeout(r, 3000)); // readiness

    // 5. Stream: ETMC header + 3200-byte (100ms) PCM frames.
    host.ws.send(new Uint8Array([0x45, 0x54, 0x4D, 0x43, 1, 0, 0, 0]).buffer);
    let off = 0;
    console.log("streaming " + Math.round(speechRaw.length / 32000) + "s of speech...");
    await new Promise(resolve => {
        const iv = setInterval(() => {
            if (off >= speechRaw.length || off >= 45 * 32000) { clearInterval(iv); resolve(); return; }
            const frame = speechRaw.subarray(off, off + 3200);
            off += 3200;
            if (host.ws.readyState === 1) host.ws.send(frame.buffer.slice(frame.byteOffset, frame.byteOffset + frame.byteLength));
        }, 100);
    });

    // 6. Grace for trailing commits + translation.
    await new Promise(r => setTimeout(r, 12000));

    console.log("\n=== GUEST received ===");
    console.log(JSON.stringify(guest.counts));
    guest.samples.forEach(s => console.log("  " + s));
    console.log("=== HOST received ===");
    console.log(JSON.stringify(host.counts));
    host.samples.forEach(s => console.log("  " + s));

    const verdict = (guest.counts.commit || 0) > 0 ? "SERVER DELIVERS — bug is client-side rendering"
        : "GUEST GOT NO COMMITS — server-side delivery bug CONFIRMED";
    console.log("\nVERDICT: " + verdict);
    process.exit(0);
})().catch(e => { console.error("repro failed:", e); process.exit(1); });
