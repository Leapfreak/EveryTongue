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
            var res = await fetch("/api/rooms", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    name: name,
                    type: "conversation",
                    visibility: isPrivate ? "private" : "public"
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

    // ── Conference hosting ──

    async function loadTemplates() {
        try {
            var res = await fetch("/api/templates");
            if (!res.ok) return;
            var templates = await res.json();
            if (!templates || templates.length === 0) {
                // No templates configured — hide conference section
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
            updateConferenceButton();
        } catch (e) {
            conferenceSection.style.display = "none";
        }
    }

    function updateConferenceButton() {
        var hasTemplate = templateSelect.value && templateSelect.value !== "";
        var hasCode = hostingCodeInput.value.trim().length > 0;
        btnCreateConference.disabled = !(hasTemplate && hasCode);
    }

    templateSelect.addEventListener("change", updateConferenceButton);
    hostingCodeInput.addEventListener("input", function () {
        conferenceError.style.display = "none";
        updateConferenceButton();
    });

    btnCreateConference.addEventListener("click", async function () {
        var templateId = templateSelect.value;
        var code = hostingCodeInput.value.trim();
        if (!templateId || !code) return;

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
            hostingCodeInput.value = "";
        } catch (err) {
            conferenceError.textContent = "Failed: " + err.message;
            conferenceError.style.display = "block";
        } finally {
            btnCreateConference.disabled = false;
            updateConferenceButton();
        }
    });

    // ── QR overlay ──
    function showQrOverlay(room) {
        qrRoomName.textContent = room.name;
        const joinUrl = location.origin + "/index.html?room=" + encodeURIComponent(room.id);
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
        if (!rooms || rooms.length === 0) {
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

    // ── Init ──
    renderMyRooms();
    startAutoRefresh();
    loadTemplates();
})();
