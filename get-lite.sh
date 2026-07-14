#!/bin/sh
# EveryTongue Lite — one-command install/update (macOS + Linux).
#
#   curl -fsSL https://raw.githubusercontent.com/Leapfreak/EveryTongue/main/get-lite.sh | sh
#
# Idempotent: run it again any time to update to the latest image.
# Config/keys/certificate/logs persist in ~/everytongue-lite across updates.

set -e

IMAGE="ghcr.io/leapfreak/everytongue-lite:latest"
NAME="everytongue-lite"
CONFIG_DIR="$HOME/everytongue-lite"

# ── Docker present and running? ─────────────────────────────────────────
if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is not installed."
    echo "  macOS/Windows: install Docker Desktop from https://www.docker.com/products/docker-desktop/"
    echo "  Linux:         https://docs.docker.com/engine/install/"
    exit 1
fi
if ! docker info >/dev/null 2>&1; then
    echo "Docker is installed but not running — start Docker Desktop (or the docker service) and re-run this script."
    exit 1
fi

# ── LAN IP for phone-facing URLs/QR codes (container can't see it) ──────
LAN_IP=""
if command -v ipconfig >/dev/null 2>&1; then       # macOS
    for IF in en0 en1 en2; do
        LAN_IP=$(ipconfig getifaddr "$IF" 2>/dev/null || true)
        [ -n "$LAN_IP" ] && break
    done
fi
if [ -z "$LAN_IP" ] && command -v hostname >/dev/null 2>&1; then   # Linux
    LAN_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || true)
fi
if [ -z "$LAN_IP" ] && command -v ip >/dev/null 2>&1; then
    LAN_IP=$(ip route get 1.1.1.1 2>/dev/null | awk '{for(i=1;i<=NF;i++) if ($i=="src") print $(i+1)}' | head -n1 || true)
fi

mkdir -p "$CONFIG_DIR"

echo "Pulling the latest EveryTongue Lite image..."
docker pull "$IMAGE"

# Replace any existing container (config lives on the volume, nothing is lost)
docker rm -f "$NAME" >/dev/null 2>&1 || true

PUBLIC_HOST_ARGS=""
if [ -n "$LAN_IP" ]; then
    PUBLIC_HOST_ARGS="-e EVERYTONGUE_PUBLIC_HOST=$LAN_IP:5081"
fi

# shellcheck disable=SC2086
docker run -d --name "$NAME" --restart unless-stopped \
    -p 5080:5080 -p 5081:5081 \
    $PUBLIC_HOST_ARGS \
    -v "$CONFIG_DIR:/config" \
    "$IMAGE" >/dev/null

SHOWN_HOST="${LAN_IP:-<this-machine's-LAN-IP>}"
echo ""
echo "EveryTongue Lite is running."
echo ""
echo "  Phones / browser:  https://$SHOWN_HOST:5081"
echo "  Lobby:             https://$SHOWN_HOST:5081/lobby.html"
echo ""
echo "  First time: accept the one-time certificate warning, tap"
echo "  'Administrator' (PIN 1234), open Settings, choose your engines,"
echo "  paste YOUR API keys, and change the PIN."
echo ""
echo "  Config persists in: $CONFIG_DIR"
echo "  Update any time by re-running this script."
