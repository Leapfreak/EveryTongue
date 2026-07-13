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

# Online-only python deps (see requirements-lite.txt). A venv keeps Debian's
# externally-managed python happy; putting it first on PATH makes FindPython()'s
# "python3" probe resolve to it.
RUN python3 -m venv /opt/etpy \
    && /opt/etpy/bin/pip install --no-cache-dir -r live-server/requirements-lite.txt
ENV PATH="/opt/etpy/bin:${PATH}"

# SaT sentence segmentation (same pinned versions as the desktop's SaT component).
# CPU-only torch from the pytorch index — the default linux torch drags in ~2.5GB
# of CUDA libraries the container can never use. Costs ~1GB; buys the
# buffer-to-pause → SaT split → per-sentence translation quality pipeline.
# The sat-3l-sm model downloads on first use into HF_HOME on the /config volume
# (survives container replacement; ~1 minute once).
RUN /opt/etpy/bin/pip install --no-cache-dir torch --index-url https://download.pytorch.org/whl/cpu \
    && /opt/etpy/bin/pip install --no-cache-dir wtpsplit==2.2.1 transformers==5.13.0 tokenizers==0.22.2
ENV HF_HOME=/config/sat-cache

# Config, HTTPS certificate, and logs persist here across restarts/updates.
ENV EVERYTONGUE_CONFIG_DIR=/config
VOLUME ["/config"]

EXPOSE 5080 5081
ENTRYPOINT ["dotnet", "EveryTongue.Lite.dll"]
