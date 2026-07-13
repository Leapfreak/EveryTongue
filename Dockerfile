# EveryTongue Lite — headless online-only server (cloud STT + cloud translation).
#
#   docker build -t everytongue-lite .
#   docker run -d --name everytongue -p 5080:5080 -p 5081:5081 \
#     -v ./et-config:/config everytongue-lite
#
# Then open https://<host>:5081 — tap "Administrator" on the language screen
# (default PIN 1234), enter your engine API keys in Settings, and change the PIN.
# Config, HTTPS cert, and logs all live on the /config volume.

# ── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# Framework-dependent publish (runs on the aspnet base image, any architecture).
# wwwroot, live-server, translate-server, locales all flow in as Content.
RUN dotnet publish EveryTongue.Lite/EveryTongue.Lite.vbproj -c Release -o /app/publish

# ── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
# python3 runs the live-server sidecar (Speechmatics streaming, web-mic ingest);
# libportaudio2 satisfies sounddevice's import (no capture happens in-container —
# the web-mic broadcast is the audio source); ffmpeg decodes conversation-room PTT.
RUN apt-get update && apt-get install -y --no-install-recommends \
        python3 python3-venv libportaudio2 ffmpeg \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Online-only python deps (no torch, no models — see requirements-lite.txt).
# A venv keeps Debian's externally-managed python happy; putting it first on
# PATH makes FindPython()'s "python3" probe resolve to it.
RUN python3 -m venv /opt/etpy \
    && /opt/etpy/bin/pip install --no-cache-dir -r live-server/requirements-lite.txt
ENV PATH="/opt/etpy/bin:${PATH}"

# Config, HTTPS certificate, and logs persist here across restarts/updates.
ENV EVERYTONGUE_CONFIG_DIR=/config
VOLUME ["/config"]

EXPOSE 5080 5081
ENTRYPOINT ["dotnet", "EveryTongue.Lite.dll"]
