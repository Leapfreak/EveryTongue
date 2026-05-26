# Agape Deployment Feature Plan

Implementation plan for making Transcription Tools field-deployable by a non-technical international organisation. Features ordered by priority — each section covers what exists today, what needs to change, and how to build it.

---

## Table of Contents

| # | Feature | Status | Phase |
|---|---------|--------|-------|
| [1](#1-qr-code-connection) | QR Code Connection | New | 1 |
| [2](#2-setup-wizard--event-setup-mode) | Setup Wizard — Event Setup Mode | Improve | 2 |
| [3](#3-audio-level-monitor) | Audio Level Monitor | New | 1 |
| [4](#4-diagnostic-bundle--remote-support) | Diagnostic Bundle / Remote Support | New | 1 |
| [5](#5-glossary-management--simplified-for-non-technical-users) | Glossary Management — Simplified | Improve | 2 |
| [6](#6-text-to-speech--server-side-engine) | Text-to-Speech — Server-Side Engine | Scaffolded | 4 |
| [7](#7-portable-usb-deployment) | Portable USB Deployment | New | 5 |
| [8](#8-crash-recovery--system-wide-resilience) | Crash Recovery — System-Wide | Improve | 2 |
| [9](#9-multi-language-operator-ui--expand-coverage) | Multi-Language Operator UI | Improve | 2 |
| [10](#10-session-recording--multi-format-export) | Session Recording & Export | Improve | 2 |
| [11A](#11a-field-feedback-system) | Field Feedback System | New | 4 |
| [11B](#11b-glossary-enrichment-pipeline) | Glossary Enrichment Pipeline | New | 4 |
| [11C](#11c-device-compatibility--suitability-scoring) | Device Compatibility & Suitability Scoring | New | 4 |
| [12](#12-hardware-readiness-score) | Hardware Readiness Score | New | 1 |
| [13](#13-recommended-specifications-generator) | Recommended Specifications Generator | New | 1 |
| [14](#14-pluggable-translation-backends) | Pluggable Translation Backends (Cloud APIs) | Scaffolded | 4 |
| [15](#15-server-infrastructure-upgrade--kestrel) | Server Infrastructure Upgrade (Kestrel) | **Done** | 3 |
| [16](#16-bible-integration) | Bible Integration | Partial | 4 |

**[Implementation Order](#implementation-order)** | **[Development Notes](#development-notes)** | **[Notes](#notes)**

---

## 1. QR Code Connection

**Status:** Not started

**Problem:** Phones currently connect by typing `https://<IP>:5081` manually. In a room of 50 people who don't speak the operator's language, this is a showstopper.

**Implementation:**

- Add NuGet package: `QRCoder` (pure .NET, no native dependencies, MIT license)
- Generate QR code encoding the HTTPS URL: `https://{localIp}:{httpsPort}`
- Display options:
  - **Floating window:** A borderless, always-on-top Form showing the QR code at ~300x300px, draggable, closeable. Operator can position it on a projector screen or hold the laptop up
  - **Button on Live tab:** "Show QR Code" next to the server status label
  - **Printable:** Right-click QR window → "Copy to Clipboard" or "Save as Image" for printing handouts
- Regenerate QR whenever the server restarts or IP changes (coordinated with network change detection in Feature #8b)
- Below the QR code, show the URL as text fallback: `https://192.168.1.5:5081`
- Add i18n string: `Btn_ShowQR` = "Show QR Code" across all supported locales

**Files to modify:**
- `TranscriptionTools.vbproj` — add QRCoder package
- `FormMain.vb` — add button, QR generation logic, floating window
- `Strings.*.resx` — add QR button label

**Complexity:** Low. Mostly UI work around a simple library call.

---

## 2. Setup Wizard — Event Setup Mode

**Status:** Basic first-run wizard exists (`RunFirstTimeSetup()` in FormMain.vb:202-224). Only asks about Start with Windows and firewall access.

**What exists:**
- `FirstRunComplete` flag in AppConfig
- Start with Windows registry toggle
- Firewall rule prompt
- Separate HTTPS cert setup overlay in SubtitleServer

**What to improve:**

### a) Expand First-Run Wizard
Add steps after the current two:
1. **Microphone selection** — Show device list from `/devices` endpoint (or local `sd.query_devices()`), let user pick and test with a level meter (see Feature #3)
2. **Speaker language** — "What language will the speaker use?" dropdown
3. **Translation languages** — Multi-select checklist: "Which languages do your listeners need?" Pre-tick common European languages
4. **Network check** — Verify the laptop is on a Wi-Fi network that phones can reach. Show the connection URL and QR code as confirmation

### b) Add "Event Setup" Mode
- New button on Live tab: "Event Setup" — re-runs a streamlined wizard for configuring a new session
- Skips first-run-only steps (Windows startup, firewall), focuses on:
  - Mic selection + test
  - Speaker language
  - Listener languages
  - QR code display
- Saves as a named "event profile" that can be recalled (e.g., "Sunday Service", "Youth Conference")
- Profile stored in AppConfig as a list of `EventProfile` objects

**Files to modify:**
- `FormMain.vb` — new wizard flow, event setup button
- `AppConfig.vb` — EventProfile model, profiles list
- `Strings.*.resx` — wizard step labels

**Complexity:** Medium. Multi-step wizard UI with validation.

---

## 3. Audio Level Monitor

**Status:** Not started. Audio capture is entirely in Python (`sounddevice` in live-server/server.py). No audio data flows to the VB.NET UI except as transcribed text.

**Problem:** Bad audio is the #1 cause of poor transcription, and the operator has no feedback about audio quality until they see garbage text.

**Implementation:**

### a) Server-Side Audio Level Endpoint
- In `live-server/server.py`, compute RMS level from the audio callback buffer (already collecting 100ms blocks at 16kHz)
- Add `/audio-level` GET endpoint returning JSON: `{"rms": 0.042, "peak": 0.15, "clipping": false, "vad_active": true}`
- Compute in the existing callback — no additional overhead (just `np.sqrt(np.mean(block**2))`)
- Include VAD state so the UI can show "speech detected" vs silence

### b) Client-Side Level Display
- In `FormMain.vb`, poll `/audio-level` every 200ms while live capture is running (lightweight GET)
- Display as a horizontal bar/meter on the Live tab, next to the microphone dropdown
- Color coding:
  - **Grey:** No audio / silence
  - **Green:** Good level (RMS 0.01–0.10)
  - **Yellow:** Low level — show warning label "Move microphone closer"
  - **Red:** Clipping — show warning "Too loud / reduce volume"
- Show a small indicator when VAD detects speech (e.g., a "speaking" dot)
- Warning labels use i18n strings

### c) Environment Check
- On "Event Setup" or first-run, run a 5-second audio test: "Please speak normally into the microphone"
- Report: average level, peak level, noise floor estimate, pass/fail

**Files to modify:**
- `live-server/server.py` — add RMS computation in callback, add `/audio-level` endpoint
- `FormMain.vb` — add level meter control, polling timer, warning labels
- `FormMain.Designer.vb` — layout for meter on Live tab
- `Strings.*.resx` — warning messages

**Complexity:** Medium. The audio math is trivial; the UI polling and visual meter are the main work.

---

## 4. Diagnostic Bundle / Remote Support

**Status:** Not started. Debug logs exist (`YYYYMMDD_pipeline-debug.log`, `YYYYMMDD_nllb-debug.log`) but there's no way to collect and export system state.

**Problem:** A dev team supporting deployments across 36 countries needs to diagnose problems remotely. Currently requires back-and-forth asking operators to find and send log files.

**Implementation:**

### a) System Info Collector
Gather into a single JSON object, using `HardwareScanner.vb` (shared with Feature #12) for hardware data:
- App version, .NET version, OS version
- Hardware profile from `HardwareScanner` — GPU, CPU, RAM, disk (same data as Feature #12, no duplication)
- Hardware readiness score and tier classification (from Feature #12)
- Audio devices: list from `/devices`
- Models loaded: whisper model name/size, NLLB model path, whether GPU is being used
- Server status: subtitle server running, ports, connected clients count
- Server metrics snapshot (from Feature #15h) — current load, latencies, alert state
- Config dump: sanitised AppConfig (exclude any paths with usernames if sensitive)
- Python environment: Python version, pip package versions from both venvs

### b) Log Bundler
- Collect last 3 days of `*_pipeline-debug.log` and `*_nllb-debug.log`
- Include `live-server/server.py` stderr capture if available
- Truncate each to last 500KB if larger

### c) Export Button
- Add "Export Diagnostics" button on Settings tab
- Creates a ZIP file: `diagnostics_{hostname}_{datetime}.zip` containing:
  - `system-info.json`
  - Recent log files
  - `glossary.json` (current glossary state)
  - `config.json` (sanitised)
- SaveFileDialog for the ZIP location
- Operator emails the ZIP to the dev team

### d) File Integrity Verification (Checksums)

Critical files can get corrupted during USB transfers, incomplete downloads, or partial updates. A checksum system catches this before the operator wastes time troubleshooting mysterious failures.

**Build-time: Generate manifest**
- CI/CD (or `build-portable.ps1`) generates `checksums.json` at build time
- Contains SHA256 hashes for every critical file:

```json
{
  "generated": "2026-05-24T12:00:00Z",
  "version": "1.3.2",
  "files": {
    "TranscriptionTools.exe": { "sha256": "a1b2c3...", "size": 152064 },
    "TranscriptionTools.dll": { "sha256": "d4e5f6...", "size": 294912 },
    "nllb-server/server.py": { "sha256": "g7h8i9...", "size": 18432 },
    "live-server/server.py": { "sha256": "j0k1l2...", "size": 24576 },
    "nllb-server/glossary.json": { "sha256": "m3n4o5...", "size": 4096 },
    "live-server/hallucinations.json": { "sha256": "p6q7r8...", "size": 1024 }
  },
  "models": {
    "nllb-model/model.bin": { "sha256": "s9t0u1...", "size": 2684354560 },
    "whisper/ggml-medium.bin": { "sha256": "v2w3x4...", "size": 1533550592 }
  }
}
```

- Separate `files` (small, must match exactly) and `models` (large, hash on demand) sections
- `checksums.json` ships alongside the app in every distribution (installer, app-only zip, portable USB)

**Runtime: Integrity check**
- **Quick check (app files):** Hash all files in the `files` section on startup or on demand — fast, < 1 second
- **Full check (including models):** Hash model files too — slower (30-60s for multi-GB files), run on demand only
- Compare actual SHA256 vs expected from manifest
- Results:

| Status | Meaning |
|--------|---------|
| **PASS** | Hash matches manifest |
| **FAIL** | Hash mismatch — file is corrupted or has been modified |
| **MISSING** | File not found on disk |
| **UNTRACKED** | File exists but isn't in the manifest (not necessarily a problem) |
| **NO MANIFEST** | `checksums.json` itself is missing — can't verify anything |

**Integration points:**
- **Diagnostics bundle:** Include `integrity-check.json` in the export ZIP with pass/fail per file. When a user reports issues, the dev team immediately sees if files are corrupted
- **Settings tab:** "Verify Files" button — runs quick check, shows results with green/red indicators per file
- **First-run / Event Setup:** Run quick check automatically. If any FAIL, warn: "Some files appear corrupted. Try re-downloading or re-copying from the original source."
- **Portable USB:** Particularly important here — USB transfers are the most likely place for corruption. Run check automatically on first launch from a new location

**Model file handling:**
- Model files are too large to hash on every startup
- Hash on demand only: "Verify Models" button (separate from quick check)
- Cache the result: store `{hash, file_modified_date, file_size}` — only re-hash if modified date or size changes
- If a model fails verification: "The NLLB model file appears corrupted. Delete it and re-download? [Yes / No]"

### e) Optional: Health Beacon (Future)
- If internet is available, POST a lightweight health ping (app version, uptime, error count) to a central endpoint
- Opt-in only, disabled by default
- Not essential for initial implementation

**Files to modify:**
- `FormMain.vb` — diagnostics export button, system info collection, verify buttons
- New file: `Diagnostics.vb` — collector, ZIP creation, and checksum verification logic
- New file (build-time): `generate-checksums.ps1` — script to create `checksums.json` during build/publish
- CI workflow (`.github/workflows/`) — add checksum generation step to release pipeline
- `Strings.*.resx` — button labels, status messages, integrity check results

**Dependencies:** `System.IO.Compression`, `System.Security.Cryptography` (both built into .NET 8, no new packages needed)

**Complexity:** Medium. Mostly data collection and file I/O. The checksum generation is trivial; the main work is the verification UI and integrating results into diagnostics, settings, and the setup wizard.

---

## 5. Glossary Management — Simplified for Non-Technical Users

**Status:** Fully implemented. `glossary.json` with trigger/source-lang/fixes model. `FormFilterEditor.vb` provides editing UI. Server-side application in `nllb-server/server.py` with hot-reload.

**What exists:**
- 21 entries, mostly Catalan theological terms
- Trigger-based matching (substring in source text)
- Per-target-language wrong→right word replacements
- Desktop editing UI with grids
- `/glossary/reload` endpoint for hot-reload

**What to improve:**

### a) Glossary Packs / Import-Export
- Pre-built glossary pack: `glossary-packs/christian-theological.json` containing common Christian terms across major European languages (grace, salvation, fellowship, the Word, Holy Spirit, etc.)
- Import button: load a pack file and merge entries (skip duplicates by trigger)
- Export button: save current glossary as a shareable file
- Packs could be distributed via USB or download alongside the app

### b) Simplified Editing UI
- Current UI exposes the full trigger/source-lang/fixes model — too complex for a volunteer
- Add a "Simple Mode" view:
  - Two-column table: "English term" → "Correct translation in [language]"
  - Auto-generates the underlying trigger/fixes structure
  - Hides source language filtering (default to "all languages")
- Keep "Advanced Mode" for power users (current UI)

### c) Community Contribution Workflow
- Handled by Features #11A and #11B — listeners flag phrases on their phones (#11A glossary suggestion via long-press), suggestions flow through the language manager review workflow (#11B)
- See Feature #11A section a (glossary suggestion) and Feature #11B section a (language manager approval UI)

**Files to modify:**
- `FormFilterEditor.vb` — simple mode toggle, import/export buttons
- `nllb-server/glossary.json` — no structural changes needed
- New file: `glossary-packs/christian-theological.json`
- `Strings.*.resx` — new UI labels

**Complexity:** Medium. The import/export is straightforward; the simplified UI is the main design challenge.

---

## 6. Text-to-Speech — Server-Side Engine

**Status:** Browser-side TTS fully implemented. Server-side TTS scaffolded on `feature/kestrel-migration` branch.

**What's done (Kestrel branch):**
- **(a) Server-Side TTS** — `TtsOrchestrator` implements `ITtsService`, selects best backend per language by priority. Three backends coded: `PiperBackend` (priority 1, local ONNX), `MmsTtsBackend` (priority 2, Python sidecar), `EdgeTtsBackend` (priority 3, cloud free). All registered in Kestrel DI.
- **TtsCache** — Ring-buffer cache in `%APPDATA%/TranscriptionTools/tts-cache/`, keyed by `{lang}_commit_{id}.{codec}`, evicts oldest when 200 entries/lang exceeded. Hit/miss tracking.

**What's NOT done:**
- No Piper models downloaded or tested
- No MMS-TTS Python sidecar created
- No Edge TTS actual API integration tested
- **(b) Voice Model Management** — No download/management UI
- **(c) Hybrid Approach** — No server TTS toggle in phone UI
- **(d) Earphone Mode** — Not started
- No `/tts/cache/{file}` endpoint or phone audio playback

**What already exists (browser-side):**

**What exists:**
- `speechSynthesis` API in SubtitleServer.vb embedded JS (lines 1458-1505)
- Voice dropdown populated from `synth.getVoices()`
- Speed presets (0.7/1.0/1.3)
- Speaks on each committed line
- Saved to localStorage

**What to improve:**

### a) Server-Side TTS (Piper or Coqui)
The browser Speech API is device-dependent — many Android phones have poor or missing voices for smaller European languages.

- Add a Python TTS sidecar (similar pattern to nllb-server and live-server)
- Use **Piper TTS** (open source, offline, fast, supports 30+ languages, small models ~15-50MB each)
- Endpoint: `POST /tts` with `{"text": "...", "language": "fra"}` → returns WAV/MP3 audio
- Phone client requests audio from server instead of using local synthesis
- Server generates audio once per language per committed line, cached and served as static files (see Appendix A, O4 for caching strategy)
- Phones play audio via `<audio>` element or Web Audio API

### b) Voice Model Management
- Pre-download voice models for the event's target languages during Event Setup
- Store in `.\tts-models\{language}\` alongside other model directories
- Show download progress and estimated size
- Allow offline pre-loading via USB (copy model folders)

### c) Hybrid Approach
- Default to browser Speech API (zero server load)
- Offer "Server TTS" toggle for languages where browser voices are poor
- Phone UI shows which mode is active

### d) Earphone Mode
- When TTS is active, show a prompt: "For best experience, use earphones"
- Auto-lower volume between utterances to save battery
- Queue management: if speaker talks fast, skip older utterances rather than falling behind

**Files to modify:**
- New directory: `tts-server/` with `server.py`, `requirements.txt`
- `TranscriptionTools.vbproj` — embed tts-server files
- New file: `TtsService.vb` — Python process lifecycle (follow TranslationService.vb pattern), auto-restart via watchdog (Feature #8)
- Kestrel endpoints (Feature #15) — add `/tts/cache/{file}` static serving, `/api/tts-status` endpoint
- `wwwroot/app.js` (after Feature #15f extraction) — audio playback, codec detection, server TTS toggle
- `FormMain.vb` — TTS server management, model download UI

**Dependencies:** piper-tts (Python package), voice model files

**Complexity:** High. New sidecar service, audio streaming, model management. Biggest feature on this list.

---

## 7. Portable USB Deployment

**Status:** Not started. Currently installed via Inno Setup installer or app-only ZIP.

**Problem:** In low-infrastructure settings, you can't assume the operator has admin rights, internet, or even their own laptop. A USB stick that "just works" on any Windows machine is ideal.

**Implementation:**

### a) Self-Contained Portable Build
- New publish profile: `portable` — self-contained .NET 8 deployment (no framework dependency)
- Bundle Python embedded distribution (embeddable zip from python.org, ~15MB) instead of requiring system Python
- Pre-create venvs with all pip packages
- Pre-download whisper model, NLLB model, and TTS voice models
- Total size estimate: ~3-4GB (mostly models)

### b) Portable Mode Detection
- On startup, check if running from a removable drive or if a `portable.flag` file exists next to the exe
- If portable mode:
  - Store all config, logs, and certs relative to the exe directory (not AppData)
  - Skip "Start with Windows" option
  - Skip firewall prompt (won't have admin rights)
  - Auto-detect that HTTPS cert path should be local

### c) USB Build Script
- PowerShell/batch script: `build-portable.ps1`
- Steps:
  1. `dotnet publish` self-contained
  2. Copy Python embedded + pre-built venvs
  3. Copy models (whisper, NLLB, TTS voices for selected languages)
  4. Copy glossary packs
  5. Create `portable.flag`
  6. Output: a single folder ready to copy to USB
- Parameterised: choose which languages/models to include (controls total size)

### d) First-Run on Foreign Laptop
- No installation, no admin rights needed
- Double-click `TranscriptionTools.exe` from USB
- Wizard detects portable mode, skips irrelevant steps
- Firewall popup will appear (Windows always asks for new executables) — document this in the setup wizard with a screenshot/instruction

**Files to modify:**
- New file: `build-portable.ps1` — build script
- `FormMain.vb` — portable mode detection, relative path logic
- `AppConfig.vb` — portable-aware path resolution
- `ConfigManager.vb` — save config next to exe in portable mode
- `TranslationService.vb`, `LiveStreamRunner.vb` — use bundled Python path

**Complexity:** High. Cross-cutting concern that touches path resolution everywhere. The build script is the easy part; making every component portable-aware is the work.

---

## 8. Crash Recovery — System-Wide Resilience

**Status:** Partial. TranslationService.vb has auto-restart with exponential backoff (5 retries, max 30s). LiveStreamRunner.vb has server reuse but no auto-restart on crash. No recovery for subtitle server or main app.

**What exists:**
- Translation server: `Exited` event handler, restart counter, backoff delay (TranslationService.vb:201-220)
- Live server: `KillExistingServer()` for cleanup, health check for reuse (LiveStreamRunner.vb:153-174)
- WebSocket: dropped clients cleaned up, but no reconnection from server side

**What to improve:**

### a) Live Server Auto-Restart
- Add the same `Exited` handler pattern from TranslationService to LiveStreamRunner
- On crash: log error, wait with backoff, restart process, resume SSE subscription
- Preserve transcript state (already in `_liveTranscript` StringBuilder on the VB.NET side)
- Resume capture from current audio — no need to replay; just pick up where it left off

### b) Subtitle Server Resilience
- After Kestrel migration (Feature #15), the server infrastructure is more robust by default, but still needs:
  - Network change detection: when the laptop switches Wi-Fi, detect new IP and restart Kestrel on new address
  - Notify operator: "Network changed — new URL is ..."
  - Regenerate QR code automatically (Feature #1)
  - Notify connected phones via WebSocket: "Server address changed, reconnecting..."

### c) Client Auto-Reconnect
- The phone WebSocket client already has reconnection logic (in the embedded JS)
- Verify it handles: server restart, network blip, laptop sleep/wake
- Add visual indicator on phone: "Reconnecting..." with spinner
- On reconnect, request history replay from last received ID (already supported via `lastId`)

### d) Watchdog Timer
- Main form runs a 30-second timer checking all component health:
  - Live server: GET `/health` — if fails 3 times, restart
  - Translation server: GET `/health` — if fails 3 times, restart
  - Subtitle server (Kestrel): check host status
  - TTS server (if running): GET `/health` — if fails 3 times, restart
- Component health feeds into the unified server metrics dashboard (Feature #15h) — no separate status bar; the watchdog is a data source for the metrics system
- Log all crashes and recoveries for diagnostics

### e) Session State Persistence
- Periodically save `_liveTranscript` to a temp file (every 30s)
- On crash recovery or app restart, offer to resume: "A previous session was interrupted. Resume transcript?"
- Store: transcript text, timestamp, active languages, connected client count

**Files to modify:**
- `LiveStreamRunner.vb` — add Exited handler, auto-restart logic
- Kestrel host (Feature #15) — network change detection, graceful restart on IP change
- `wwwroot/app.js` (after Feature #15f extraction) — verify/improve WebSocket reconnection UX
- `FormMain.vb` — watchdog timer (feeds into Feature #15h metrics), session state persistence
- `ServerMetrics.vb` (Feature #15h) — watchdog health checks as metric data source
- `Strings.*.resx` — status messages, reconnection labels

**Complexity:** Medium-High. Each component needs its own recovery strategy, and they interact (e.g., restarting the live server should not lose the subtitle server's client connections).

---

## 9. Multi-Language Operator UI — Expand Coverage

**Status:** Fully implemented with ResourceManager and 8 locales: English, Spanish, French, German, Catalan, Portuguese, Japanese, Chinese Simplified. Dynamic switching via CultureInfo.

**What exists:**
- `Strings.resx` (English base) with ~80 string keys
- 7 satellite `.resx` files with translations
- `_uiLocales` array in FormMain.vb with language codes and native names
- ComboBox for language selection on Settings tab
- `Thread.CurrentThread.CurrentUICulture` set on startup

**What to improve:**

### a) Add Agape-Priority Languages
New locale files needed (in priority order based on Agape's European footprint):

| Language | Code | File |
|----------|------|------|
| Italian | it | Strings.it.resx |
| Polish | pl | Strings.pl.resx |
| Romanian | ro | Strings.ro.resx |
| Dutch | nl | Strings.nl.resx |
| Hungarian | hu | Strings.hu.resx |
| Czech | cs | Strings.cs.resx |
| Greek | el | Strings.el.resx |
| Ukrainian | uk | Strings.uk.resx |
| Russian | ru | Strings.ru.resx |
| Croatian | hr | Strings.hr.resx |

### b) Translation Workflow
- Create a master spreadsheet (`translations.csv` or Google Sheet) with all string keys and translations
- Script to generate `.resx` files from the spreadsheet: `build-translations.ps1`
- Agape volunteers who speak each language can contribute translations via the spreadsheet
- CI step to validate all keys are present in all locale files

### c) Phone Client i18n Expansion
- The embedded web client already has 8 languages (in SubtitleServer.vb JS)
- Add the same new languages to the phone client's `i18n` object
- Keep phone client and desktop app locale lists in sync

### d) RTL Support (Future)
- Not critical for European deployment, but Arabic (`ar`) would need RTL layout
- Flag for future if Agape's Middle East/North Africa work needs it

**Files to modify:**
- New files: `Strings.it.resx`, `Strings.pl.resx`, etc. (10 new files)
- `FormMain.vb` — expand `_uiLocales` array
- `SubtitleServer.vb` — expand JS `i18n` object
- New file: `build-translations.ps1` — spreadsheet-to-resx converter
- `Strings.*.resx` — any new string keys from other features must be added to ALL locales

**Complexity:** Low-Medium per language (mostly translation work, not code). The tooling for sustainable translation management is the real investment.

---

## 10. Session Recording & Multi-Format Export

**Status:** Basic implementation. Desktop saves committed transcript to plain .txt. Web client has no save. Log files exist but are debug-oriented.

**What exists:**
- `btnLiveSave_Click` in FormMain.vb:1727-1746 — saves `_liveTranscript` to `.txt`
- `rtbLiveOutput.Text` contains full log with timestamps
- SaveFileDialog with default name `live_transcript_{datetime}.txt`
- Debug logs in `YYYYMMDD_pipeline-debug.log`

**What to improve:**

### a) Multi-Language Export
- Save original transcript AND translations side-by-side
- Format options:
  - **Plain text:** One paragraph per language, separated by language headers
  - **CSV/TSV:** Columns: Timestamp | Original | French | German | Polish | ...
  - **HTML:** Formatted document with language sections, suitable for printing or emailing
- Source data: capture translations as they're generated (currently only the final translated text is sent to phone clients — need to also store server-side)

### b) Timestamped Output
- Each committed line already has a timestamp in the debug log
- Add timestamp to `_liveTranscript` entries: `[14:32:05] The speaker said this...`
- Enables syncing transcript with audio/video recordings made separately

### c) SRT Subtitle Export
- Generate `.srt` files (standard subtitle format) from timestamped commits
- Useful for: adding subtitles to recorded sermons/talks, post-event video distribution
- One `.srt` per language
- Format: sequential numbering, start→end timestamps, text

### d) Audio Recording (Optional)
- Record the raw audio stream to a WAV file alongside the transcript
- Toggle: "Record audio" checkbox on Live tab
- In `live-server/server.py`, write audio buffer to file in parallel with transcription
- Enables post-session quality review and re-transcription with different settings

### e) Web Client Download
- Add "Save Transcript" button to the phone client
- Generates a text file download from the client's received messages
- Each phone saves in their own language
- Uses `Blob` + `URL.createObjectURL` (works in ES5 with polyfill, or acceptable to use modern JS for this)

**Files to modify:**
- `FormMain.vb` — enhanced save dialog with format options, timestamp storage
- `SubtitleServer.vb` — server-side translation storage, SRT generation endpoint
- `SubtitleServer.vb` (JS) — phone client save button
- `live-server/server.py` — optional audio recording to file
- `Strings.*.resx` — export option labels

**Complexity:** Medium. Multiple export formats, but each is straightforward. Audio recording adds the most complexity.

---

## 11A. Field Feedback System

**Status:** Not started. Currently glossary edits are made by the developer on the desktop app. No mechanism for field users (listeners on phones) or operators to contribute feedback or suggest corrections.

**Problem:** The system translates into dozens of languages, but only the developer can judge quality and fix errors. In the field, the people who *actually know* whether a translation is correct are the listeners — a Romanian volunteer reading the Romanian output, a Polish pastor noticing a theological term was mistranslated. That knowledge is currently lost. Building a feedback loop turns every deployment into an opportunity to improve the system for all future deployments.

**Scope:** Phone feedback UI + operator feedback + session reports + data format. This feature collects the data; Features #11B and #11C process and act on it.

**Implementation:**

### a) Listener Feedback — Phone Client

Add a lightweight feedback mechanism to the phone subtitle client:

**Experience Log:**
- Small "Feedback" button in the phone client menu/settings area
- Tapping opens a simple form:
  - **Rating:** How well could you follow? (1-5 stars or simple Good/OK/Poor)
  - **Free text:** "Anything you'd like us to know?" (optional)
  - **Issue categories:** checkboxes for common problems — "Text didn't appear", "Translation was wrong", "Page didn't load properly", "Audio read-aloud didn't work", "Screen went to sleep"
  - **Auto-captured metadata:** device info (User-Agent, platform, screen size), session ID, client language, timestamp
- Submitted via POST to `/api/feedback` on the subtitle server
- Stored locally in `feedback/session_{datetime}.json` on the operator's laptop
- Must work offline — no external server required

**Glossary Suggestion ("Flag a phrase"):**
- Long-press (or tap-and-hold) on any subtitle line to flag it
- Popup shows the original text and the translation the user received
- User can type the correct translation in their language
- Fields: original text | received translation | suggested correction | language | timestamp
- Submitted via POST to `/api/glossary-suggestion`
- Stored in `feedback/glossary-suggestions.json` on the operator's laptop

**UI considerations:**
- Keep it minimal — one tap to rate, two taps to flag a phrase
- Don't interrupt the subtitle reading experience
- Confirmation: "Thank you — your feedback has been saved" (i18n)
- All feedback UI strings added to the phone client's `i18n` object (all supported languages)

### b) Operator Feedback — Desktop App

- After stopping a session, prompt: "How did this session go?" (optional, dismissable)
- Quick form:
  - Overall quality (1-5)
  - Audio conditions (good mic / noisy room / outdoor / etc.)
  - Any issues noted (free text)
  - Which languages seemed to work well / poorly
- Saved alongside listener feedback in the same session folder

### c) Feedback Aggregation & Reporting

- **Session summary report:** after each session, auto-generate a summary:
  - Number of connected listeners, by language
  - Average rating (if feedback submitted)
  - Number of glossary suggestions received
  - Flagged phrases (potential problem areas)
  - Device breakdown: how many Android/iOS/other, which browsers, which OS versions (from device info captured in feedback)
- Stored in `feedback/reports/session_{datetime}.html` — simple HTML viewable in any browser
- Over time, patterns emerge: "Romanian always gets flagged for theological terms" → prioritise Romanian glossary work

### d) Feedback Data Format

All feedback stored as JSON for easy parsing and future aggregation:

```json
// feedback/session_2026-05-24_1430.json
{
  "session": {
    "id": "2026-05-24_1430",
    "date": "2026-05-24T14:30:00",
    "operator_rating": 4,
    "operator_notes": "Good session, microphone picked up some echo",
    "audio_conditions": "indoor_echo",
    "duration_minutes": 45,
    "languages_served": ["fra_Latn", "ron_Latn", "pol_Latn"]
  },
  "listener_feedback": [
    {
      "language": "fra_Latn",
      "rating": 4,
      "comment": "Very good, a few words were wrong",
      "issues": ["translation_wrong"],
      "timestamp": "2026-05-24T15:10:00",
      "device": {
        "user_agent": "Mozilla/5.0 (Linux; Android 14; SM-A546B) AppleWebKit/537.36 ...",
        "platform": "Android",
        "os_version": "14",
        "browser": "Chrome Mobile",
        "browser_version": "125.0",
        "device_model": "SM-A546B",
        "screen": "1080x2340",
        "secure_context": true,
        "websocket_supported": true,
        "speech_synthesis": true,
        "wake_lock": true,
        "connection_type": "wifi"
      }
    }
  ],
  "glossary_suggestions": [
    {
      "id": "gs_001",
      "original_text": "la gracia de Dios",
      "received_translation": "la grâce de Dieu",
      "suggested_correction": "la grâce de Dieu",
      "language": "fra_Latn",
      "timestamp": "2026-05-24T14:45:00",
      "status": "pending",
      "reviewed_by": null,
      "review_notes": null
    }
  ]
}
```

**Files to modify:**
- Kestrel endpoints (Feature #15) — add `/api/feedback`, `/api/glossary-suggestion` POST endpoints
- `wwwroot/app.js` (after Feature #15f extraction) — feedback button, long-press handler, suggestion form, i18n strings
- `FormMain.vb` — post-session feedback prompt, session summary generation
- New directory: `feedback/` — storage for session feedback and suggestions
- New directory: `feedback/reports/` — auto-generated session reports
- `Strings.*.resx` — feedback UI labels

**Dependencies:** None. All JSON file I/O and HTML generation using built-in .NET libraries.

**Complexity:** Medium. The phone client feedback UI is simple. The main work is designing the long-press glossary suggestion flow to be intuitive without disrupting subtitle reading.

---

## 11B. Glossary Enrichment Pipeline

**Status:** Not started. Depends on Feature #11A for glossary suggestion data. Extends Feature #5c (community contribution workflow).

**Problem:** Glossary suggestions collected via Feature #11A need a review and approval workflow before they become glossary entries. Without this, suggestions accumulate but never improve the system.

**Scope:** Language manager review UI + distributed review workflow. Takes raw suggestions from #11A and turns them into approved glossary entries.

**Implementation:**

### a) Language Manager Review — Desktop App

Introduce a **Language Manager** role — someone who speaks a target language and can approve or reject glossary suggestions.

**Review UI (new tab or dialog in FormFilterEditor):**
- Lists all pending glossary suggestions, grouped by language
- For each suggestion, shows:
  - Original source text (what the speaker said)
  - What the system translated it to
  - What the listener says it should be
  - Session context (timestamp, how many times this phrase appeared)
- Language manager actions:
  - **Approve** — converts the suggestion into a glossary entry (trigger + fix) and adds to `glossary.json`
  - **Edit & Approve** — manager adjusts the correction before approving (listener might be close but not exact)
  - **Reject** — marks as reviewed, does not add to glossary
  - **Defer** — skip for now, keep in queue
- Approved entries automatically formatted into the glossary data model (trigger, source_langs, fixes)
- After approval, hot-reload glossary via `/glossary/reload` (already supported)

**Access control (simple, offline-friendly):**
- No authentication system needed — this runs on the operator's laptop
- Language manager access is implicit: if you're sitting at the laptop reviewing suggestions, you're authorised

### b) Distributed Review Workflow

For when Agape has language managers across multiple countries who can't sit at the operator's laptop:

- **Export:** Operator clicks "Export Pending Suggestions" → generates a `pending-review_{language}.json` file per language
- **Email/share:** Send each file to the relevant language manager
- **Review tool:** A standalone lightweight HTML page (single file, opens in any browser, works offline) that loads the JSON and presents approve/edit/reject UI
- **Import:** Language manager returns the reviewed file, operator imports via "Import Reviews" button
- Approved entries merge into `glossary.json`

This avoids building any server infrastructure — it's a file-based workflow that works over email, USB sticks, or any file sharing method available.

**Files to modify:**
- `FormFilterEditor.vb` — glossary suggestion review tab, approve/reject/edit workflow
- New file: `review-tool.html` — standalone offline review page for distributed workflow
- `Strings.*.resx` — review workflow labels

**Dependencies:** Feature #11A (needs suggestion data to review). Feature #5 (glossary packs — extends the existing glossary editing UI).

**Complexity:** Medium-High. The review UI in FormFilterEditor is the main design work — it needs to be intuitive enough that a non-developer language manager can use it confidently.

---

## 11C. Device Compatibility & Suitability Scoring

**Status:** Not started. Can be developed independently of Features #11A and #11B.

**Problem:** Every phone that connects is a potential data point about what works and what doesn't. Currently there's no way to know if a user's device supports all features, and no guidance when features are unavailable. This feature captures device capabilities automatically and uses the accumulated data to warn future users proactively.

**Scope:** Device fingerprinting, passive device logging, curated compatibility database, connection-time warnings, feature capability indicators, TTS voice audit, device suitability score. Primarily phone-side JS with server-side logging.

**Implementation:**

### a) Device Fingerprinting (on connection)

When a phone connects via WebSocket, the client JS immediately sends a capability report:

```javascript
var deviceInfo = {
  userAgent: navigator.userAgent,
  platform: navigator.platform,
  screen: screen.width + 'x' + screen.height,
  secureContext: window.isSecureContext,
  websocket: 'WebSocket' in window,
  speechSynthesis: 'speechSynthesis' in window,
  wakeLock: 'wakeLock' in navigator,
  serviceWorker: 'serviceWorker' in navigator,
  connectionType: (navigator.connection || {}).type || 'unknown',
  touchscreen: 'ontouchstart' in window,
  language: navigator.language
};
```

Server parses the User-Agent to extract: OS (Android/iOS/Windows/etc.), OS version, browser name, browser version, device model (where available from UA string).

### b) Device Log (passive, every session)

Every connecting device is logged to `feedback/device-log.json` automatically — no user action required:

```json
{
  "devices": [
    {
      "fingerprint": "android_14_chrome_125_SM-A546B",
      "first_seen": "2026-05-10T09:00:00",
      "last_seen": "2026-05-24T14:30:00",
      "sessions": 12,
      "platform": "Android",
      "os_version": "14",
      "browser": "Chrome Mobile",
      "browser_version": "125.0",
      "model": "SM-A546B",
      "capabilities": {
        "websocket": true,
        "secure_context": true,
        "speech_synthesis": true,
        "wake_lock": true
      },
      "issues_reported": 0,
      "status": "verified"
    }
  ]
}
```

`status` values:
- **verified** — device has connected successfully across multiple sessions with no issues
- **ok** — device connected, no problems reported (but limited data)
- **issues** — device has reported problems (issues_reported > 0)
- **blocked** — manually flagged as incompatible by the dev team

### c) Compatibility Database (curated)

Maintain a curated `compatibility.json` that ships with the app, updated each release:

```json
{
  "known_issues": [
    {
      "match": {"platform": "iOS", "browser": "Safari", "version_below": "15.0"},
      "severity": "warning",
      "message_key": "compat_ios_safari_old",
      "detail": "WebSocket connections may drop frequently on iOS Safari < 15. Update iOS for best experience.",
      "workaround": "Use Chrome for iOS instead"
    },
    {
      "match": {"platform": "Android", "browser": "Samsung Internet", "version_below": "18.0"},
      "severity": "info",
      "message_key": "compat_samsung_tts",
      "detail": "Read-aloud may not work. Use Chrome for full feature support."
    },
    {
      "match": {"platform": "Android", "version_below": "8.0"},
      "severity": "error",
      "message_key": "compat_android_old",
      "detail": "This version of Android is too old for reliable WebSocket connections. Some features will not work."
    }
  ],
  "tested_devices": [
    {"model": "iPhone 13", "os": "iOS 17", "browser": "Safari", "status": "fully_compatible"},
    {"model": "Samsung Galaxy A54", "os": "Android 14", "browser": "Chrome", "status": "fully_compatible"},
    {"model": "Xiaomi Redmi Note 12", "os": "Android 13", "browser": "Chrome", "status": "fully_compatible"},
    {"model": "Huawei P30 Lite", "os": "Android 10", "browser": "Chrome", "status": "compatible_no_wake_lock"}
  ]
}
```

### d) Connection-Time Warning

When a phone connects, the client JS checks the device against `compatibility.json`:

- **No issues found:** connect normally, no message
- **Warning-level match:** show a dismissable banner: "Your browser may have limited support. For best experience, use Chrome." (in the client's language via i18n)
- **Error-level match:** show a persistent banner: "Your device may not work well with this system. [Details]"
- **Blocked:** show a full-screen message explaining the problem and suggesting alternatives

The check happens client-side (JS loads `compatibility.json` via fetch on connect) so it works even before the WebSocket is established.

### e) Feature Capability Indicators

On the phone client, show small icons indicating which features are available on this device:

| Feature | Requires | Indicator |
|---------|----------|-----------|
| Subtitles | WebSocket | Always required — if missing, nothing works |
| Read Aloud (TTS) | speechSynthesis + voice for language | Speaker icon (greyed/amber/green — see TTS audit below) |
| Screen Stay-On | Wake Lock API + HTTPS | Lock icon (greyed out if unavailable) |
| Bible Search | Fetch API | Book icon (greyed out if unavailable) |
| Save Transcript | Blob + URL.createObjectURL | Download icon (greyed out if unavailable) |

If a feature is unavailable, tapping the greyed icon explains why: "Read-aloud is not supported on your browser. Try using Chrome."

### f) TTS Voice Audit

The `speechSynthesis` API existing on a device doesn't mean TTS actually works for the listener's language. A phone might report `speechSynthesis: true` but have zero voices installed for Romanian or Hungarian. The client needs to audit what's actually usable.

On connect (after `speechSynthesis.onvoiceschanged` fires), run a voice audit:

```javascript
function auditTtsVoices(targetLang) {
  var voices = speechSynthesis.getVoices();
  var result = {
    total_voices: voices.length,
    matching_voices: [],
    best_match: null,
    quality: 'none'  // none | poor | fair | good
  };
  // Match voices to the listener's selected language
  for (var i = 0; i < voices.length; i++) {
    if (voices[i].lang.substring(0, 2) === targetLang.substring(0, 2)) {
      var info = {
        name: voices[i].name,
        lang: voices[i].lang,
        local: voices[i].localService,  // true = on-device, false = network
        default_voice: voices[i].default
      };
      result.matching_voices.push(info);
    }
  }
  if (result.matching_voices.length === 0) {
    result.quality = 'none';
  } else if (result.matching_voices.some(function(v) { return v.local; })) {
    result.quality = 'good';  // has on-device voice
    result.best_match = /* prefer local voices */;
  } else {
    result.quality = 'fair';  // network voices only (need internet)
  }
  return result;
}
```

Quality levels:
- **good** — on-device voice available for this language (works offline, low latency)
- **fair** — network-only voice available (requires internet, higher latency)
- **poor** — voices exist but wrong dialect/region (e.g., `pt-BR` when listener wants `pt-PT`)
- **none** — no voices for this language at all

### g) TTS Guidance for Users

When TTS quality is `none` or `poor` for their language, show actionable guidance instead of just greying out the icon:

| Platform | Guidance |
|----------|----------|
| **Android** | "To enable read-aloud in {language}: Settings → System → Languages → Text-to-Speech → Install voice data for {language}. Google TTS supports most European languages." |
| **Android (no Google TTS)** | "Install 'Google Text-to-Speech' from the Play Store for the best voice support." |
| **iOS** | "To enable read-aloud in {language}: Settings → Accessibility → Spoken Content → Voices → {language} → Download a voice." |
| **Samsung** | "Samsung devices: Settings → General Management → Text-to-Speech → Samsung TTS → Install language. Or install Google TTS from the Play Store for wider language support." |

The guidance is platform-specific (detected from User-Agent) and language-specific. Store the guidance templates in `compatibility.json` so they can be updated without code changes:

```json
{
  "tts_guidance": {
    "android_chrome": {
      "install_voices": "Settings → System → Languages & input → Text-to-Speech → {engine} → Install voice data → {language}",
      "install_engine": "Install 'Google Text-to-Speech' from the Play Store",
      "recommended_engine": "Google TTS"
    },
    "android_samsung": {
      "install_voices": "Settings → General Management → Text-to-Speech → Install language",
      "install_engine": "Install 'Google Text-to-Speech' from the Play Store for wider language support",
      "recommended_engine": "Google TTS or Samsung TTS"
    },
    "ios_safari": {
      "install_voices": "Settings → Accessibility → Spoken Content → Voices → {language} → Download",
      "note": "iOS voices are generally high quality but must be downloaded per language"
    }
  }
}
```

Display as a step-by-step overlay (similar to the existing HTTPS certificate acceptance guide) — the user follows the steps, installs the voice, then taps "Try again" to re-audit.

### h) TTS Voice Data in Device Log

Include the full TTS audit in the device fingerprint sent to the server:

```json
{
  "device": {
    "...existing fields...",
    "tts_audit": {
      "total_voices": 24,
      "target_language": "ro",
      "matching_voices": [
        {"name": "Google Romanian", "lang": "ro-RO", "local": true}
      ],
      "quality": "good"
    }
  }
}
```

This feeds into session reports (Feature #11A): "8 of 12 listeners had TTS voices available for their language; 4 Romanian users had no voices — consider adding server-side TTS (Feature #6) for Romanian."

### i) Device Suitability Score

Give each connecting phone a suitability score out of 100, shown briefly on connection (or accessible from a "Device Info" menu item):

| Component | Weight | Scoring |
|-----------|--------|---------|
| **WebSocket** | 30% | Supported = 100, Not supported = 0 (hard fail) |
| **HTTPS / Secure Context** | 20% | Secure = 100, Insecure = 30 (Wake Lock and some APIs unavailable) |
| **TTS for selected language** | 20% | good = 100, fair = 60, poor = 30, none = 0 |
| **Wake Lock** | 15% | Supported = 100, Not supported = 0 |
| **Browser currency** | 10% | Last 2 major versions = 100, 3-4 versions old = 50, older = 10 |
| **Screen size** | 5% | Width ≥ 360px = 100, 320-359 = 60, < 320 = 20 |

**Score = (WS × 0.30) + (HTTPS × 0.20) + (TTS × 0.20) + (WakeLock × 0.15) + (Browser × 0.10) + (Screen × 0.05)**

Traffic light display on the phone:
- **Green (75-100):** "Your device is well suited for this system"
- **Amber (50-74):** "Your device will work but some features are limited" + list of what's missing
- **Red (< 50):** "Your device may have problems" + specific guidance for each failing component

Show the score and breakdown when the user taps a small info icon in the phone client header. Don't show it intrusively — most users just want to read subtitles. But when something doesn't work, it gives them (and the operator) a clear picture of why.

The score is also sent to the server as part of the device fingerprint, so session reports can show: "Average device score: 82/100. 2 devices scored below 50 (both Android 7, Samsung Internet)."

### j) Feeding Back Into Development

The device log accumulated across sessions becomes a compatibility report:
- "85% of connections are Android Chrome — prioritise testing there"
- "12 Samsung Internet users connected last month, 4 reported issues �� investigate"
- "3 users on iOS 14 Safari had WebSocket drops — add to known_issues"
- "Romanian TTS quality is 'none' on 60% of devices — prioritise server-side TTS for Romanian"

The dev team updates `compatibility.json` based on this data each release. The feedback loop: field data → curated compatibility rules → proactive warnings → TTS guidance → better user experience.

**Note on pre-Kestrel implementation:** Device fingerprinting can be sent over the existing WebSocket (no new endpoints needed). Compatibility checking happens entirely client-side (JS fetches `compatibility.json` via the existing HTTP server). The device log writing is server-side but triggered by a WebSocket message. This means #11C can partially ship before the Kestrel migration.

**Files to modify:**
- `wwwroot/app.js` (or SubtitleServer.vb embedded JS if pre-Kestrel) — device capability detection, TTS voice audit, compatibility check on load, feature availability indicators, device suitability score, TTS guidance overlay, i18n strings
- New file: `compatibility.json` — curated known issues, tested devices, TTS guidance templates (ships with app, updated per release)
- `SubtitleServer.vb` — handle device fingerprint WebSocket message, write to device log
- New file: `feedback/device-log.json` — passive device logging (auto-created)
- `Strings.*.resx` — compatibility warning messages, TTS guidance text

**Dependencies:** None. All client-side JS detection + server-side JSON file I/O.

**Complexity:** High. The most complex part is the TTS voice audit and platform-specific guidance system. However, each subsection (fingerprinting, compatibility check, TTS audit, suitability score) is independently buildable and testable.

---

## 12. Hardware Readiness Score

**Status:** Not started. No system capability detection exists. Users discover their hardware is inadequate only after trying to run transcription and getting unusable results.

**Problem:** Someone in rural Poland installs the app on a 2015 ultrabook with integrated graphics. The app starts, everything looks fine, but transcription runs at 0.3x real-time and the translations lag 30 seconds behind the speaker. They conclude the software doesn't work. A hardware check on first run would have told them upfront: "This laptop isn't powerful enough for live transcription" — saving frustration and protecting the tool's reputation.

**Implementation:**

### a) Hardware Detection

Collect on startup (cached, re-run on demand):

| Component | Detection Method | Data Collected |
|-----------|-----------------|----------------|
| **GPU** | `nvidia-smi` CLI or WMI `Win32_VideoController` | Model name, VRAM (MB), CUDA capability, driver version |
| **CPU** | WMI `Win32_Processor` | Model, core count, base clock speed, architecture |
| **RAM** | WMI `Win32_ComputerSystem` | Total physical memory |
| **Disk** | `DriveInfo` (.NET) | Free space on app drive |
| **OS** | `Environment.OSVersion` | Windows version, 64-bit check |

For GPU, try `nvidia-smi` first (gives CUDA version and precise VRAM). Fall back to WMI for non-NVIDIA or if `nvidia-smi` isn't in PATH. Detect AMD/Intel integrated graphics as "no CUDA" explicitly.

### b) Scoring System — Score out of 100

Each component scored independently, then weighted into an overall score:

| Component | Weight | Scoring Criteria |
|-----------|--------|-----------------|
| **GPU** | 40% | NVIDIA + VRAM is the dominant factor for whisper/NLLB performance |
| **CPU** | 25% | Matters for CPU-only fallback and general overhead |
| **RAM** | 20% | Models need memory; too little causes swapping |
| **Disk** | 10% | Models are large; need space for whisper + NLLB + TTS |
| **OS** | 5% | 64-bit Windows 10+ required; older = 0 |

**GPU scoring (0-100, weight 40%):**

| VRAM | CUDA Compute | GPU Score |
|------|-------------|-----------|
| 8GB+ NVIDIA (RTX 3060+) | 7.0+ | 100 |
| 6GB NVIDIA (RTX 2060, GTX 1660) | 6.1+ | 80 |
| 4GB NVIDIA (GTX 1650, etc.) | 6.1+ | 55 |
| 2GB NVIDIA or older CUDA | < 6.1 | 25 |
| No NVIDIA (Intel/AMD integrated) | None | 10 |

**CPU scoring (0-100, weight 25%):**

| Cores | Clock | CPU Score |
|-------|-------|-----------|
| 8+ cores, 3.0GHz+ | Modern (12th gen+) | 100 |
| 6+ cores, 2.5GHz+ | | 75 |
| 4 cores, 2.0GHz+ | | 45 |
| 2 cores or < 2.0GHz | | 15 |

**RAM scoring (0-100, weight 20%):**

| RAM | Score |
|-----|-------|
| 32GB+ | 100 |
| 16GB | 85 |
| 8GB | 50 |
| < 8GB | 15 |

**Disk scoring (0-100, weight 10%):**

| Free Space | Score |
|------------|-------|
| 20GB+ | 100 |
| 10-20GB | 70 |
| 5-10GB | 40 |
| < 5GB | 10 |

**OS scoring (0-100, weight 5%):**

| OS | Score |
|----|-------|
| Windows 10/11 64-bit | 100 |
| Windows 10/11 32-bit | 20 |
| Older Windows | 0 |

**Overall = (GPU × 0.40) + (CPU × 0.25) + (RAM × 0.20) + (Disk × 0.10) + (OS × 0.05)**

### c) Traffic Light System

The overall score maps to a clear traffic light:

| Score | Light | Verdict | Message |
|-------|-------|---------|---------|
| 75-100 | **GREEN** | Excellent | "This PC is well suited for live transcription and translation." |
| 50-74 | **AMBER** | Adequate | "This PC can run transcription but performance may be limited. See recommendations below." |
| 25-49 | **RED** | Poor | "This PC will struggle with live transcription. Consider using a more powerful laptop." |
| 0-24 | **RED (dark)** | Not recommended | "This PC does not meet minimum requirements for usable live transcription." |

### d) Actionable Recommendations

Based on individual component scores, generate specific advice:

- GPU < 50: "No NVIDIA GPU detected — transcription will run on CPU only, expect significant delays. For live use, an NVIDIA GPU with at least 4GB VRAM is strongly recommended."
- GPU 50-70: "Your GPU has {X}GB VRAM — use the 'small' or 'base' whisper model instead of 'medium' for best real-time performance."
- RAM < 50: "With {X}GB RAM, running transcription and translation simultaneously may cause slowdowns. Close other applications before starting."
- Disk < 40: "Only {X}GB free disk space. You may not have room for all language models. Free up space or use an external drive."
- CPU only (no GPU): "Without a GPU, consider using the 'tiny' or 'base' whisper model. Accuracy will be lower but speed will be usable."

### e) UI Integration

**Note:** This is the **server/laptop** score (out of 100). There is a separate **phone/device** suitability score (also out of 100) in Feature #11C section i. Different things, same visual language. The desktop app shows "Server Score: 78/100" for the laptop hardware. The phone client shows "Device Score: 85/100" for the phone's browser capabilities. Both use the green/amber/red traffic light system.

**First-run wizard (Feature #2):**
- Run hardware scan as the first wizard step, before anything else
- Show the traffic light and score prominently
- If RED: warn clearly but don't block — "You can still try, but expect limitations"
- If GREEN: reassure and move on quickly

**Settings tab:**
- "Hardware Score" section showing:
  - Overall score with traffic light indicator (coloured circle or icon)
  - Component breakdown: GPU [80/100] CPU [65/100] RAM [85/100] etc.
  - "Re-scan" button (for after hardware/driver changes)
  - Recommendations panel

**Diagnostics bundle (Feature #4):**
- Include hardware score and component breakdown in the diagnostics export
- When a user reports issues, the dev team immediately sees their hardware capability

### f) Smart Defaults Based on Score

Use the hardware score to auto-configure optimal settings:

| Score Range | Auto-Configuration |
|-------------|-------------------|
| 75+ | Medium whisper model, NLLB enabled, all features on |
| 50-74 | Small whisper model, NLLB enabled, warn about TTS overhead |
| 25-49 | Base whisper model, suggest disabling NLLB (transcription only), longer interim intervals |
| < 25 | Tiny whisper model, NLLB disabled by default, maximum interim interval |

- Auto-configuration applied on first run; user can override in Settings
- Show what was auto-configured and why: "Based on your hardware score (58/100), we've selected the 'small' whisper model for the best balance of speed and accuracy."

**Files to modify:**
- New file: `HardwareScanner.vb` — WMI queries, nvidia-smi parsing, scoring logic
- `FormMain.vb` — hardware score display on Settings tab, first-run integration
- `FormMain.Designer.vb` — score UI layout (traffic light, component bars, recommendations panel)
- `AppConfig.vb` — cached hardware score, auto-configured model selections
- `Diagnostics.vb` (Feature #4) — include hardware report in export
- `Strings.*.resx` — score verdicts, recommendations, component labels

**Dependencies:** `System.Management` (WMI, built into .NET on Windows). No external packages.

**Complexity:** Medium. The WMI/nvidia-smi queries are straightforward. The scoring weights will need calibration against real hardware — initial values are estimates based on known whisper performance benchmarks, but should be validated with testing on a range of machines. The smart defaults integration touches model selection logic which needs careful testing.

---

## 13. Recommended Specifications Generator

**Status:** Not started. Users who want to buy a computer for running the system have no guidance on what to look for, and the requirements shift as features and models change between releases.

**Problem:** A country director in Romania asks "what laptop should I buy?" — currently there's no good answer beyond "something with an NVIDIA GPU." The requirements depend on which features they'll use (transcription only vs transcription + translation + TTS), and they change when we update models or add features. A static spec sheet goes stale; a generated one stays accurate.

**Implementation:**

### a) Specification Tiers

Define three tiers based on usage profiles:

| Tier | Use Case | Target Score |
|------|----------|-------------|
| **Minimum** | Transcription only (no translation), small groups, base/small whisper model | Score 50+ (Amber) |
| **Recommended** | Transcription + translation, medium whisper model, 10-20 listeners | Score 75+ (Green) |
| **Optimal** | Full stack: transcription + translation + TTS, large whisper model, 50+ listeners, multi-hour sessions | Score 90+ (Green) |

### b) Specification Profiles

Each tier maps to concrete hardware specs:

```
MINIMUM (Score 50+)
  GPU:    NVIDIA GTX 1650 (4GB VRAM) or better
  CPU:    Intel i5 10th gen / AMD Ryzen 5 3600 or better (4+ cores)
  RAM:    8GB
  Disk:   20GB free (SSD preferred)
  OS:     Windows 10/11 64-bit

RECOMMENDED (Score 75+)
  GPU:    NVIDIA RTX 3060 (6GB VRAM) or better
  CPU:    Intel i5 12th gen / AMD Ryzen 5 5600 or better (6+ cores)
  RAM:    16GB
  Disk:   30GB free (SSD required)
  OS:     Windows 10/11 64-bit

OPTIMAL (Score 90+)
  GPU:    NVIDIA RTX 3070/4060 (8GB+ VRAM) or better
  CPU:    Intel i7 12th gen / AMD Ryzen 7 5800 or better (8+ cores)
  RAM:    32GB
  Disk:   50GB free (NVMe SSD)
  OS:     Windows 11 64-bit
```

### c) Localised Spec Sheet Generation

- "Generate Spec Sheet" button on Settings tab (or accessible from hardware score panel)
- Generates a PDF or HTML document in the operator's selected UI language
- Contents:
  - App name, version, generation date
  - Three-tier specification table (as above)
  - Plain-language explanation of each component and why it matters
  - "What to look for when buying" tips (e.g., "Look for laptops marketed as 'gaming laptops' — they have the GPU power needed")
  - Country-specific buying tips if available (e.g., common laptop brands in that region)
- Output: saved as `Recommended_Specifications_{lang}.html` — viewable in any browser, printable, emailable
- Operator can send this to a purchasing manager or donor who's buying equipment

### d) Per-Release Recalculation

The spec tiers must be **recalculated each release** if resource requirements change significantly:

- New file: `specs.json` — defines the current spec tiers, generated/updated during the release process
- Format:

```json
{
  "version": "1.4.0",
  "last_updated": "2026-06-01",
  "change_notes": "Added TTS sidecar, increased optimal RAM to 32GB",
  "tiers": {
    "minimum": {
      "gpu_vram_gb": 4,
      "gpu_examples": "GTX 1650, GTX 1050 Ti",
      "cpu_cores": 4,
      "cpu_clock_ghz": 2.0,
      "cpu_examples": "Intel i5 10th gen, AMD Ryzen 5 3600",
      "ram_gb": 8,
      "disk_free_gb": 20,
      "disk_type": "SSD preferred",
      "os": "Windows 10/11 64-bit",
      "use_case": "Transcription only, small groups"
    },
    "recommended": { ... },
    "optimal": { ... }
  }
}
```

- CI/release checklist includes: "Have resource requirements changed? Update `specs.json`"
- The app reads `specs.json` to generate the localised spec sheet — so updating the JSON automatically updates what every user sees
- Hardware score thresholds (Feature #12) also derive from `specs.json` to stay consistent

### e) Integration with Hardware Score

- When the hardware score runs, show which tier the current PC falls into:
  - "Your PC meets the **Recommended** specification tier"
  - "Your PC falls below the **Minimum** specification — see recommendations"
- Link to the full spec sheet from the hardware score panel
- If score is RED, include the spec sheet in the first-run wizard: "Here's what to look for if you need a more powerful computer"

### f) Localisation

Spec sheet text must be translated into all supported UI languages. Key strings:
- Tier names and descriptions
- Component explanations ("GPU" → what it is in plain language)
- Buying tips
- All stored in `Strings.*.resx` alongside other UI strings

For languages not yet in the UI (e.g., before Feature #9 adds Polish/Romanian), generate in English with a note: "This document is not yet available in your language."

**Files to modify:**
- New file: `specs.json` — tier definitions, updated per release
- New file: `SpecSheetGenerator.vb` — reads `specs.json` + locale, generates HTML output
- `FormMain.vb` — "Generate Spec Sheet" button, integration with hardware score panel
- `HardwareScanner.vb` (Feature #12) — tier classification based on `specs.json` thresholds
- `Strings.*.resx` — spec sheet text, tier descriptions, buying tips
- CI/release pipeline — checklist/reminder to update `specs.json`

**Dependencies:** None. HTML generation using built-in .NET string/file operations.

**Complexity:** Low-Medium. The spec sheet generation is straightforward HTML templating. The main ongoing cost is remembering to update `specs.json` each release — the CI checklist helps with this.

---

## 14. Pluggable Translation Backends (Cloud APIs)

**Status:** Scaffolded on `feature/kestrel-migration` branch.

**What's done:**
- **(a) Backend Abstraction** — `ITranslationBackend` interface with `TranslateAsync`, `GetSupportedLanguagesAsync`, `CheckHealthAsync`, `IsAvailable`, `RequiresInternet`, `Name`. `TranslationOrchestrator` implements `ITranslationService` with fallback chain and per-language backend overrides.
- **(b) Cloud Backend Implementations** — `DeepLBackend`, `GoogleBackend`, `AzureBackend` all coded in `Services/Translation/CloudTranslationBackend.vb`. Each has `Configure(apiKey)` method. Return `IsAvailable=False` until API keys set.
- `NllbBackend` wraps existing `TranslationService` as an `ITranslationBackend`
- All backends registered in Kestrel DI container
- **(e) Hybrid Mode** — `TranslationOrchestrator.LanguageOverrides` dictionary supports per-language backend selection with NLLB as always-available fallback

**What's NOT done:**
- **(c) Configuration UI** — No Settings tab UI for backend selection or API key entry
- **(d) Language Mapping** — `ILanguageMapService` interface defined but not implemented (no `language-codes.json`)
- **(f) Cost Awareness** — No usage tracking or budget limits
- **(g) Latency Considerations** — No batching or latency indicators
- **(h) Glossary Integration** — Not wired to cloud backends
- NllbBackend not yet registered in DI (needs legacy TranslationService instance from FormMain)

**Original problem description (kept for context):**

Not started. Translation is currently NLLB-200 only, running locally via the Python sidecar. No option to use cloud translation services.

**Problem:** NLLB-200 is excellent for offline use, but cloud translation APIs (Google Translate, DeepL, Azure Translator) generally produce higher-quality translations, especially for less-resourced languages. When internet is available and accuracy matters more than offline capability, users should be able to choose a cloud backend. This also opens the door for organisations that already have API keys/contracts with translation providers.

**Implementation:**

### a) Translation Backend Abstraction

Refactor the translation pipeline to support pluggable backends:

```
ITranslationBackend
  ├── NllbBackend        (existing — local Python sidecar)
  ├── DeepLBackend       (cloud API)
  ├── GoogleBackend      (cloud API)
  ├── AzureBackend       (cloud API)
  └── CustomApiBackend   (user-defined endpoint)
```

**Common interface:**
- `Translate(text As String, sourceLang As String, targetLangs As List(Of String)) As Dictionary(Of String, String)`
- `GetSupportedLanguages() As List(Of LanguageInfo)`
- `IsAvailable() As Boolean` (health check — is the API reachable / is the local server running?)
- `RequiresInternet As Boolean`
- `Name As String` (display name for UI)

The existing `TranslationService.vb` becomes the orchestrator that delegates to the active backend.

### b) Cloud Backend Implementations

**DeepL:**
- REST API: `POST https://api-free.deepl.com/v2/translate` (free tier: 500K chars/month) or `api.deepl.com` (paid)
- Supports 33 languages — covers all major European languages well
- Known for high quality, especially European language pairs
- Config: API key only

**Google Cloud Translation:**
- REST API: `POST https://translation.googleapis.com/language/translate/v2`
- Supports 130+ languages
- Good general coverage, competitive quality
- Config: API key

**Azure Translator:**
- REST API: `POST https://api.cognitive.microsofttranslator.com/translate`
- Supports 130+ languages
- Free tier: 2M chars/month
- Config: subscription key + region

**Custom API:**
- User provides a URL endpoint that accepts the same JSON format
- For organisations running their own translation infrastructure
- Config: endpoint URL, optional API key header

### c) Configuration UI

On the Settings tab, new "Translation" section:

- **Backend selector:** dropdown — "Offline (NLLB-200)" / "DeepL" / "Google Translate" / "Azure Translator" / "Custom API"
- **API key field:** text input, shown only for cloud backends, stored encrypted in AppConfig
- **Test button:** sends a sample translation request, shows result and latency
- **Fallback toggle:** "Fall back to offline (NLLB) if cloud API is unreachable" — enabled by default
- **Status indicator:** shows current backend health (connected/disconnected/rate-limited)

### d) Language Mapping

Each backend uses different language codes:
- NLLB: `fra_Latn`, `deu_Latn`, `pol_Latn`
- DeepL: `FR`, `DE`, `PL`
- Google/Azure: `fr`, `de`, `pl`

Create a language mapping table (`language-codes.json`) that maps between all formats:

```json
{
  "french": {
    "nllb": "fra_Latn",
    "deepl": "FR",
    "google": "fr",
    "azure": "fr",
    "display_name": "French",
    "native_name": "Français"
  }
}
```

The backend abstraction handles translation between code formats transparently.

### e) Hybrid Mode

Allow mixing backends for best results:
- Primary backend: DeepL (high quality for supported languages)
- Fallback for unsupported languages: NLLB (covers ~200 languages)
- Example: DeepL handles French/German/Spanish; NLLB handles Catalan/Albanian/Georgian
- Configuration: per-language backend override (advanced setting)

### f) Cost Awareness

Cloud APIs charge per character. Show usage tracking:
- Characters translated this session / this month
- Estimated cost based on known pricing (DeepL: €5/1M chars, Google: $20/1M chars)
- Warning when approaching free tier limits
- Option to set a monthly character budget — switch to NLLB when exceeded

### g) Latency Considerations

Cloud APIs add network latency (100-500ms per request) vs NLLB's local processing (~50ms):
- Batch translations: collect text for 1-2 seconds before sending to reduce API calls
- Show latency indicator in the UI so the operator knows the translation delay
- If latency exceeds a threshold (e.g., 2 seconds), warn and suggest switching to offline

### h) Glossary Integration

Some cloud APIs support their own glossary features:
- **DeepL:** built-in glossary API — upload term pairs, applied automatically
- **Google:** AutoML custom models (more complex)
- **Azure:** Custom Translator (training required)

For simplicity, apply the existing local glossary (Feature #5) as a post-processing step on cloud translations too — same wrong→right word replacement. This means glossary corrections work identically regardless of backend.

**Files to modify:**
- New file: `ITranslationBackend.vb` — interface definition
- New file: `NllbBackend.vb` — wraps existing TranslationService.vb logic
- New file: `DeepLBackend.vb` — DeepL API client
- New file: `GoogleTranslateBackend.vb` — Google API client
- New file: `AzureTranslatorBackend.vb` — Azure API client
- New file: `CustomApiBackend.vb` — user-defined endpoint client
- New file: `language-codes.json` — cross-backend language code mapping
- `TranslationService.vb` — refactor to orchestrator role, backend selection logic, fallback chain
- `FormMain.vb` — backend selector UI, API key config, test button, usage display
- `AppConfig.vb` — backend selection, encrypted API keys, usage counters, per-language overrides
- `SubtitleServer.vb` — no changes needed (receives translated text from TranslationService regardless of backend)
- `Strings.*.resx` — backend names, config labels, usage warnings

**Dependencies:** `System.Net.Http` (built into .NET 8). No external packages — all cloud APIs are simple REST endpoints.

**Complexity:** Medium-High. The abstraction layer and individual backend clients are straightforward. The complexity is in: fallback logic, language code mapping, hybrid mode routing, and cost tracking. Each backend should be implemented incrementally — start with DeepL (best quality for European languages), then add others.

---

## 15. Server Infrastructure Upgrade — Kestrel

**Status:** **DONE** — Implemented on `feature/kestrel-migration` branch (12 commits). Kestrel replaces legacy SubtitleServer as the sole server. All sub-features (a-h) complete. Legacy `Pipeline/SubtitleServer.vb` is dead code.

**What was built:**
- `Server/KestrelHost.vb` — Kestrel hosted in-process on background thread, full DI container
- `Server/EndpointRegistration.vb` — All Minimal API endpoints (health, config, control, Bible, audio, metrics, cert, nosleep)
- `Server/ServerOptions.vb` — Config (ports, colors, BiblesDirectory, AllowRemote)
- `Server/Middleware/` — ErrorHandlingMiddleware, RequestLoggingMiddleware
- `Server/Hubs/SubtitleHub.vb` — WebSocket hub with backpressure
- `Services/Subtitle/SubtitleService.vb` — ISubtitleService with broadcast, history, client management
- `Services/Infrastructure/CertificateService.vb` — Self-signed cert (RSA 2048, SAN with local IPs)
- `Services/Infrastructure/MetricsService.vb` — Thread-safe metrics collection
- `wwwroot/` — Extracted static web client (index.html, css/app.css, js/app.js)
- FormMain cutover: `SubtitleSvc` property resolves from DI, all broadcasts routed through Kestrel
- Response compression: Brotli + Gzip (~63% reduction)
- HTTP/2 support, proper WebSocket middleware, static file caching

**Original problem description (kept for context):**

Current implementation uses HttpListener (HTTP) + raw TcpListener with manual SslStream (HTTPS). Functional but not designed for scale.

**Problem:** The existing SubtitleServer architecture works for 10-20 clients but has fundamental bottlenecks that prevent scaling to 100+ concurrent connections. As features grow (Bible queries, feedback endpoints, spec sheet serving, glossary suggestions, diagnostics API), the manual routing and request handling becomes increasingly fragile and hard to maintain. Every new endpoint must be duplicated across both HTTP and HTTPS code paths.

### Current Architecture — What Breaks at Scale

| Bottleneck | Impact |
|-----------|--------|
| Sequential WebSocket broadcast loop | 100 clients = 100 Task allocations per subtitle line, multiple times per second |
| HTML page rebuilt from scratch on every request | ~150-200KB string allocation + encoding per connection, no caching |
| HTTPS header parsing reads one byte at a time | Slow handshake under concurrent connections |
| No response compression | 150KB HTML page × 100 clients = 15MB over Wi-Fi hotspot just for page loads |
| Every route duplicated for HTTP and HTTPS paths | Maintenance burden, easy to miss routes on one path |
| No static file caching | CSS, JS, images (if added) re-served from memory on each request |
| Unbounded history queue | Multi-hour session = massive replay payload on reconnect |

### Target Architecture — Kestrel

Replace HttpListener + TcpListener with ASP.NET Core Kestrel, hosted in-process within the WinForms app. Kestrel is already part of the .NET 8 runtime — no new dependencies.

**Implementation:**

### a) Kestrel Host Inside WinForms

- Create a `WebApplication` builder in a background thread at startup
- Kestrel listens on the same ports (5080 HTTP, 5081 HTTPS)
- Self-signed certificate loaded from existing AppData path — same cert, just configured via Kestrel options instead of manual SslStream
- WinForms app remains the main process; Kestrel runs as a hosted service within it

```
FormMain (WinForms) ─── hosts ──→ WebApplication (Kestrel)
                                    ├── /          (subtitle client HTML)
                                    ├── /ws        (WebSocket endpoint)
                                    ├── /api/...   (REST endpoints)
                                    ├── /bible/... (Bible API — Feature #16)
                                    └── static files (cached, compressed)
```

### b) Unified Routing

All routes defined once, served over both HTTP and HTTPS automatically:

```
Current (duplicated):                    Target (unified):
  HTTP path:  manual string matching       app.MapGet("/", ServeHtml)
  HTTPS path: manual string matching       app.MapGet("/api/control", ControlHandler)
  (must maintain both in sync)             app.UseWebSockets()
                                           (one definition, both protocols)
```

New endpoints from other features slot in naturally:
- `app.MapPost("/api/feedback", FeedbackHandler)` — Feature #11A
- `app.MapPost("/api/glossary-suggestion", SuggestionHandler)` — Feature #11A
- `app.MapGet("/api/diagnostics", DiagnosticsHandler)` — Feature #4
- `app.MapGet("/api/hardware-score", HardwareHandler)` — Feature #12
- `app.MapGet("/bible/{reference}", BibleHandler)` — Feature #16

### c) Built-In Performance Features

Kestrel provides out of the box:

| Feature | Benefit |
|---------|---------|
| **Response compression** (gzip/brotli) | 150KB HTML → ~25KB compressed. 100 clients = 2.5MB instead of 15MB |
| **Static file middleware** | Serve HTML/CSS/JS from memory cache, proper ETags, conditional GET |
| **Connection pooling** | Efficient TCP connection reuse |
| **HTTP/2 support** | Multiplexed connections, header compression |
| **WebSocket middleware** | Standard upgrade handling, no manual byte parsing |
| **Request pipeline** | Middleware chain for auth, logging, compression in correct order |
| **Async everywhere** | True async I/O throughout, no thread-per-connection |
| **Backpressure** | Built-in flow control for slow clients |

### d) WebSocket Broadcast Improvements

Replace the sequential client loop with a more efficient pattern:

- **Pre-encode messages once:** JSON → bytes happens once, shared `ReadOnlyMemory<byte>` sent to all clients
- **Parallel fan-out:** Use `Parallel.ForEachAsync` or channel-based pattern for broadcast
- **Per-client send queue:** Instead of dropping messages when busy, queue up to N messages per client, drop oldest if full
- **History cap:** Limit `_committedLines` to last 500 entries. Phones reconnecting after hours don't need the entire session

### e) Migration Strategy

This is a significant refactor but can be done incrementally:

1. **Phase 1:** Stand up Kestrel alongside existing SubtitleServer, serving on different ports. Verify basic HTML + WebSocket works
2. **Phase 2:** Migrate routes one by one from SubtitleServer to Kestrel controllers
3. **Phase 3:** Remove HttpListener/TcpListener code entirely
4. **Phase 4:** Add new endpoints (Bible, feedback, diagnostics) directly on Kestrel

During migration, both servers can run simultaneously for testing. The phone client doesn't care which server it talks to — same WebSocket protocol, same JSON messages.

### f) Extracting the HTML Client

Currently the entire subtitle client (~800 lines of HTML/CSS/JS) is embedded as a VB.NET string literal in SubtitleServer.vb. This makes editing painful and prevents browser caching.

With Kestrel's static file middleware:
- Extract the client into separate files: `wwwroot/index.html`, `wwwroot/app.js`, `wwwroot/style.css`
- Embed as .NET resources or ship as files alongside the exe
- Browser caches JS/CSS — only fetches on version change
- Much easier to develop and debug the web client
- Can use proper JS tooling (linting, minification) if desired

### g) HTTPS Simplification

Current HTTPS is manual TcpListener → SslStream → parse HTTP headers byte by byte → route manually. With Kestrel:

```vb
webBuilder.ConfigureKestrel(Sub(opts)
    opts.ListenAnyIP(5080)  ' HTTP
    opts.ListenAnyIP(5081, Sub(listenOpts)
        listenOpts.UseHttps(certPath, certPassword)  ' HTTPS
    End Sub)
End Sub)
```

Same self-signed cert, same ports, but all the SSL/TLS handling, HTTP parsing, and connection management is handled by battle-tested framework code.

### h) Server Load Monitoring & Capacity Dashboard

The operator needs to know when the system is approaching its limits — before subtitles start lagging, before audio stutters, before phones start dropping. A real-time health dashboard on the desktop app gives early warning.

**Metrics to collect (sampled every 2 seconds):**

| Metric | Source | What it tells you |
|--------|--------|-------------------|
| **Connected clients** | WebSocket client count | Are we at 20 or 95? |
| **Clients by language** | ClientInfo dictionary | Which languages are active, load distribution |
| **WebSocket send queue depth** | Per-client pending messages | Are clients keeping up? Rising queue = clients falling behind |
| **Messages dropped (backpressure)** | Counter incremented on SendBusy skip | How many messages are being lost to slow clients |
| **Broadcast latency** | Time to complete one full broadcast loop | At 20 clients: <5ms. At 100: should still be <50ms. If >200ms, clients are seeing stale text |
| **HTTP request rate** | Kestrel request counter | Requests/sec — spikes on page load, Bible browsing |
| **HTTP response time (p95)** | Kestrel middleware timing | Are API responses fast? >500ms means something is choking |
| **Bytes out/sec** | Network counter | Actual bandwidth usage. Compare against Wi-Fi capacity |
| **CPU usage** | `Process.GetCurrentProcess()` | Server process CPU — includes Kestrel, WebSocket handling, TTS generation |
| **Memory usage** | `Process.WorkingSet64` | Memory pressure — leak detection over long sessions |
| **GPU usage** | `nvidia-smi` query (if available) | Is whisper/NLLB saturating the GPU? |
| **Transcription lag** | Time from audio capture to committed text | Is whisper keeping up with real-time speech? |
| **Translation lag** | Time from committed text to translated text broadcast | Is NLLB/cloud API keeping up? |
| **TTS generation time** | Time to generate audio clip | Is TTS keeping up? If generation > speech interval, audio falls behind |

**Implementation:**

```vb
Public Class ServerMetrics
    ' Gauges (current value)
    Public Property ConnectedClients As Integer
    Public Property ClientsByLanguage As Dictionary(Of String, Integer)
    Public Property BytesOutPerSecond As Long
    Public Property CpuPercent As Double
    Public Property MemoryMB As Long
    Public Property GpuPercent As Double
    Public Property GpuMemoryMB As Long

    ' Counters (cumulative, reset per session)
    Public Property TotalMessagessent As Long
    Public Property TotalMessagesDropped As Long
    Public Property TotalHttpRequests As Long
    Public Property TotalBytesOut As Long

    ' Latencies (rolling average over last 30 seconds)
    Public Property BroadcastLatencyMs As Double
    Public Property HttpResponseP95Ms As Double
    Public Property TranscriptionLagMs As Double
    Public Property TranslationLagMs As Double
    Public Property TtsGenerationMs As Double

    ' Timestamps
    Public Property SessionStarted As DateTime
    Public Property LastUpdated As DateTime
End Class
```

**Desktop UI — Status Bar + Detail Panel:**

Compact status bar at the bottom of the main form (always visible during a session):

```
[Clients: 47] [CPU: 32%] [GPU: 68%] [Net: 0.8 Mbps] [Lag: 1.2s] [●●●○○]
```

The five dots are the overall health traffic light:
- 5 green: all systems nominal
- 4 green, 1 amber: one metric approaching limit
- Mixed amber/red: specific problems, expand for details

Click the status bar to expand a detail panel showing all metrics with live updating charts (simple bar/line graphs using GDI+ drawing — no external charting library needed):

- Connection count over time (line graph)
- Bandwidth usage over time (line graph)
- Broadcast latency over time (line graph, with red threshold line)
- CPU/GPU usage (bar gauges)
- Per-language client breakdown (horizontal bars)
- Messages dropped counter (should stay at 0 — if rising, highlight red)

**Threshold-Based Alerts:**

Define warning and critical thresholds:

| Metric | Warning | Critical | Action |
|--------|---------|----------|--------|
| Connected clients | 75 | 95 | "Approaching connection limit — consider a second server" |
| CPU usage | 70% | 90% | "High CPU — transcription may lag" |
| GPU usage | 80% | 95% | "GPU saturated — consider smaller whisper model" |
| Broadcast latency | 100ms | 500ms | "Subtitle delivery slowing — some phones may see delays" |
| Messages dropped/min | 10 | 50 | "Messages being dropped — slow clients can't keep up" |
| Transcription lag | 3s | 8s | "Transcription falling behind real-time speech" |
| Translation lag | 2s | 5s | "Translations delayed — consider switching to cloud API" |
| Memory usage | 2 GB | 3.5 GB | "High memory usage — long session may need restart" |
| Bytes out/sec | 5 Mbps | 15 Mbps | "High bandwidth — check Wi-Fi capacity" |

When a warning threshold is crossed:
- Status bar dot turns amber for that category
- Tooltip shows the specific issue
- Log entry in pipeline debug log

When critical threshold is crossed:
- Status bar dot turns red
- Pop-up notification (non-modal, auto-dismiss after 10 seconds): "GPU at 95% — transcription may fall behind"
- Audible beep option (configurable — don't want it going off during a sermon)

**Metrics API Endpoint:**

Expose metrics via Kestrel for external monitoring or the phone admin panel:

```
GET /api/metrics → full metrics JSON (admin only)
GET /api/health  → simple OK/WARN/CRITICAL status (public)
```

The phone admin panel (existing) can show a simplified version:
- Traffic light indicator
- Client count
- "System healthy" / "System under load" / "System overloaded"

**Metrics in Session Report (Feature #11A section c):**

After each session, include in the auto-generated report:
- Peak client count and when it occurred
- Peak CPU/GPU usage
- Total data transferred
- Number of messages dropped (if any)
- Any threshold alerts that fired, with timestamps
- Session duration
- Average and peak broadcast latency

This gives the dev team performance data from real field deployments to tune thresholds and identify bottlenecks.

**Metrics in Diagnostics Bundle (Feature #4):**

Include a `metrics-snapshot.json` in the diagnostics ZIP:
- Current values of all metrics
- Last 5 minutes of metric history (sampled every 2 seconds = 150 data points)
- Any active alerts
- Allows the dev team to see system state at the moment the operator hit "Export Diagnostics"

**Files to modify:**
- `SubtitleServer.vb` — major refactor: replace HttpListener/TcpListener with Kestrel WebApplication host. Migrate all route handlers to Minimal API endpoints. Extract embedded HTML/JS/CSS to separate files
- New directory: `wwwroot/` — extracted web client files (index.html, app.js, style.css)
- New file: `ServerMetrics.vb` — metrics collection class, threshold definitions, alert logic
- New file: `MetricsMiddleware.vb` — Kestrel middleware for request timing, byte counting
- `FormMain.vb` — update server start/stop to use Kestrel host lifecycle, status bar with health indicators, expandable metrics detail panel
- `FormMain.Designer.vb` — status bar layout, metrics panel controls
- `TranscriptionTools.vbproj` — add `Microsoft.AspNetCore.App` framework reference (already in .NET 8 runtime, just needs the reference)
- `AppConfig.vb` — server configuration (ports, cert path) compatible with Kestrel options, alert threshold overrides
- `Diagnostics.vb` (Feature #4) — include metrics snapshot in diagnostics bundle
- `Strings.*.resx` — alert messages, status bar labels, metrics panel labels

**Dependencies:** `Microsoft.AspNetCore.App` framework reference (included in .NET 8 runtime — no additional download, already on every machine running the app).

**Complexity:** High. This is the largest single refactor in the plan. SubtitleServer.vb is ~1870 lines with deeply intertwined HTTP handling, WebSocket management, and embedded HTML. However, the migration can be done incrementally (run both servers during transition), and the end result dramatically simplifies every subsequent feature that needs a server endpoint. The load monitoring adds moderate complexity on top (mostly sampling timers and GDI+ drawing for charts) but is essential for operating at scale. This is an investment that pays for itself across Features #1, #4, #11, #12, #13, #14, and #16.

---

## 16. Bible Integration

**Status:** Partially implemented on `feature/kestrel-migration` branch.

**What's done:**
- **(a) Bible Data Source** — `Services/Bible/BibleService.vb` scans `Bibles/{lang_code}/` for SQLite files. Reads ISO language codes from DB `info` table. Tested with KJV+ (en), BCI (ca), NVIC'17 (es). Configurable path via `ServerOptions.BiblesDirectory`. Strips markup tags (`<S>`, `<J>`, `<pb/>`).
- **(b) Bible API Endpoints** — All working: `/bible/translations`, `/bible/{id}/{book}/{chapter}`, `/bible/{id}/{book}/{chapter}/{verses}`, `/bible/search`, `/bible/parse`
- **(c) Reference Parser** — BookAliases dictionary (English names/abbreviations), regex pattern matching for chapter:verse format. `DetectReferencesInText()` method coded.
- **(e) Reference Detection** — `DetectReferencesInText()` coded in BibleService but NOT yet wired into WebSocket commit messages.

**What's NOT done:**
- **(d) Phone Client Bible Tab** — No Bible browsing/search UI on the phone client yet
- **(e) WebSocket integration** — Reference detection exists but not wired to enrich commit messages with `bible_refs`
- **(f) Offline Bible Bundles** — No download/management UI

**Original problem description (kept for context):**

Not started. No Bible data, search, or verse lookup functionality exists in the application.

**Problem:** Agape's work is centred on communicating the Bible across languages. When a speaker references a Bible verse, listeners need to see it in their own language — not a machine translation of the speaker's quote, but the actual published Bible text in their language. Machine-translating scripture produces approximations; serving the real text produces trust.

Beyond verse display, having a searchable multilingual Bible on every listener's phone — accessible offline through the same local server — turns the transcription tool into a genuinely useful ministry companion.

**Implementation:**

### a) Bible Data Source

Use freely available Bible translations in structured format:

**Data format:** OSIS XML or USX (Unified Scripture XML) — standard Bible markup formats used by most digital Bible projects.

**Sources for free/open translations:**
- **eBible.org** — hundreds of translations in OSIS/USX format, many Creative Commons licensed
- **Digital Bible Library (DBL)** — requires partnership agreement but has excellent coverage
- **Crosswire** (SWORD modules) — large library, various open licenses

**Storage:**
- Pre-packaged SQLite database per language: `bibles/{lang}/bible.db`
- Schema:

```sql
CREATE TABLE translations (
    id TEXT PRIMARY KEY,         -- e.g., "ESV", "RVR1960", "LSG"
    language TEXT NOT NULL,      -- NLLB/ISO code
    name TEXT NOT NULL,          -- "English Standard Version"
    license TEXT,                -- license info
    copyright TEXT               -- attribution text
);

CREATE TABLE verses (
    translation_id TEXT NOT NULL,
    book TEXT NOT NULL,          -- "GEN", "MAT", "REV" (OSIS abbreviations)
    chapter INTEGER NOT NULL,
    verse INTEGER NOT NULL,
    text TEXT NOT NULL,
    PRIMARY KEY (translation_id, book, chapter, verse)
);

CREATE TABLE books (
    abbrev TEXT PRIMARY KEY,     -- "GEN", "EXO", "MAT"
    name_en TEXT NOT NULL,       -- "Genesis"
    testament TEXT NOT NULL,     -- "OT" / "NT"
    sort_order INTEGER NOT NULL
);

-- Full-text search index
CREATE VIRTUAL TABLE verses_fts USING fts5(text, content=verses, content_rowid=rowid);
```

- **Size estimate:** ~5MB per translation (compressed). 20 languages = ~100MB total
- Pre-built databases shipped with the app or downloadable per language

### b) Bible API Endpoints (on Kestrel — Feature #15)

```
GET  /bible/translations                    → list available translations
GET  /bible/translations/{lang}             → translations available in a language
GET  /bible/{translation}/books             → list of books
GET  /bible/{translation}/{book}/{chapter}  → full chapter text
GET  /bible/{translation}/{book}/{ch}/{vs}  → single verse
GET  /bible/{translation}/{book}/{ch}/{vs1}-{vs2}  → verse range
GET  /bible/search?q={query}&lang={lang}    → full-text search
GET  /bible/reference?ref={ref}&lang={lang} → parse human reference ("John 3:16") → verse(s)
```

All endpoints return JSON:

```json
{
  "reference": "John 3:16",
  "translation": "LSG",
  "language": "fra_Latn",
  "verses": [
    {
      "book": "JHN",
      "chapter": 3,
      "verse": 16,
      "text": "Car Dieu a tant aimé le monde qu'il a donné son Fils unique..."
    }
  ],
  "copyright": "Public Domain"
}
```

### c) Reference Parser

Parse human-readable Bible references in multiple formats and languages:

- English: "John 3:16", "Gen 1:1-3", "1 Cor 13:4-7", "Psalm 23"
- Spanish: "Juan 3:16", "Gn 1:1-3"
- French: "Jean 3:16"
- Abbreviated: "Jn 3:16", "Gn 1:1", "Ps 23"
- Ranges: "Matthew 5:3-12", "Romans 8:28-39"
- Whole chapters: "Psalm 23" (no verse = full chapter)

Implementation:
- Book name alias table: `{"john": "JHN", "juan": "JHN", "jean": "JHN", "johannes": "JHN", ...}`
- Regex pattern matching for chapter:verse[-verse] format
- Fuzzy matching for misspelled book names

### d) Phone Client — Bible Tab

Add a Bible section to the phone web client (alongside the subtitle view):

**Browse mode:**
- Book list → Chapter list → Verse display
- Translation selector (shows available translations for the client's chosen language)
- Clean, readable typography — designed for reading scripture on a phone

**Search mode:**
- Text search box — full-text search across the selected translation
- Results show verse reference + text with highlighted matches
- Search in the client's language

**Quick reference:**
- Input box: type "John 3:16" → shows the verse immediately
- Share button: copy verse text to clipboard

**Integration with subtitles:**
- When the speaker says a Bible reference and it appears in the transcript, auto-detect it and show a tappable link in the subtitle view
- Tapping opens the verse in the Bible tab, in the listener's language
- This is the killer feature: speaker says "turn to Romans 8:28" in Spanish, the French listener sees "Romains 8:28" as a link, taps it, and reads the actual French Bible text

### e) Bible Reference Detection in Transcription

Automatically detect Bible references in transcribed/translated text:

- Pattern matching on the committed text for book names + chapter:verse patterns
- When detected, enrich the WebSocket message with structured reference data:

```json
{
  "type": "commit",
  "text": "As it says in Romans 8:28, all things work together...",
  "bible_refs": [
    {"ref": "ROM.8.28", "display": "Romans 8:28", "start": 17, "end": 28}
  ]
}
```

- Phone client renders detected references as tappable links
- Links fetch the verse from `/bible/reference` in the client's language
- Graceful degradation: if the Bible translation isn't available for a language, just show the reference as plain text

### f) Offline Bible Bundles

For field deployment (ties into Feature #7 — Portable USB):

- Bible databases included in the portable USB package
- Language selection during Event Setup (Feature #2): "Which Bible translations should be available?"
- Download manager on Settings tab for adding new translations when internet is available
- Each translation is a single SQLite file — easy to copy via USB between devices

### g) Copyright and Licensing

Bible translations have specific licensing requirements:

- Store copyright/attribution per translation in the database
- Display copyright notice when showing text from a copyrighted translation
- Some translations require: display of copyright on every page, no modification of text, attribution
- Public domain translations (KJV, LSG 1910, Reina-Valera 1909, etc.) have no restrictions
- Modern translations (NIV, ESV, NLT) typically require licensing agreements for digital distribution
- **Recommendation:** Start with public domain translations for each major European language, then pursue licensing for modern translations if Agape has existing agreements with publishers

### h) Available Public Domain Translations (Starting Set)

| Language | Translation | Year | Notes |
|----------|------------|------|-------|
| English | KJV (King James Version) | 1769 | Public domain worldwide |
| Spanish | RVA (Reina-Valera Antigua) | 1909 | Public domain |
| French | LSG (Louis Segond) | 1910 | Public domain |
| Portuguese | ARA (Almeida Revisada) | 1959 | Public domain in many jurisdictions |
| German | Luther 1912 | 1912 | Public domain |
| Italian | Diodati | 1894 | Public domain |
| Romanian | Cornilescu | 1924 | Public domain |
| Dutch | SVV (Statenvertaling) | 1750 | Public domain |
| Polish | BG (Biblia Gdańska) | 1881 | Public domain |
| Russian | Synodal | 1876 | Public domain |
| Czech | Kralická | 1613 | Public domain |
| Hungarian | Károli | 1908 | Public domain |
| Greek | Modern Greek NT | Various | Several public domain options |
| Ukrainian | Ogienko | 1962 | Public domain |
| Croatian | Šarić | 1942 | Check status per jurisdiction |

This gives baseline coverage for the majority of Agape's European footprint using freely distributable texts.

**Files to modify:**
- New directory: `bibles/` — SQLite database files per language
- New file: `BibleService.vb` — database access, reference parsing, search
- New file: `BibleController.vb` (or Minimal API handlers) — REST endpoints on Kestrel
- New file: `bible-books.json` — book names, aliases, abbreviations across languages
- New file: `build-bible-db.py` — script to convert OSIS/USX source files into SQLite databases
- `wwwroot/index.html` / `wwwroot/app.js` (Feature #15) — Bible tab UI in phone client
- `SubtitleServer.vb` (or Kestrel WebSocket handler) — Bible reference detection in committed text, enriched WebSocket messages
- `FormMain.vb` — Bible translation management UI (download, select available translations)
- `AppConfig.vb` — selected Bible translations per language
- `Strings.*.resx` — Bible UI labels, book names (if localised in desktop app)
- `TranscriptionTools.vbproj` — add `Microsoft.Data.Sqlite` NuGet package

**Dependencies:**
- `Microsoft.Data.Sqlite` — lightweight SQLite access for .NET (NuGet package, ~1MB)
- Bible source data from eBible.org or equivalent (build-time dependency for creating databases)

**Complexity:** High. Multiple components: data pipeline (OSIS→SQLite), API layer, phone client UI, reference parser, transcript integration. However, each piece is independently useful — even just the Bible browse/search tab without transcript integration adds significant value. Build incrementally:
1. First: SQLite database + API endpoints + phone browse UI
2. Then: search functionality
3. Then: reference detection in transcriptions
4. Then: auto-linking in subtitle view

---

## Implementation Order

Recommended sequence based on impact, dependencies, and complexity. Designed for a solo developer with a 300-person church congregation as a live testing environment.

### Phase 1 — Quick Wins (no server changes, high visibility)

These features require zero new SubtitleServer endpoints. They deliver immediate, visible value to the congregation.

1. **QR Code Connection (#1)** — biggest UX win, low effort. People can connect by pointing their phone camera at a code instead of typing an IP address
2. **Hardware Readiness Score (#12)** — catches bad hardware immediately. Prevents wasted setup time on underpowered machines
3. **Recommended Specifications Generator (#13)** — ties into hardware score. Answers "what laptop should we buy?"
4. **Audio Level Monitor (#3)** — Python endpoint + VB.NET polling. Prevents the #1 failure mode (bad audio → garbage transcription)
5. **Diagnostic Bundle (#4)** — uses HardwareScanner from #12. Enables remote support from day one

### Phase 2 — Operator Experience (still no server changes)

6. **Setup Wizard expansion (#2)** — integrates QR, audio monitor, hardware score from Phase 1. The wizard becomes a comprehensive pre-event checklist
7. **Crash Recovery (#8)** — watchdog, session persistence, auto-restart. Reliability for unsupervised operation
8. **Glossary Packs (#5)** — import/export, simplified editing UI. Christian terminology for Agape's context
9. **Multi-Language UI expansion (#9)** — add 10 locale files for Agape's European footprint. Can run in parallel with other work (requires volunteer translators)
10. **Session Export (#10)** — multi-format transcript output. Multiplies the value of each session

### Phase 3 — Kestrel Migration (#15)

Do this AFTER 10 features are shipped and battle-tested. The migration is focused: no feature pressure, just infrastructure. By this point the architecture's limitations are well-understood from real field use.

Delivers:
- Unified routing (no HTTP/HTTPS duplication)
- Response compression (150KB → 25KB per page load)
- Static file caching with ETags
- Extracted web client (`wwwroot/` — no more HTML-in-VB-strings)
- Server load monitoring and capacity dashboard (section 15h)

**Why wait:** Kestrel delivers zero user-visible value on its own — the 300-person church won't notice it. Shipping 10 features first maintains momentum and gives the congregation real improvements. When the migration happens, it's a clean refactor with no feature pressure.

### Phase 4 — Features that benefit from Kestrel

These features either require new HTTP endpoints (easier on Kestrel) or benefit from the extracted web client.

11. **Device Compatibility & Suitability Scoring (#11C)** — can partially ship pre-Kestrel via WebSocket + client-side checks. Full implementation benefits from Kestrel's static file serving for `compatibility.json`
12. **Field Feedback System (#11A)** — POST endpoints for feedback/suggestions. Simple on Kestrel, duplicated work on old architecture
13. **Glossary Enrichment Pipeline (#11B)** — depends on #11A for suggestion data. Desktop-side review UI
14. **Pluggable Translation Backends (#14)** — pure VB.NET abstraction layer, no server needed. But pairs well with Kestrel for status/config API endpoints
15. **Server-Side TTS (#6)** — Python sidecar + cached audio served via Kestrel static files. High value but high complexity
16. **Bible Integration (#16)** — REST API with parameterized routes. The only feature that truly *requires* Kestrel's routing (impractical with manual string matching)

### Phase 5 — Maximum Portability

17. **Portable USB Deployment (#7)** — last, when the feature set is stable. Cross-cutting concern that touches path resolution everywhere. Bible databases, TTS voice models, and all other assets bundled for offline USB deployment

---

## Appendix A: Bandwidth Analysis & Optimization Strategy

### Raw Bandwidth Requirements (100 Concurrent Clients)

**A1. Text subtitles (WebSocket)**

| Metric | Value |
|--------|-------|
| Committed line payload | ~150-250 bytes JSON |
| Interim update payload | ~200-300 bytes JSON |
| Peak message rate | ~2 messages/sec (1 interim + occasional commit) |
| Per client | ~600 bytes/sec |
| **100 clients uncompressed** | **60 KB/s = 0.5 Mbps** |
| **100 clients compressed (permessage-deflate)** | **~15-20 KB/s = 0.15 Mbps** |

Verdict: trivial. Even uncompressed, this is negligible on any Wi-Fi network.

**A2. Server-side TTS audio streaming**

| Codec | Bitrate (mono) | Per sentence (~5 sec) | 100 clients × 1 sentence | Sustained (1 sentence every 8 sec) |
|-------|---------------|----------------------|--------------------------|-----------------------------------|
| WAV 16kHz 16-bit | 256 kbps | 160 KB | 16 MB burst | ~20 Mbps |
| MP3 32kbps | 32 kbps | 20 KB | 2 MB burst | ~2.5 Mbps |
| **Opus 16kbps** | **16 kbps** | **10 KB** | **1 MB burst** | **~1.25 Mbps** |
| Opus 24kbps (better quality) | 24 kbps | 15 KB | 1.5 MB burst | ~1.9 Mbps |

**Critical optimization — generate once per language, not per client:**
In a typical session with 5-8 active translation languages, you generate 5-8 audio clips per sentence, not 100. If 30 French listeners all need the same audio, encode it once and send the same bytes to all 30.

| Scenario | Without dedup | With per-language dedup |
|----------|--------------|------------------------|
| 100 clients, 6 languages, Opus 24kbps | 1.9 Mbps | ~0.3 Mbps (6 streams shared) |
| 100 clients, 6 languages, MP3 32kbps | 2.5 Mbps | ~0.4 Mbps |

**A3. Bible access**

| Metric | Value |
|--------|-------|
| Single verse response | ~200-500 bytes JSON |
| Full chapter response | ~5-20 KB JSON |
| Full chapter compressed | ~2-8 KB (gzip) |
| Peak burst (speaker says a reference, all 100 tap it) | 100 × 500 bytes = 50 KB |
| Sustained browsing (~10 requests/sec across all clients) | 200 KB/s = 1.6 Mbps |
| Sustained browsing compressed | ~60 KB/s = 0.5 Mbps |

Verdict: very manageable. Bible text compresses extremely well (repetitive structure, common words).

**A4. Initial page load (100 phones scan QR code simultaneously)**

| Metric | Current | With Kestrel |
|--------|---------|-------------|
| HTML + JS + CSS payload | ~150-200 KB | ~150-200 KB (same content) |
| Compressed | N/A (no compression) | ~25-35 KB (gzip) / ~20-28 KB (brotli) |
| 100 phones | 15-20 MB | 2.5-3.5 MB |
| Time on 50 Mbps hotspot | 3-4 seconds | < 1 second |
| With caching (return visit) | Still 15-20 MB (no-cache headers) | ~0 (304 Not Modified, ETag match) |

**A5. Total worst-case simultaneous**

| Traffic type | Unoptimized | Optimized |
|-------------|-------------|-----------|
| Subtitles (text) | 0.5 Mbps | 0.15 Mbps |
| TTS audio (6 languages) | 2.5 Mbps | 0.4 Mbps |
| Bible browsing | 1.6 Mbps | 0.5 Mbps |
| Page loads (burst) | 40 Mbps burst | 7 Mbps burst |
| **Sustained total** | **~5 Mbps** | **~1 Mbps** |
| **Peak burst** | **~45 Mbps** | **~8 Mbps** |

A portable Wi-Fi hotspot (Wi-Fi 5/6) typically handles 50-300 Mbps between local devices. Optimized traffic fits comfortably with headroom. Unoptimized is fine for sustained but the page-load burst could stutter on cheap hotspots.

### Optimization Strategies

**O1. Response Compression (Kestrel middleware — Feature #15)**

```vb
app.UseResponseCompression()  ' gzip + brotli out of the box
```

- All HTTP responses (HTML, JSON, CSS, JS) compressed automatically
- Reduces page load by ~80%, API responses by ~70%
- No client-side changes needed — every modern browser supports gzip
- **Impact:** High. Single biggest bandwidth reduction.

**O2. WebSocket Compression (permessage-deflate)**

```vb
app.UseWebSockets(New WebSocketOptions With {
    .KeepAliveInterval = TimeSpan.FromSeconds(30)
    ' Kestrel supports permessage-deflate negotiation
})
```

- Compresses each WebSocket frame — subtitle text compresses ~70-80% (repetitive JSON structure, natural language text)
- Negotiated per-connection; falls back to uncompressed if client doesn't support it
- Slight CPU cost per frame — negligible for text, worth monitoring if also streaming audio over WebSocket
- **Impact:** Medium. Subtitles are already small; this helps most with large history replay on reconnect.

**O3. Static File Caching (Kestrel middleware — Feature #15)**

```vb
app.UseStaticFiles(New StaticFileOptions With {
    .OnPrepareResponse = Sub(ctx)
        ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000"  ' 1 year
    End Sub
})
```

- Append version hash to filenames: `app.v1.4.0.js`, `style.v1.4.0.css`
- Browser caches JS/CSS indefinitely; only re-downloads on version change
- `index.html` uses short cache (e.g., 5 minutes) or ETag for freshness
- After first visit, return visitors download essentially nothing
- **Impact:** High for repeat connections (same session reconnects, multi-day events). Zero transfer after first load.

**O4. TTS Audio — Generate Once, Serve Many**

The server generates TTS audio per language, not per client:

```
Committed text: "God loves you"
  → French TTS: generate once → cache as /tts/cache/fra_commit_42.opus
  → German TTS: generate once → cache as /tts/cache/deu_commit_42.opus
  → Polish TTS: generate once → cache as /tts/cache/pol_commit_42.opus

30 French clients all fetch /tts/cache/fra_commit_42.opus → same cached bytes
```

Implementation:
- TTS server (Feature #6) generates audio, writes to a ring-buffer cache directory
- Each clip keyed by: language + commit ID
- Served as a static file via Kestrel — automatic caching, compression, range requests
- Cache eviction: keep last 100 clips per language, delete oldest
- Client fetches audio URL from the WebSocket commit message: `{"type": "commit", "text": "...", "tts_url": "/tts/cache/fra_commit_42.opus"}`

**O5. Audio Codec Selection**

| Codec | Browser support | Quality at low bitrate | Offline generation |
|-------|----------------|----------------------|-------------------|
| **Opus** | Chrome, Firefox, Edge, Safari 15+ | Excellent at 16-24 kbps | Supported by Piper TTS |
| MP3 | Universal | Acceptable at 32 kbps | Widely supported |
| AAC | Safari-preferred, all modern | Good at 32 kbps | Needs ffmpeg |
| WAV | Universal | Perfect (uncompressed) | Native — but too large |

**Recommendation:** Generate Opus as primary (best quality per byte), MP3 as fallback for older Safari. Client sends codec support in device fingerprint (Feature #11C); server returns the best format the client supports.

```javascript
// Client-side codec detection
var audio = document.createElement('audio');
var canOpus = audio.canPlayType('audio/ogg; codecs=opus') !== '';
var canMp3 = audio.canPlayType('audio/mpeg') !== '';
// Send in WebSocket handshake
```

**O6. Bible Client-Side Caching**

Once a phone fetches a Bible chapter, cache it locally:

```javascript
// On chapter fetch
var cacheKey = 'bible_' + translation + '_' + book + '_' + chapter;
localStorage.setItem(cacheKey, JSON.stringify(response));

// On next request, check cache first
var cached = localStorage.getItem(cacheKey);
if (cached) { render(JSON.parse(cached)); return; }
```

- Bible text never changes — infinite cache validity
- A typical phone session might access 5-10 chapters = 50-100 KB cached
- After warming the cache, Bible browsing generates zero network traffic
- `localStorage` limit is ~5-10 MB — enough for hundreds of chapters
- For larger needs: use Cache API or IndexedDB

**O7. Prefetch and Predictive Loading**

- When a client loads a Bible chapter, prefetch the next and previous chapters in the background
- When a Bible reference is detected in subtitles, prefetch the verse before the user taps it
- Prefetch the `compatibility.json` and `specs.json` files on initial page load (small, needed later)

**O8. Message Batching**

Instead of sending every interim update as a separate WebSocket frame:

- Batch updates within a 200ms window into a single frame
- Reduces frame overhead (each WebSocket frame has 2-14 bytes of header)
- Reduces system call overhead (fewer send() calls)
- Negligible latency impact (200ms is below human perception for reading text)

**O9. Binary WebSocket Protocol (Future)**

Current: JSON text frames (`{"type":"commit","id":42,"text":"..."}`)
Future option: binary protocol using MessagePack or Protocol Buffers

| Format | Typical commit message | Compressed |
|--------|----------------------|-----------|
| JSON text | ~200 bytes | ~80 bytes |
| MessagePack | ~120 bytes | ~60 bytes |

Saves ~40% per message. At 100 clients and 2 messages/sec, this saves ~16 KB/s — not critical, but worthwhile if implementing from scratch. **Not recommended as a migration** from the existing JSON protocol unless there's a specific bottleneck. The JSON format is debuggable and human-readable, which matters more during development.

**O10. History Replay Cap**

Currently `_committedLines` grows unbounded during a session. After a 2-hour sermon at ~1 commit every 8 seconds, that's ~900 entries × ~200 bytes = ~180 KB of history replayed to every reconnecting client.

- Cap at 200 entries (configurable)
- On reconnect, send only entries newer than `lastId` (already supported), but cap the replay to the most recent 200 even if `lastId` is older
- Show a "session started earlier — showing recent subtitles" notice on the client

### Network Hardware Recommendations

Include in the Recommended Specifications (Feature #13):

| Audience Size | Wi-Fi Recommendation | Notes |
|--------------|---------------------|-------|
| 1-20 | Phone hotspot or any router | Even 2.4 GHz is fine at this scale |
| 20-50 | Dedicated portable router (Wi-Fi 5+) | TP-Link travel router or similar, 5 GHz band preferred |
| 50-100 | Dual-band router (Wi-Fi 6) | 5 GHz for phones, 2.4 GHz as fallback. Separate from any internet uplink |
| 100+ | Enterprise AP or mesh system | Multiple APs, load balancing, dedicated subnet |

Key advice for operators:
- **Use 5 GHz band** — less interference, higher throughput per client
- **Dedicated network** — don't share the subtitle Wi-Fi with general internet access
- **No internet needed** — the router just needs to create a local network, no WAN uplink required
- **Position the router centrally** — every phone needs line-of-sight or near it
- **Test before the event** — connect 5+ phones, verify subtitles flow, check for dropouts

---

## Development Notes

**Solo developer context:** This plan is maintained by one person (Jeremy) and developed incrementally alongside a live deployment serving a 300-person church congregation. Every Sunday is a live test with real users who don't speak the same language as the speaker.

**Testing strategy:** The congregation is the test bed. Features ship when they work locally, then get real-world validation every week. This means:
- Ship small, ship often — each feature is independently deployable
- Visible features first — the congregation provides feedback on things they can see and use
- Infrastructure later — Kestrel, backend abstractions, etc. deliver developer quality-of-life but zero user-visible improvement

**Why Kestrel is Phase 3, not Phase 1:** Analysis of all 16 features against the current HttpListener/TcpListener architecture shows that 13 of 15 non-Kestrel features need zero new SubtitleServer endpoints. Only Bible Integration (#16) truly requires Kestrel's routing. Field Feedback (#11A) would benefit but is buildable without it. The duplicated HTTP/HTTPS routing is a maintenance burden, not a user-facing problem — and it's been working reliably for the existing feature set.

**Feature #11 split rationale:** The original Feature #11 contained 7 subsections spanning feedback collection, glossary review, distributed workflows, device fingerprinting, compatibility databases, TTS auditing, and device scoring. These are 3+ distinct systems with different dependencies, complexities, and deployment timelines. Split into:
- **#11A (Field Feedback)** — collects data (phone UI + file I/O)
- **#11B (Glossary Enrichment)** — processes data (desktop review UI, depends on #11A)
- **#11C (Device Compatibility)** — independent system (client-side detection + server logging, can ship without #11A/#11B)

---

## Notes

- Each feature should include its own i18n strings added to ALL existing locale files
- All new Python endpoints should include health checks compatible with the watchdog (Feature #8)
- The phone client JS must remain ES5 compatible (no const/let/arrow functions) except for specific modern APIs (Wake Lock, Web Speech) — **unless** the Kestrel migration (Feature #15) extracts the client to separate files, at which point modern JS can be considered with a transpilation step
- Test each feature with the "borrowed laptop" scenario: no admin rights, unfamiliar Windows install, no internet
- `specs.json` must be reviewed and updated each release if resource requirements change
- Cloud translation backends (Feature #14) should gracefully degrade to NLLB when offline — the offline-first principle remains core to the product
- Bible text must always show copyright/attribution per the translation's license terms
- The Kestrel migration (Feature #15) is Phase 3 — build the first 10 features on the existing architecture, then migrate. After Kestrel, build all new endpoints on the new architecture
