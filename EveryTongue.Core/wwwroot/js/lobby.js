/* Every Tongue — Lobby
   Single-page room listing + creation.
   ES6 is fine for new files. */

(function () {
    "use strict";

    // ── DOM refs ──
    const roomsSection = document.getElementById("rooms-section");
    const myRoomsSection = document.getElementById("my-rooms");
    const myRoomList = document.getElementById("my-room-list");
    const publicRoomsSection = document.getElementById("public-rooms");
    const roomList = document.getElementById("room-list");
    const btnCreate = document.getElementById("btn-create");
    const togglePrivate = document.getElementById("toggle-private");

    // Conference hosting refs
    const conferenceSection = document.getElementById("conference-section");
    const templateSelect = document.getElementById("template-select");
    const hostingCodeInput = document.getElementById("hosting-code");
    const conferenceError = document.getElementById("conference-error");
    const btnCreateConference = document.getElementById("btn-create-conference");

    // ── Volunteer tier gate ─────────────────────────────────────────────
    // When the server has a CreatorCode, guests see join-only; the creation
    // tools unlock via the "Host tools" link (code kept for the session).
    // Endpoints enforce the code server-side; this is only the door.
    const creatorTools = document.getElementById("creator-tools");
    const hostToolsGate = document.getElementById("host-tools-gate");
    const hostToolsLink = document.getElementById("host-tools-link");
    const hostToolsForm = document.getElementById("host-tools-form");
    const creatorCodeInput = document.getElementById("creator-code-input");
    const creatorCodeGo = document.getElementById("creator-code-go");
    const creatorCodeMsg = document.getElementById("creator-code-msg");

    function creatorCode() { return sessionStorage.getItem("creatorCode") || ""; }

    fetch("/api/config").then(r => r.json()).then(cfg => {
        if (!cfg.creatorCodeRequired) return;              // open mode — leave everything visible
        if (creatorCode()) {
            // already unlocked this session — re-verify silently (code may have changed)
            fetch("/api/creator/verify?code=" + encodeURIComponent(creatorCode()))
                .then(r => r.json()).then(v => { if (!v.ok) lockCreatorTools(); });
            return;
        }
        lockCreatorTools();
    }).catch(() => { });

    function lockCreatorTools() {
        sessionStorage.removeItem("creatorCode");
        creatorTools.style.display = "none";
        hostToolsGate.style.display = "block";
    }

    hostToolsLink.addEventListener("click", function (e) {
        e.preventDefault();
        hostToolsForm.style.display = hostToolsForm.style.display === "none" ? "block" : "none";
        if (hostToolsForm.style.display === "block") creatorCodeInput.focus();
    });
    creatorCodeGo.addEventListener("click", tryUnlock);
    creatorCodeInput.addEventListener("keydown", function (e) { if (e.key === "Enter") tryUnlock(); });
    function tryUnlock() {
        const code = creatorCodeInput.value.trim();
        if (!code) return;
        fetch("/api/creator/verify?code=" + encodeURIComponent(code)).then(r => r.json()).then(v => {
            if (v.ok) {
                sessionStorage.setItem("creatorCode", code);
                hostToolsGate.style.display = "none";
                creatorTools.style.display = "";
            } else {
                creatorCodeMsg.textContent = "Invalid code";
            }
        }).catch(() => { creatorCodeMsg.textContent = "Could not verify"; });
    }

    // QR overlay
    const qrOverlay = document.getElementById("qr-overlay");
    const qrRoomName = document.getElementById("qr-room-name");
    const qrImage = document.getElementById("qr-image");
    const qrUrl = document.getElementById("qr-url");
    const btnJoinOwn = document.getElementById("btn-join-own");
    const btnCloseQr = document.getElementById("btn-close-qr");

    // ── State ──
    let isPrivate = true;
    let createdRoom = null;
    let refreshTimer = null;
    let hasMyRooms = false;
    let hasPublicRooms = false;

    // ── "Your Rooms" localStorage helpers ──
    function getMyRooms() {
        try {
            return JSON.parse(localStorage.getItem("myRooms") || "[]");
        } catch (e) { return []; }
    }
    function saveMyRooms(rooms) {
        localStorage.setItem("myRooms", JSON.stringify(rooms));
    }
    function addMyRoom(room) {
        const rooms = getMyRooms().filter(function (r) { return r.id !== room.id; });
        rooms.unshift({ id: room.id, name: room.name, type: room.type, hostToken: room.hostToken });
        saveMyRooms(rooms);
    }

    function updateRoomsVisibility() {
        roomsSection.style.display = (hasMyRooms || hasPublicRooms) ? "block" : "none";
    }

    async function renderMyRooms() {
        const rooms = getMyRooms();
        if (rooms.length === 0) {
            hasMyRooms = false;
            myRoomsSection.style.display = "none";
            updateRoomsVisibility();
            return;
        }
        // Verify which rooms are still active
        const active = [];
        for (const r of rooms) {
            try {
                const res = await fetch("/api/rooms/" + encodeURIComponent(r.id));
                if (res.ok) active.push(r);
            } catch (e) { /* remove dead rooms */ }
        }
        saveMyRooms(active);
        if (active.length === 0) {
            hasMyRooms = false;
            myRoomsSection.style.display = "none";
            updateRoomsVisibility();
            return;
        }
        hasMyRooms = true;
        myRoomsSection.style.display = "block";
        myRoomList.innerHTML = "";
        active.forEach(function (room) {
            const li = document.createElement("li");
            li.className = "room-item";
            li.style.borderLeft = "3px solid #7c9cf7";
            li.innerHTML =
                '<div class="room-name">' + escapeHtml(room.name) +
                '<span class="room-type">' + escapeHtml(room.type) + '</span></div>' +
                '<div class="room-meta"><span style="color:#7c9cf7">Created by you</span></div>';
            li.addEventListener("click", function () {
                location.href = "/index.html?room=" + encodeURIComponent(room.id);
            });
            myRoomList.appendChild(li);
        });
        updateRoomsVisibility();
    }

    // ── Private toggle ──
    togglePrivate.addEventListener("click", function () {
        isPrivate = !isPrivate;
        togglePrivate.classList.toggle("on", isPrivate);
    });

    // ── Create conversation room ──
    btnCreate.addEventListener("click", async function () {
        var suffix = Math.random().toString(36).substring(2, 6);
        var name = "Conversation " + suffix;
        btnCreate.disabled = true;
        try {
            var engineSel = document.getElementById("conv-engine");
            var engineVal = engineSel ? engineSel.value : "";
            var res = await fetch("/api/rooms", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    name: name,
                    type: "conversation",
                    visibility: isPrivate ? "private" : "public",
                    translationEngine: engineVal,
                    creatorCode: creatorCode()
                })
            });
            if (!res.ok) throw new Error("Server returned " + res.status);
            createdRoom = await res.json();
            addMyRoom(createdRoom);
            location.href = "/index.html?room=" + encodeURIComponent(createdRoom.id);
        } catch (err) {
            alert("Failed to create room: " + err.message);
        } finally {
            btnCreate.disabled = false;
        }
    });

    // ── Conference hosting (two-step: select template → enter code) ──

    const hostingCodeGroup = document.getElementById("hosting-code-group");
    let conferenceStep = 0; // 0 = pick template, 1 = enter code & submit

    async function loadTemplates() {
        try {
            var res = await fetch("/api/templates");
            if (!res.ok) return;
            var templates = await res.json();
            if (!templates || templates.length === 0) {
                conferenceSection.style.display = "none";
                return;
            }
            conferenceSection.style.display = "block";
            templateSelect.innerHTML = "";
            templates.forEach(function (t) {
                var opt = document.createElement("option");
                opt.value = t.id;
                opt.textContent = t.name;
                templateSelect.appendChild(opt);
            });
            resetConferenceStep();
        } catch (e) {
            conferenceSection.style.display = "none";
        }
    }

    function resetConferenceStep() {
        conferenceStep = 0;
        hostingCodeGroup.style.display = "none";
        hostingCodeInput.value = "";
        conferenceError.style.display = "none";
        btnCreateConference.disabled = !(templateSelect.value && templateSelect.value !== "");
    }

    function updateConferenceButton() {
        var hasTemplate = templateSelect.value && templateSelect.value !== "";
        if (conferenceStep === 0) {
            btnCreateConference.disabled = !hasTemplate;
        } else {
            var hasCode = hostingCodeInput.value.trim().length > 0;
            btnCreateConference.disabled = !(hasTemplate && hasCode);
        }
    }

    templateSelect.addEventListener("change", function () {
        resetConferenceStep();
    });
    hostingCodeInput.addEventListener("input", function () {
        conferenceError.style.display = "none";
        updateConferenceButton();
    });

    btnCreateConference.addEventListener("click", async function () {
        var templateId = templateSelect.value;
        if (!templateId) return;

        // Step 0: reveal hosting code field
        if (conferenceStep === 0) {
            conferenceStep = 1;
            hostingCodeGroup.style.display = "block";
            hostingCodeInput.focus();
            updateConferenceButton();
            return;
        }

        // Step 1: submit with hosting code
        var code = hostingCodeInput.value.trim();
        if (!code) return;

        btnCreateConference.disabled = true;
        conferenceError.style.display = "none";

        try {
            var res = await fetch("/api/rooms/from-template", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    templateId: templateId,
                    hostingCode: code,
                    hostClientId: ""
                })
            });

            if (res.status === 403) {
                conferenceError.textContent = "Invalid hosting code";
                conferenceError.style.display = "block";
                return;
            }
            if (!res.ok) throw new Error("Server returned " + res.status);

            createdRoom = await res.json();
            addMyRoom(createdRoom);
            showQrOverlay(createdRoom);
            loadRooms();
            renderMyRooms();
            resetConferenceStep();
        } catch (err) {
            conferenceError.textContent = "Failed: " + err.message;
            conferenceError.style.display = "block";
        } finally {
            btnCreateConference.disabled = false;
            updateConferenceButton();
        }
    });

    // ── QR overlay ──
    // Share link must be PHONE-reachable — prefer the server-reported public
    // host over location.origin (which is "localhost" when the operator
    // browses on the server machine and would point each phone at itself).
    let serverPublicHost = "";
    fetch("/api/config").then(r => r.json()).then(cfg => { serverPublicHost = cfg.publicHost || ""; }).catch(() => { });
    function showQrOverlay(room) {
        qrRoomName.textContent = room.name;
        const base = serverPublicHost ? location.protocol + "//" + serverPublicHost : location.origin;
        const joinUrl = base + "/index.html?room=" + encodeURIComponent(room.id);
        qrImage.src = "/api/rooms/" + encodeURIComponent(room.id) + "/qr";
        qrUrl.innerHTML = '<a href="' + joinUrl + '" style="color:#7c9cf7;text-decoration:underline">' + joinUrl + '</a>';
        createdRoom = room;
        qrOverlay.classList.add("visible");
    }

    btnJoinOwn.addEventListener("click", function () {
        if (createdRoom) {
            location.href = "/index.html?room=" + encodeURIComponent(createdRoom.id);
        }
    });

    btnCloseQr.addEventListener("click", function () {
        qrOverlay.classList.remove("visible");
    });

    // ── Room list ──
    async function loadRooms() {
        try {
            const res = await fetch("/api/rooms");
            if (!res.ok) return;
            const rooms = await res.json();
            renderRooms(rooms);
        } catch (err) {
            // Network error — leave list as-is
        }
    }

    function renderRooms(rooms) {
        roomList.innerHTML = "";
        // Filter out rooms already shown in "Your Rooms"
        var myIds = {};
        getMyRooms().forEach(function (r) { myIds[r.id] = true; });
        rooms = (rooms || []).filter(function (r) { return !myIds[r.id]; });
        if (rooms.length === 0) {
            hasPublicRooms = false;
            publicRoomsSection.style.display = "none";
            updateRoomsVisibility();
            return;
        }
        hasPublicRooms = true;
        publicRoomsSection.style.display = "block";
        rooms.forEach(function (room) {
            const li = document.createElement("li");
            li.className = "room-item";
            li.innerHTML =
                '<div class="room-name">' + escapeHtml(room.name) +
                '<span class="room-type">' + escapeHtml(room.type) + '</span></div>' +
                '<div class="room-meta">' +
                '<span>' + room.clients + ' connected</span>' +
                '</div>';
            li.addEventListener("click", function () {
                location.href = "/index.html?room=" + encodeURIComponent(room.id);
            });
            roomList.appendChild(li);
        });
        updateRoomsVisibility();
    }

    function escapeHtml(str) {
        if (!str) return "";
        return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    // ── Auto-refresh ──
    function startAutoRefresh() {
        loadRooms();
        refreshTimer = setInterval(loadRooms, 5000);
    }

    // ── Populate the conversation translation-engine dropdown ──
    async function loadEngines() {
        var sel = document.getElementById("conv-engine");
        if (!sel) return;
        try {
            var res = await fetch("/api/translation-engines");
            if (!res.ok) return;
            var engines = await res.json();
            for (var i = 0; i < engines.length; i++) {
                var opt = document.createElement("option");
                opt.value = engines[i].key;
                opt.textContent = engines[i].name;
                sel.appendChild(opt);
            }
        } catch (err) { /* leave just the Default option */ }
    }

    // ── Init ──
    renderMyRooms();
    startAutoRefresh();
    loadTemplates();
    loadEngines();
})();
