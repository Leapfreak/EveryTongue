/* Teardown verification: create a room (spawns a live-server sidecar), close
   the room, and report — the Lite log should then show the graceful-shutdown
   request followed by "stopped cleanly" instead of "exited with code -1".
     node tools/verify-teardown.js */
const http = require("http");

function req(method, path, body) {
    return new Promise((resolve, reject) => {
        const r = http.request({ host: "localhost", port: 5981, path, method, headers: { "Content-Type": "application/json" } }, res => {
            let d = "";
            res.on("data", c => d += c);
            res.on("end", () => resolve({ status: res.statusCode, body: d }));
        });
        r.on("error", reject);
        if (body) r.write(JSON.stringify(body));
        r.end();
    });
}

(async () => {
    const create = await req("POST", "/api/rooms/from-template", { templateId: "b53079f7", hostingCode: "host77" });
    console.log("create:", create.status, create.body.slice(0, 200));
    const room = JSON.parse(create.body);
    await new Promise(r => setTimeout(r, 12000)); // let the sidecar fully start
    const del = await req("DELETE", "/api/rooms/" + room.id + "?clientId=" + encodeURIComponent(room.hostToken));
    console.log("delete:", del.status, del.body.slice(0, 200));
    await new Promise(r => setTimeout(r, 8000)); // let teardown complete + log flush
    console.log("done — check lite log for teardown lines");
})();
