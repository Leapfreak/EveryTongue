# Stage the updater app zip for a release — mirrors setup.iss [Files] exactly
# (same inclusions, same Excludes), zip root = {app} layout (the in-app updater
# Expand-Archive's it straight over the install dir).
#   powershell -NoProfile -File tools\stage-app-zip.ps1 -Version 2.7.19
param([Parameter(Mandatory = $true)][string]$Version)

$pub = Join-Path $PSScriptRoot "..\EveryTongue\bin\Publish"
$stage = Join-Path $env:TEMP "et-app-stage"
$zip = Join-Path $env:TEMP "EveryTongue_App_v$Version.zip"

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory $stage | Out-Null

# Root files (setup.iss lines 52-63)
Copy-Item (Join-Path $PSScriptRoot "..\LICENSE") $stage
Copy-Item (Join-Path $PSScriptRoot "..\THIRD_PARTY_NOTICES.md") $stage
foreach ($f in 'EveryTongue.exe', 'EveryTongue.dll', 'EveryTongue.deps.json', 'EveryTongue.runtimeconfig.json') {
    Copy-Item (Join-Path $pub $f) $stage
}
$exclDll = 'cublas*.dll', 'cublasLt*.dll', 'cudart*.dll', 'cudnn*.dll', 'cufft*.dll', 'nvrtc*.dll', 'zlibwapi.dll', 'AWSSDK.*.dll'
Get-ChildItem (Join-Path $pub '*.dll') | Where-Object {
    $n = $_.Name
    -not ($exclDll | Where-Object { $n -like $_ })
} | Copy-Item -Destination $stage
foreach ($f in 'checksums.json', 'component-versions.json') {
    $p = Join-Path $pub $f
    if (Test-Path $p) { Copy-Item $p $stage }
}

# Runtimes (lines 66-70)
foreach ($d in 'runtimes\win-arm64\native', 'runtimes\win-x64\native', 'runtimes\win-x86\native', 'runtimes\win-arm\native', 'runtimes\win\lib\net8.0') {
    $src = Join-Path $pub $d
    if (Test-Path $src) {
        New-Item -ItemType Directory (Join-Path $stage $d) -Force | Out-Null
        Copy-Item (Join-Path $src '*') (Join-Path $stage $d)
    }
}

# Recursive content dirs (lines 73, 76)
foreach ($d in 'wwwroot', 'Help') {
    $src = Join-Path $pub $d
    if (Test-Path $src) { Copy-Item $src (Join-Path $stage $d) -Recurse }
}

# Flat content dirs (lines 79, 82)
foreach ($d in 'test-data', 'locales') {
    $src = Join-Path $pub $d
    if (Test-Path $src) {
        New-Item -ItemType Directory (Join-Path $stage $d) -Force | Out-Null
        Get-ChildItem $src -File | Copy-Item -Destination (Join-Path $stage $d)
    }
}

# Python sidecars (lines 87-89): translate/mms flat, live-server recursive, no *.pyc/*.log
foreach ($d in 'translate-server', 'mms-tts-server') {
    $src = Join-Path $pub $d
    if (Test-Path $src) {
        New-Item -ItemType Directory (Join-Path $stage $d) -Force | Out-Null
        Get-ChildItem $src -File | Where-Object { $_.Extension -notin '.pyc', '.log' } |
            Copy-Item -Destination (Join-Path $stage $d)
    }
}
$liveSrc = Join-Path $pub 'live-server'
if (Test-Path $liveSrc) {
    Copy-Item $liveSrc (Join-Path $stage 'live-server') -Recurse
    Get-ChildItem (Join-Path $stage 'live-server') -Recurse -File |
        Where-Object { $_.Extension -in '.pyc', '.log' } | Remove-Item
    Get-ChildItem (Join-Path $stage 'live-server') -Recurse -Directory -Filter '__pycache__' |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "Staged: $zip ($mb MB)"
