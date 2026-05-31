/* Every Tongue — Lobby
   Room listing, creation, and QR code sharing.
   ES6 is fine for new files. */

(function () {
    "use strict";

    // ── DOM refs ──
    const tabs = document.querySelectorAll(".tab");
    const panels = {
        join: document.getElementById("panel-join"),
        create: document.getElementById("panel-create")
    };
    const roomList = document.getElementById("room-list");
    const myRoomsSection = document.getElementById("my-rooms");
    const myRoomList = document.getElementById("my-room-list");
    const emptyState = document.getElementById("empty-state");
    const roomNameInput = document.getElementById("room-name");
    const btnCreate = document.getElementById("btn-create");
    const typeOptions = document.querySelectorAll(".type-option");
    const togglePrivate = document.getElementById("toggle-private");

    // QR overlay
    const qrOverlay = document.getElementById("qr-overlay");
    const qrRoomName = document.getElementById("qr-room-name");
    const qrImage = document.getElementById("qr-image");
    const qrUrl = document.getElementById("qr-url");
    const btnJoinOwn = document.getElementById("btn-join-own");
    const btnCloseQr = document.getElementById("btn-close-qr");

    // ── State ──
    let selectedType = "conference";
    let isPrivate = false;
    let createdRoom = null;   // { id, name, type, visibility, hostToken }
    let refreshTimer = null;

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
    async function renderMyRooms() {
        const rooms = getMyRooms();
        if (rooms.length === 0) {
            myRoomsSection.style.display = "none";
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
            myRoomsSection.style.display = "none";
            return;
        }
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
    }

    // ── Tabs ──
    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            const target = tab.dataset.tab;
            tabs.forEach(function (t) { t.classList.remove("active"); });
            tab.classList.add("active");
            Object.values(panels).forEach(function (p) { p.classList.remove("active"); });
            if (panels[target]) panels[target].classList.add("active");
        });
    });

    // ── Type picker ──
    typeOptions.forEach(function (opt) {
        opt.addEventListener("click", function () {
            typeOptions.forEach(function (o) { o.classList.remove("selected"); });
            opt.classList.add("selected");
            selectedType = opt.dataset.type;
            // Conversation defaults to private
            if (selectedType === "conversation" && !isPrivate) {
                isPrivate = true;
                togglePrivate.classList.add("on");
            }
        });
    });

    // ── Private toggle ──
    togglePrivate.addEventListener("click", function () {
        isPrivate = !isPrivate;
        togglePrivate.classList.toggle("on", isPrivate);
    });

    // ── Create room ──
    btnCreate.addEventListener("click", async function () {
        const name = roomNameInput.value.trim();
        if (!name) {
            roomNameInput.focus();
            return;
        }
        btnCreate.disabled = true;
        try {
            const res = await fetch("/api/rooms", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    name: name,
                    type: selectedType,
                    visibility: isPrivate ? "private" : "public"
                })
            });
            if (!res.ok) throw new Error("Server returned " + res.status);
            createdRoom = await res.json();
            addMyRoom(createdRoom);
            showQrOverlay(createdRoom);
            roomNameInput.value = "";
            loadRooms();
            renderMyRooms();
        } catch (err) {
            alert("Failed to create room: " + err.message);
        } finally {
            btnCreate.disabled = false;
        }
    });

    // ── QR overlay ──
    function showQrOverlay(room) {
        qrRoomName.textContent = room.name;
        // Build the join URL from current page origin
        const joinUrl = location.origin + "/index.html?room=" + encodeURIComponent(room.id);
        qrImage.src = "/api/rooms/" + encodeURIComponent(room.id) + "/qr";
        qrUrl.textContent = joinUrl;
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
            emptyState.style.display = "block";
            return;
        }
        emptyState.style.display = "none";
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
})();
