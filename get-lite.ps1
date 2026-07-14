# EveryTongue Lite — one-command install/update (Windows PowerShell).
#
#   irm https://raw.githubusercontent.com/Leapfreak/EveryTongue/main/get-lite.ps1 | iex
#
# Idempotent: run it again any time to update to the latest image.
# Config/keys/certificate/logs persist in %USERPROFILE%\everytongue-lite.

# NOT "Stop": docker writes routine notices to stderr (e.g. "No such
# container" on first install), which Stop would turn into script aborts.
# Failures are detected via $LASTEXITCODE instead.
$ErrorActionPreference = "Continue"

$Image = "ghcr.io/leapfreak/everytongue-lite:latest"
$Name = "everytongue-lite"
$ConfigDir = Join-Path $env:USERPROFILE "everytongue-lite"

# ── Docker present and running? ─────────────────────────────────────────
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker is not installed."
    Write-Host "  Install Docker Desktop from https://www.docker.com/products/docker-desktop/"
    return
}
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker is installed but not running - start Docker Desktop and re-run this script."
    return
}

# ── LAN IP for phone-facing URLs/QR codes (container can't see it) ──────
$LanIp = ""
try {
    $LanIp = (Get-NetIPConfiguration |
        Where-Object { $_.IPv4DefaultGateway -ne $null -and $_.NetAdapter.Status -eq "Up" } |
        Select-Object -First 1).IPv4Address.IPAddress
} catch { }

New-Item -ItemType Directory -Force -Path $ConfigDir | Out-Null

Write-Host "Pulling the latest EveryTongue Lite image..."
docker pull $Image
if ($LASTEXITCODE -ne 0) { Write-Host "Image pull failed - check your internet connection."; return }

# Replace any existing container (config lives on the volume, nothing is lost)
docker rm -f $Name 2>&1 | Out-Null

$RunArgs = @(
    "run", "-d", "--name", $Name, "--restart", "unless-stopped",
    "-p", "5080:5080", "-p", "5081:5081",
    "-v", "${ConfigDir}:/config"
)
if ($LanIp) { $RunArgs += @("-e", "EVERYTONGUE_PUBLIC_HOST=${LanIp}:5081") }
$RunArgs += $Image

docker @RunArgs | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host "Container failed to start - is something else using ports 5080/5081?"; return }

$ShownHost = if ($LanIp) { $LanIp } else { "<this-machine's-LAN-IP>" }
Write-Host ""
Write-Host "EveryTongue Lite is running."
Write-Host ""
Write-Host "  Phones / browser:  https://${ShownHost}:5081"
Write-Host "  Lobby:             https://${ShownHost}:5081/lobby.html"
Write-Host ""
Write-Host "  First time: accept the one-time certificate warning, tap"
Write-Host "  'Administrator' (PIN 1234), open Settings, choose your engines,"
Write-Host "  paste YOUR API keys, and change the PIN."
Write-Host ""
Write-Host "  Config persists in: $ConfigDir"
Write-Host "  Update any time by re-running this script."
