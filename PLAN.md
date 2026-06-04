# EveryTongue — TODO (updated 2026-06-05)

> **Architecture shift:** EveryTongue is evolving from a single-session desktop transcription tool into a **headless multi-room translation server**. The desktop app still has operator workspaces (Live, Transcribe, Translate, Bible), but the primary user interface is now the **phone web client**. Anyone with a phone can create rooms, manage conversations, and receive translations — no operator required. The desktop just runs the server and auto-starts engines at launch.

## Plan Status Summary

### Kestrel Migration — COMPLETE
All 10 phases done. Kestrel in-process with DI, WebSocket hub, static files, TTS, Bible, audio streaming.

### Code Quality — COMPLETE
All 11 items done plus additional cleanup. TTS and Translation backend registries added for pluggable engine discovery. FormOptions is now the single source of truth for all settings.

### Rooms — GOVERNANCE COMPLETE + POLISHED (v1.7.0–v1.7.3)
Multi-room translation with lobby, PTT audio, text chat, virtual members, host controls, TTS, speaker colours, conference room targeting. All governance + polish complete. See [#19](#19-rooms--multi-room-translation) for remaining items.

### Conference Room Templates — COMPLETE (v1.7.4–v1.7.5)
Multi-pipeline conference templates with hosting codes, template manager UI, lobby hosting flow, room-scoped TTS.

### VAD Pipeline — IMPLEMENTED (v1.8.0–v1.8.1)
Frame-level Silero VAD with 4-tier commit system (soft/duration/hard/force), whisper-server inference serialization, stdout pipe deadlock fix. Stable across 45+ consecutive inferences. All old batch loop code removed (Phase 9 complete).

### Engine Genericization — COMPLETE (v1.7.5)
Pluggable STT/Translation/TTS backends via registries. NLLB 3.3B support. Translation backend hot-switching. All UI genericized to "STT" terminology.

## User-Reported Issues & Tasks
- [x] Implement stubs — most done (QR Code, Hardware Score, Diagnostics Export, File Integrity, Translate workspace). Remaining stubs: Session Wizard, Audio Level Monitor, Glossary Simple Mode, Event Profiles, Spec Sheet Generator, Portable Mode, Feedback prompt
- [x] Connected Clients dialog — popup form showing all connected phones with model, OS, browser, language, TTS, connection time
- [ ] Audio routing: NDI or Direct Audio output

## Suggested Next Priorities
1. Headless server / systray mode — see plan file `memoized-shimmying-elephant.md`
2. Audio Level Monitor (#3) — operator feedback, prevents bad audio
3. Setup Wizard expansion (#2) — integrates QR, audio monitor, hardware score
4. Priority Queue Pipeline — STT/Translation/TTS queues with dynamic priority scoring for multi-room load

## Immediate TODO (2026-06-05)
- [ ] Verify the code actually uses the user's selected engine (not hardcoded to a specific backend)
- [ ] Test whisper-cpp performance — should support more than 5 concurrent sessions
- [ ] Check YouTube downloading and transcription still works end-to-end
- [ ] Investigate which translation model is used in benchmarks (NLLB? MADLAD? specific size?)
- [ ] Continue headless server / systray implementation

---

## Robust VAD Pipeline — Silero VAD + Whisper.cpp Rewrite — IMPLEMENTED (v1.8.0–v1.8.1)

All 9 phases implemented and tested. Pipeline is stable across 45+ consecutive inferences with zero hangs or lost sentences.

**Key deviations from original plan:**
- **Frame size**: 512 samples (32ms) not 1536 (96ms) — Silero v5 only accepts 512 at 16kHz
- **DURATION-COMMIT** (v1.8.1): New 4th commit type — fires at 8s continuous speech without a 400ms pause, prevents bulk sentence dumps. Uses SOFT-COMMIT type internally.
- **Whisper-server pipe fix** (v1.8.1): stdout=DEVNULL to prevent Windows pipe buffer deadlock after ~14 inferences
- **Inference serialization** (v1.8.1): `_whisper_lock` prevents concurrent Vulkan GPU calls that caused deadlocks
- **Speech offset is immediate**: `is_speech` goes False immediately when prob < silence_threshold (state machine has time-based thresholds for commits, so no frame-count confirmation needed for offset)

### Goal
Replace the current batch-VAD capture loop in `live-server/server.py` (`_capture_loop_whisper_cpp`) with a robust frame-level VAD pipeline that eliminates clipped speech, duplicated text, unreliable endpointing, and transcript corruption during long-running sessions.

### Module Structure

The pipeline lives in a dedicated `vad/` package alongside `server.py`. Each file is small and single-purpose. `server.py` creates a `VadPipeline` instance, passes callbacks, and calls `start()`/`stop()`.

```
live-server/
  server.py                 — FastAPI app, endpoints, Whisper/SSE infra (slimmed)
  vad/
    __init__.py              — public imports (VadPipeline, VadConfig)
    buffers.py               — PrerollBuffer, UtteranceBuffer
    frame_vad.py             — FrameVAD (Silero wrapper + hysteresis)
    state_machine.py         — State enum, UtteranceStateMachine
    merger.py                — BoundaryMerger, split_sentences()
    segment.py               — TranscriptSegment dataclass, SessionStats extensions
    pipeline.py              — VadPipeline orchestrator (threads, queues, start/stop)
```

**Dependency direction**: `vad/` modules depend only on each other and stdlib/numpy/torch. They do NOT import from `server.py`. Instead, `server.py` passes callbacks to `VadPipeline`:
- `transcribe_fn(audio, language, beam_size, initial_prompt)` → Whisper inference
- `broadcast_fn(event_type, text, lang)` → SSE broadcast to clients
- `hallucination_fn(segments, last_text, lang, recent_langs)` → hallucination check
- `stats` object reference → for recording telemetry

This keeps the VAD pipeline testable in isolation and avoids circular imports.

### Design Principles
- Silero is the **sole authority** for speech detection and audio endpointing.
- Whisper is **only responsible** for transcription. It never influences when audio recording stops.
- **Two-tier silence thresholds**: a soft commit (400ms silence) delivers sentences quickly at natural pause points; a hard commit (750ms silence) marks definitive utterance boundaries. Both are purely VAD-driven.
- **Post-commit sentence splitting**: after Whisper transcribes a committed utterance, the text is split into sentences and each sentence is broadcast as a separate `commit` event. This gives per-sentence UI updates without Whisper influencing audio boundaries.
- A dedicated **boundary merger** handles overlap reconciliation, scoped to the actual audio overlap duration (not a fixed word window).
- **Never** use Whisper timestamps, punctuation, or transcript content to determine speech end.
- **Single-writer rule**: only one thread writes to the utterance buffer (the VAD thread), eliminating cross-thread race conditions.

### High-Level Pipeline

```
Microphone → sounddevice callback (96ms frames, 1536 samples)
    ↓
Pre-roll Ring Buffer (400ms, always filling)  ←── callback writes here always
    ↓
vad_queue (frame queue)  ←── callback enqueues every frame
    ↓
VAD Thread (sole owner of utterance buffer + state machine)
    ├── Frame-Level Silero VAD (per-frame speech probability with hysteresis)
    ├── Utterance State Machine (IDLE / SPEAKING)
    │     ├── IDLE→SPEAKING: snapshot pre-roll into utterance buffer
    │     ├── SPEAKING: append frame to utterance buffer
    │     └── SPEAKING→IDLE: commit utterance audio to transcribe_queue
    ↓
transcribe_queue
    ↓
Transcription Worker Thread
    ├── Whisper.cpp Vulkan Inference (finalized utterances only)
    ├── Segment Extraction (timestamped segments with metadata)
    ├── Boundary Merger (fuzzy tail-overlap dedup)
    └── SSE broadcast → UI
```

**Critical design choice**: The audio callback does NOT write to the utterance buffer. It only writes to the pre-roll ring buffer and enqueues frames to `vad_queue`. The VAD thread is the **sole writer** to the utterance buffer, eliminating the race condition where the callback and state machine compete over buffer ownership.

### Commit Types

*(Detailed phase-by-phase implementation specs removed. All 9 phases complete. See `live-server/vad/` source.)*

| Type | Trigger | Pre-roll overlap? | Merger runs? | Next state |
|------|---------|-------------------|-------------|------------|
| SOFT-COMMIT | 400ms silence, mid-speech | No (pre-roll is silence) | No | SPEAKING |
| DURATION-COMMIT | 8s continuous speech (no 400ms pause) | No (pre-roll is current speech) | No | SPEAKING |
| COMMIT | 750ms silence, definitive pause | No (pre-roll is silence) | No | IDLE |
| FORCE-COMMIT | 25s max duration hit | Yes (~400ms speech overlap) | Yes | SPEAKING |

**DURATION-COMMIT** (added v1.8.1) prevents bulk sentence dumps when a speaker talks continuously for 8+ seconds without pausing 400ms. Without it, a 12-second continuous utterance would commit all at once, sending 3-4 sentences in a burst. With it, the utterance is split at 8s into a SOFT-COMMIT, keeping sentence delivery steady. Uses the SOFT-COMMIT type internally (same behavior: stay SPEAKING, start fresh buffer with pre-roll).

**SOFT-COMMIT** is the key to low-latency sentence delivery. Speakers naturally pause 300–500ms between sentences. A 400ms soft threshold catches these pauses and commits the accumulated speech without waiting for the full 750ms hard threshold. The committed audio is sent to the transcription queue, and a fresh utterance buffer starts with a pre-roll snapshot. Since the pre-roll at a soft-commit point is silence (the speaker just paused for 400ms), there's no speech overlap — the boundary merger is not needed.

If silence continues past 750ms after a soft commit, the new buffer contains only silence. The state machine transitions to IDLE **without committing** the empty buffer (checked via `_has_speech_since_commit` flag). No wasted Whisper inference on silence.

**Example**: Speaker says "First sentence. [450ms pause] Second sentence. [800ms pause]"
1. 400ms after "First sentence.": soft commit → Whisper transcribes → `commit: "First sentence."` (~400ms + inference latency)
2. Speaker resumes "Second sentence." — new utterance buffer captures it
3. 800ms after "Second sentence.": hard commit → IDLE → Whisper transcribes → `commit: "Second sentence."`

Each sentence committed individually with ~400ms + inference latency, instead of both waiting for the 800ms hard silence at the end.

### Files to Create

| File | Contents |
|------|----------|
| `live-server/vad/__init__.py` | Public imports: `VadPipeline`, `VadConfig`. |
| `live-server/vad/buffers.py` | `PrerollBuffer` (ring buffer), `UtteranceBuffer` (growable buffer). |
| `live-server/vad/frame_vad.py` | `FrameVAD` (Silero wrapper with hysteresis, per-frame inference). |
| `live-server/vad/state_machine.py` | `State` enum, `UtteranceStateMachine` (IDLE/SPEAKING, commit logic). |
| `live-server/vad/merger.py` | `BoundaryMerger` (fuzzy dedup for force-commits), `split_sentences()`. |
| `live-server/vad/segment.py` | `TranscriptSegment` dataclass, `SessionStats` extensions. |
| `live-server/vad/pipeline.py` | `VadPipeline` orchestrator: creates queues/threads, starts/stops capture, holds `VadConfig`. Contains `_vad_thread()`, `_transcription_worker()`, `_interim_worker()`, `audio_callback()`. |

### Files to Modify

| File | Action |
|------|--------|
| `live-server/server.py` | Replace `_capture_loop_whisper_cpp()` call with `VadPipeline.start()`/`stop()`. Pass callbacks (`_transcribe_whisper_cpp`, `broadcast_event`, `_is_hallucination`). Update `/start` and `/config` endpoints with new VAD parameters. Fix `_transcribe_whisper_cpp()` to forward `initial_prompt`. Delete all faster-whisper code. |


.NET Faster-Whisper removal complete (commit `25b921d`).

### Files NOT to Modify

| File | Reason |
|------|--------|
| `Pipeline/LiveStreamRunner.vb` | Consumes SSE events — protocol unchanged. New params passed through existing `UpdateConfigAsync` / start body. |
| `Services/Rooms/ConversationAudioHandler.vb` | Uses `/transcribe` endpoint (one-shot), not the live capture loop. |
| `Server/KestrelHost.vb` | No changes needed. |
| `wwwroot/js/app.js` | Web client consumes `update`/`commit` SSE events — protocol unchanged. |
| `Models/AppConfig.vb` | Existing `LiveVadSilenceMs`, `LiveMaxSegmentSec` map to new params. New params use defaults server-side. |

### Verification — PASSED (v1.8.1)

Tested with "The Crow and the Pitcher" story read 3 times consecutively (23 utterances, 45 interims, 0 failures):

1. [x] **Build**: Embedded Python loads silero-vad, torch, sounddevice, numpy successfully.
2. [x] **Startup**: Pipeline auto-activates in whisper-cpp mode. Logs show `[PIPELINE] READY`.
3. [x] **Short utterance**: "The Crow." → commit after ~800ms silence, 1 commit event.
4. [x] **Soft commit (phrase pause)**: Sentence pairs separated by ~420ms pauses → soft-commit fires, stays SPEAKING.
5. [x] **Soft commit to hard commit**: After last sentence + long silence → soft-commit, then IDLE (silence-only discarded).
6. [x] **Multi-sentence continuous**: 2-sentence utterances → Whisper transcribes → sentence splitter emits 2 separate commits.
7. [x] **Duration-commit**: 8s continuous speech without 400ms pause → DURATION-COMMIT fires, splits into 2 sentences, stays SPEAKING.
8. [x] **Interim updates**: `update` events appear every ~1.5s during speech with provisional text.
9. [x] **Session stability**: 3 consecutive readings (~2 minutes) → no hangs, no buffer growth, no transcript corruption.
10. [x] **Whisper-server stability**: 45+ consecutive inferences with 0 timeouts (stdout=DEVNULL fix).
11. [x] **Inference serialization**: `_whisper_lock` prevents concurrent requests, all inferences complete in 0.1-0.4s.
12. [x] **Frame size**: sounddevice delivers exactly 512-sample frames, Silero v5 accepts them.
13. [x] **Hallucination**: Silence between readings → VAD stays IDLE, no phantom commits.

**Not yet tested:**
- Force-commit (25s continuous speech) — not triggered in test sessions
- Non-Latin scripts — needs multilingual testing
- Backpressure (queue full) — would require artificially slow Whisper

---

## Future Work (not scheduled)
- **Headless server / Windows Service mode** — run EveryTongue as a Windows service (no GUI, auto-start with OS). The desktop app becomes optional — the server hosts rooms, engines, and the web client independently. Remove/deprecate the WebView2 viewer panel (redundant now that rooms + phone web client handle everything). Operator controls (start/stop engines, view logs) move to a web-based admin dashboard served by Kestrel. Install/uninstall service via CLI or installer option.
- **Cross-platform (Linux / macOS)** — the headless server is the prerequisite for this. Once the WinForms dependency is removed:
  - **What's already cross-platform:** Kestrel (ASP.NET Core), all web client HTML/JS/CSS, Python sidecars (translation, MMS-TTS, live-server), Piper TTS, whisper.cpp. The entire phone experience (rooms, lobby, translation, TTS, Bible) has zero Windows dependency.
  - **What needs replacing:** WinForms UI → web-based admin dashboard (already planned for headless mode). WebView2 → not needed (phones are the primary UI). NAudio audio capture → platform-specific audio backends or USB audio passed to whisper.cpp directly. Firewall rules (`netsh`) → platform-aware or manual setup.
  - **Linux** — highest-value target. Cheaper to run a headless Ubuntu/Debian server than Windows. Churches in developing countries could use low-cost hardware. Raspberry Pi 5 (ARM64) is viable for single-stream STT with whisper.cpp.
  - **macOS** — Metal backend for whisper.cpp is fast (~1.2–1.5x vs CUDA, near real-time on M-series). Apple Silicon Macs are increasingly common in churches/organisations.
  - **Docker** — package headless server + all sidecars (translation, Piper, whisper.cpp) as a single Docker image. One `docker run` and it's serving rooms. GPU passthrough via `--gpus all` (NVIDIA) or `--device /dev/dri` (AMD/Intel). This is the ideal deployment for technical users and cloud hosting.
  - **Build approach:** Extract all server logic into a shared `EveryTongue.Core` library (no WinForms references). The Windows desktop app references Core + WinForms. A new `EveryTongue.Server` console app references Core only — this is the cross-platform headless entry point. Both share the same Kestrel pipeline, DI container, and engine orchestrators.
- Priority queue pipeline with dynamic priority scoring and backpressure/degradation
- Mesh WiFi / mDNS service discovery for automatic server finding
- Session recording & per-room transcript export
- Plugin auto-discovery from `plugins/` folder
- Plugin Manager UI with model management

---

# Agape Deployment Feature Plan

Implementation plan for making Every Tongue field-deployable by a non-technical international organisation. Features ordered by priority — each section covers what exists today, what needs to change, and how to build it.

---

## Table of Contents

| # | Feature | Status | Phase |
|---|---------|--------|-------|
| [1](#1-qr-code-connection) | QR Code Connection | **Done** | 1 |
| [2](#2-setup-wizard--event-setup-mode) | Setup Wizard — Event Setup Mode | Improve | 2 |
| [3](#3-audio-level-monitor) | Audio Level Monitor | New | 1 |
| [4](#4-diagnostic-bundle--remote-support) | Diagnostic Bundle / Remote Support | **Done** (a-d) | 1 |
| [5](#5-glossary-management--simplified-for-non-technical-users) | Glossary Management — Simplified | Improve | 2 |
| [6](#6-text-to-speech--server-side-engine) | Text-to-Speech — Server-Side Engine | Done | 4 |
| [7](#7-portable-usb-deployment) | Portable USB Deployment | New | 5 |
| [8](#8-crash-recovery--system-wide-resilience) | Crash Recovery — System-Wide | Improve | 2 |
| [9](#9-multi-language-operator-ui--expand-coverage) | Multi-Language Operator UI | **Done** (core + downloadable packs), Improve (add more languages) | 2 |
| [10](#10-session-recording--multi-format-export) | Session Recording & Export | Improve | 2 |
| [11A](#11a-field-feedback-system) | Field Feedback System | New | 4 |
| [11B](#11b-glossary-enrichment-pipeline) | Glossary Enrichment Pipeline | New | 4 |
| [11C](#11c-device-compatibility--suitability-scoring) | Device Compatibility & Suitability Scoring | New | 4 |
| [12](#12-hardware-readiness-score) | Hardware Readiness Score | **Done** | 1 |
| [13](#13-recommended-specifications-generator) | Recommended Specifications Generator | Moved to [Documentation](#documentation) | — |
| [14](#14-pluggable-translation-backends) | Pluggable Translation Backends (Cloud APIs) | Partial (a-e done) | 4 |
| [15](#15-server-infrastructure-upgrade--kestrel) | Server Infrastructure Upgrade (Kestrel) | **Done** | 3 |
| [16](#16-bible-integration) | Bible Integration | **Done** (a-g), partial (h) | 4 |
| [17](#17-text-chat-in-rooms) | Text Chat in Rooms | **Done** (in conversation rooms) | 2 |
| [18](#18-dictation-in-translate-workspace) | Dictation in Translate Workspace | New | 2 |
| [19](#19-rooms--multi-room-translation) | Rooms — Multi-Room Translation | **Governance Complete** (TTS remaining) | 1 |
| [20](#20-translation-load-testing-suite) | Translation Load Testing Suite | New | 2 |

**[Implementation Order](#implementation-order)** | **[Development Notes](#development-notes)** | **[Notes](#notes)**

---

## 1. QR Code Connection

**Status:** Done. FormQrCode with QRCoder, accessible from session wizard and menu. Shows QR + URL text fallback.

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
- `locales/*.json` — wizard step labels

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
- `locales/*.json` — warning messages

**Complexity:** Medium. The audio math is trivial; the UI polling and visual meter are the main work.

---

## 4. Diagnostic Bundle / Remote Support

**Status:** Done (a-d). System info collector (HardwareScanner), log bundling, export diagnostics ZIP, file integrity checksums with build-time manifest and runtime verification. Health beacon (e) is future/opt-in.

---

## 5. Glossary Management — Simplified for Non-Technical Users

**Status:** Fully implemented. `glossary.json` with trigger/source-lang/fixes model. `FormFilterEditor.vb` provides editing UI. Server-side application in `translate-server/server.py` with hot-reload.

**What exists:**
- 21 entries, mostly Catalan theological terms
- Trigger-based matching (substring in source text)
- Per-target-language wrong→right word replacements
- Desktop editing UI with grids
- `/glossary/reload` endpoint for hot-reload

**Pending: Catalan glossary expansion** — a previous Claude session performed a translation quality assessment and identified a batch of Catalan theological/church terms that NLLB mistranslates. These need to be added to `glossary.json`. Run the assessment again or check conversation history to retrieve the specific terms.

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
- `translate-server/glossary.json` — no structural changes needed
- New file: `glossary-packs/christian-theological.json`
- `locales/*.json` — new UI labels

**Complexity:** Medium. The import/export is straightforward; the simplified UI is the main design challenge.

---

## 6. Text-to-Speech — Server-Side Engine

**Status:** Done. TtsOrchestrator with 3 backends (Piper, MMS-TTS, EdgeTTS), TtsCache ring-buffer, fire-and-forget pipeline, hybrid browser/server approach, Bible verse TTS, local NAudio output. Not done: Piper voice model download UI (b), earphone mode (d).

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
- Double-click `EveryTongue.exe` from USB
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
- `locales/*.json` — status messages, reconnection labels

**Complexity:** Medium-High. Each component needs its own recovery strategy, and they interact (e.g., restarting the live server should not lose the subtitle server's client connections).

---

## 9. Multi-Language Operator UI — Expand Coverage

**Status:** Core localization complete (v1.6.0). JSON-based `LanguagePackService` with 566+ locale keys. Every user-facing string uses locale lookups. Language packs are downloadable on demand from GitHub CDN via Download Manager — the number of supported languages is unlimited and new ones can be added at any time.

**What exists:**
- `locales/*.json` — JSON locale files, one per language (en.json ships embedded; others downloaded on demand)
- `LanguagePackService` singleton loads JSON at startup, with embedded en.json fallback
- All forms, controllers, shell, and Program localized via `GetString("key")`
- Language packs downloadable from GitHub CDN via Download Manager
- No hardcoded list of languages — locales are auto-discovered dynamically
- Debug/log output intentionally remains in English

**What to improve:**

### a) Add More Language Packs
Create new `locales/{code}.json` files and publish to GitHub. Priority languages for Agape's European footprint: Italian, Polish, Romanian, Dutch, Hungarian, Czech, Greek, Ukrainian, Russian, Croatian. Any volunteer can create a new language pack by translating the keys in `en.json`.

### b) Translation Workflow
- Create a master spreadsheet or tool for managing translations across all locale files
- CI step to validate all keys are present in all locale files
- Agape volunteers contribute translations; packs published to GitHub for download

### c) RTL Support (Future)
- Not critical for European deployment, but Arabic (`ar`) would need RTL layout
- Flag for future if Agape's Middle East/North Africa work needs it

**Files to modify:**
- New files: `locales/{code}.json` for each new language
- `locales/*.json` — any new string keys from features must be added to ALL existing locale files

**Complexity:** Low per language (mostly translation work, not code).

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
- `locales/*.json` — export option labels

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
- `locales/*.json` — feedback UI labels

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
- `locales/*.json` — review workflow labels

**Dependencies:** Feature #11A (needs suggestion data to review). Feature #5 (glossary packs — extends the existing glossary editing UI).

**Complexity:** Medium-High. The review UI in FormFilterEditor is the main design work — it needs to be intuitive enough that a non-developer language manager can use it confidently.

---

## 11C. Device Compatibility & Suitability Scoring

**Status:** Not started. Can be developed independently of Features #11A and #11B.

**Problem:** Every phone that connects is a potential data point about what works and what doesn't. Currently there's no way to know if a user's device supports all features, and no guidance when features are unavailable. This feature captures device capabilities automatically and uses the accumulated data to warn future users proactively.

**Scope:** Device fingerprinting, passive device logging, curated compatibility database, connection-time warnings, feature capability indicators, TTS voice audit, device suitability score. Primarily phone-side JS with server-side logging.

**Implementation:**

### Pre-requisite: Connected Clients Dialog

Before the full scoring system, build a simple popup form (`FormConnectedClients`) that the operator can open from the menu or status bar to see who's connected right now.

**Data per client row:**
- Device model (parsed from User-Agent, e.g. "Samsung Galaxy A54", "iPhone 13")
- OS + version (e.g. "Android 14", "iOS 17.4")
- Browser + version (e.g. "Chrome 125", "Safari 17.4")
- Selected language (the translation language they're receiving)
- TTS status (has voices for their language: yes/no/server-only)
- Connection time (how long connected, e.g. "12m ago")
- Screen size

**Implementation:**
- Phone client sends a `deviceInfo` WebSocket message on connect (User-Agent, platform, screen, capabilities, selected language)
- `SubtitleHub` / `SubtitleService` stores client info alongside existing connection tracking
- `FormConnectedClients.vb` — DataGridView popup, refreshes on a timer (every 5s), sortable columns
- Accessible from: View menu → "Connected Clients" or click the client count in the status bar
- Summary row at bottom: "12 clients — 4 languages — 3 Android, 8 iOS, 1 other"

**Server stats panel (top of dialog):**
- CPU usage (process %) and GPU usage (nvidia-smi, if available)
- Network throughput (bytes out/sec, current bandwidth)
- Memory usage
- Transcription/translation lag
- Pulled from existing `MetricsService` — same data as the status bar, just displayed in the dialog header for context when reviewing client load

**Files to modify:**
- `wwwroot/js/app.js` — send `deviceInfo` message on WebSocket connect
- `Server/Hubs/SubtitleHub.vb` — handle `deviceInfo` message, store in client registry
- `Services/Subtitle/SubtitleService.vb` — `ClientInfo` class with device fields, expose connected client list
- `Services/Infrastructure/MetricsService.vb` — expose current metrics for the dialog
- New file: `Forms/FormConnectedClients.vb` + `.Designer.vb` — popup dialog with server stats panel + client DataGridView
- `Forms/FormMain.Shell.vb` — menu item and status bar click handler

**Complexity:** Low-Medium. User-Agent parsing is the fiddly part; server stats reuse MetricsService; the rest is straightforward WinForms.

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
- `locales/*.json` — compatibility warning messages, TTS guidance text

**Dependencies:** None. All client-side JS detection + server-side JSON file I/O.

**Complexity:** High. The most complex part is the TTS voice audit and platform-specific guidance system. However, each subsection (fingerprinting, compatibility check, TTS audit, suitability score) is independently buildable and testable.

---


## 12. Hardware Readiness Score

**Status:** Done. HardwareScanner detects GPU/CPU/RAM/disk/OS via WMI + nvidia-smi. Weighted scoring (GPU 40%, CPU 25%, RAM 20%, Disk 10%, OS 5%) with green/amber/red traffic light. Options → Hardware panel shows score, component breakdown, and recommendations. Auto-scans on first visit. Smart defaults based on score.

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
- All stored in `locales/*.json` alongside other UI strings

For languages not yet in the UI (e.g., before Feature #9 adds Polish/Romanian), generate in English with a note: "This document is not yet available in your language."

**Files to modify:**
- New file: `specs.json` — tier definitions, updated per release
- New file: `SpecSheetGenerator.vb` — reads `specs.json` + locale, generates HTML output
- `FormMain.vb` — "Generate Spec Sheet" button, integration with hardware score panel
- `HardwareScanner.vb` (Feature #12) — tier classification based on `specs.json` thresholds
- `locales/*.json` — spec sheet text, tier descriptions, buying tips
- CI/release pipeline — checklist/reminder to update `specs.json`

**Dependencies:** None. HTML generation using built-in .NET string/file operations.

**Complexity:** Low-Medium. The spec sheet generation is straightforward HTML templating. The main ongoing cost is remembering to update `specs.json` each release — the CI checklist helps with this.

---

## 14. Pluggable Translation Backends (Cloud APIs)

**Status:** Partially implemented.

**What's done:**
- **(a) Backend Abstraction** — `ITranslationBackend` interface with `TranslateAsync`, `GetSupportedLanguagesAsync`, `CheckHealthAsync`, `IsAvailable`, `RequiresInternet`, `Name`. `TranslationOrchestrator` implements `ITranslationService` with fallback chain and per-language backend overrides.
- **(b) Cloud Backend Implementations** — `DeepLBackend`, `GoogleBackend`, `AzureBackend` all coded in `Services/Translation/CloudTranslationBackend.vb`. Each has `Configure(apiKey)` method. Return `IsAvailable=False` until API keys set.
- `SidecarTranslationBackend` wraps existing `TranslationService` as an `ITranslationBackend`
- All backends registered in Kestrel DI container
- **(c) Configuration UI** — `TranslationBackendRegistry` with engine selector combo in Options → Translation panel. `AppConfig.TranslationBackend` stores selection. Options dialog auto-populates from registry.
- **(d) Language Mapping** — `language-codes.json` (161 languages) with `LanguageCodeService` singleton providing cross-format conversion (FLORES ↔ ISO 639-1 ↔ ISO 639-3 ↔ DeepL ↔ Google ↔ Azure ↔ Whisper). Replaces all hardcoded dictionaries in `TranslationService` and `BibleService`.
- **(e) Hybrid Mode** — `TranslationOrchestrator.LanguageOverrides` dictionary supports per-language backend selection with local sidecar as always-available fallback

**What's NOT done:**
- **(f) Cost Awareness** — No usage tracking or budget limits
- **(g) Latency Considerations** — No batching or latency indicators
- **(h) Glossary Integration** — Not wired to cloud backends
- SidecarTranslationBackend not yet registered in DI (needs legacy TranslationService instance from FormMain)

**Original problem description (kept for context):**

Not started. Translation is currently NLLB-200 only, running locally via the Python sidecar. No option to use cloud translation services.

**Problem:** NLLB-200 is excellent for offline use, but cloud translation APIs (Google Translate, DeepL, Azure Translator) generally produce higher-quality translations, especially for less-resourced languages. When internet is available and accuracy matters more than offline capability, users should be able to choose a cloud backend. This also opens the door for organisations that already have API keys/contracts with translation providers.

**Implementation:**

### a) Translation Backend Abstraction

Refactor the translation pipeline to support pluggable backends:

```
ITranslationBackend
  ├── SidecarTranslationBackend  (existing — local Python sidecar)
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

- **Backend selector:** dropdown — "Local (NLLB/MADLAD)" / "DeepL" / "Google Translate" / "Azure Translator" / "Custom API"
- **API key field:** text input, shown only for cloud backends, stored encrypted in AppConfig
- **Test button:** sends a sample translation request, shows result and latency
- **Fallback toggle:** "Fall back to offline translation if cloud API is unreachable" — enabled by default
- **Status indicator:** shows current backend health (connected/disconnected/rate-limited)

### d) Language Mapping

Each backend uses different language codes:
- FLORES: `fra_Latn`, `deu_Latn`, `pol_Latn`
- DeepL: `FR`, `DE`, `PL`
- Google/Azure: `fr`, `de`, `pl`

Create a language mapping table (`language-codes.json`) that maps between all formats:

```json
{
  "french": {
    "flores": "fra_Latn",
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
- Fallback for unsupported languages: local sidecar (covers ~200 languages)
- Example: DeepL handles French/German/Spanish; local sidecar handles Catalan/Albanian/Georgian
- Configuration: per-language backend override (advanced setting)

### f) Cost Awareness

Cloud APIs charge per character. Show usage tracking:
- Characters translated this session / this month
- Estimated cost based on known pricing (DeepL: €5/1M chars, Google: $20/1M chars)
- Warning when approaching free tier limits
- Option to set a monthly character budget — switch to offline translation when exceeded

### g) Latency Considerations

Cloud APIs add network latency (100-500ms per request) vs the local sidecar's processing (~50ms):
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
- New file: `SidecarTranslationBackend.vb` — wraps existing TranslationService.vb logic
- New file: `DeepLBackend.vb` — DeepL API client
- New file: `GoogleTranslateBackend.vb` — Google API client
- New file: `AzureTranslatorBackend.vb` — Azure API client
- New file: `CustomApiBackend.vb` — user-defined endpoint client
- New file: `language-codes.json` — cross-backend language code mapping
- `TranslationService.vb` — refactor to orchestrator role, backend selection logic, fallback chain
- `FormMain.vb` — backend selector UI, API key config, test button, usage display
- `AppConfig.vb` — backend selection, encrypted API keys, usage counters, per-language overrides
- `SubtitleServer.vb` — no changes needed (receives translated text from TranslationService regardless of backend)
- `locales/*.json` — backend names, config labels, usage warnings

**Dependencies:** `System.Net.Http` (built into .NET 8). No external packages — all cloud APIs are simple REST endpoints.

**Complexity:** Medium-High. The abstraction layer and individual backend clients are straightforward. The complexity is in: fallback logic, language code mapping, hybrid mode routing, and cost tracking. Each backend should be implemented incrementally — start with DeepL (best quality for European languages), then add others.

---


## 15. Server Infrastructure Upgrade — Kestrel

**Status:** Done. Kestrel replaces legacy SubtitleServer. KestrelHost.vb with DI, EndpointRegistration.vb with Minimal API, SubtitleHub.vb WebSocket hub, extracted web client (wwwroot/), response compression (Brotli + Gzip), HTTP/2, MetricsService, CertificateService. All sub-features (a-h) complete.

---

## 16. Bible Integration

**Status:** Done (a-g). BibleService with SQLite, Bible API endpoints, reference parser, phone client Bible panel (browse/search/quick-ref), WebSocket Bible reference detection, eBible.org download + USFM→SQLite conversion, copyright display. Not done: (h) licensing agreements for modern copyrighted translations.

---

## 17. Text Chat in Rooms

**Status:** Done. Type-to-translate in conversation rooms, same translation pipeline as PTT audio. WebSocket chatMessage type, server-side translation + broadcast, mixed audio/text conversation.

---

## 18. Dictation in Translate Workspace

**Status:** New

**Problem:** The desktop Translate workspace currently only accepts typed text input. Users who want to speak instead of type — whether for speed, accessibility, or because they're translating spoken content — have no option. Adding a dictation (speech-to-text) button would let users speak into their microphone and have the text automatically transcribed and placed into the source text box, ready for translation.

**Implementation:**

#### a) Microphone capture and STT
- Add a microphone/dictation toggle button next to the source text input in the Translate workspace
- Use NAudio to capture audio from the default input device
- Send audio to the local Whisper live-server `/transcribe` endpoint (same sidecar used by Live and Rooms)
- Auto-detect or use the selected source language for Whisper's `lang` parameter
- Append transcribed text to the source text box

#### b) UI flow
- Button toggles between recording and idle states (mic icon with visual indicator)
- While recording, show a pulsing indicator so the user knows dictation is active
- On stop, the transcribed text appears in the source field
- User can edit the transcribed text before clicking Translate
- Support both push-to-talk (hold button) and toggle (click to start/stop) modes

#### c) Engine readiness
- If Whisper live-server is not running, auto-start it (same as conversation rooms do)
- If Whisper dependencies are missing, prompt via `AppLogger.PromptDownloadManager`

**Files to modify:**
- `Controllers/TranslateController.vb` — add dictation logic, mic capture, STT calls
- `Forms/FormMain.Designer.vb` — add dictation button to Translate workspace UI
- `locales/*.json` — add dictation button label/tooltip strings

---


## 19. Rooms — Multi-Room Translation

**Status:** Room Governance complete. All core infrastructure working. See Plan Status Summary for full feature list.

### Remaining

#### g) Desktop server dashboard
- Simple overview on the desktop app: active rooms, total clients, resource usage
- Conference room dropdown on Live workspace (server-side `TargetRoomId` ready, ComboBox UI not yet added)
- No room creation/management from desktop — that's the phone's job

### Future: Priority Queue Pipeline

When multiple rooms run concurrently, STT/translation/TTS become bottlenecks. Each gets a priority queue with dynamic scoring (Conversation > Conference, fast turn-taking boosted, starvation prevention). Backpressure: switch to smaller model, increase batching, skip TTS, show "high demand" indicator.

### Future: Plugin Architecture

Auto-discovery from `plugins/` folder. Plugin Manager UI with model management. Benchmark suite for standardised engine testing. Existing registries (ISttBackend, ITranslationBackend, ITtsBackend) are the foundation.

---

## Implementation Order

Phase 1 (Quick Wins): #1 QR ✅, #12 Hardware ✅, #13 Spec Sheet, #3 Audio Monitor, #4 Diagnostics ✅
Phase 2 (Operator Experience): #2 Setup Wizard, #8 Crash Recovery, #5 Glossary, #9 Localization, #10 Session Export
Phase 3 (Kestrel): ✅ DONE
Phase 4 (Kestrel-dependent): #11C Device Compat, #11A Feedback, #11B Glossary Enrichment, #14 Cloud Translation, #6 TTS ✅, #16 Bible ✅
Phase 5 (Portability): #7 Portable USB

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

## 20. Translation Load Testing Suite

**Status:** New

**Goal:** A built-in testing tool that fires static translation tasks at the translation server under controlled load, measuring throughput, latency, and output quality. Answers: "How fast is my translation server?", "Does output degrade under load?", "Is my hardware keeping up?"

### What exists today

- Translation server exposes `/translate` endpoint (single request, blocking)
- `TranslationService.TranslateAsync()` handles one source→N target calls sequentially
- No way to stress-test without a live conference session
- No output quality validation — operator has to eyeball translations

### What to build

#### a) Test corpus
- Ship a static test corpus in `test-data/translation-corpus.json` — curated sentence pairs with known-good reference translations
- Organised by domain: general, religious/sermon, medical, legal — so operators can test with realistic content
- Each entry: `{ source, sourceLang, targets: [{ lang, reference }] }` — source text + expected translations for comparison
- Start with 50-100 sentences covering the most common language pairs (ca→en, ca→es, ca→fr, ca→de, en→es, en→fr)

#### b) Load test runner
- New workspace or dialog: **Tools → Translation Benchmark**
- Parameters: corpus selection, concurrency level (1/2/5/10 simultaneous requests), number of iterations, target languages
- Fires requests directly at the translation server's `/translate` endpoint (or via `TranslationService`)
- Measures per-request: latency (ms), tokens in/out, source→target pair
- Measures aggregate: requests/sec, p50/p95/p99 latency, total throughput (tokens/sec), error rate
- Progress bar with live stats during the run

#### c) Output quality scoring
- Compare translation output against reference translations using simple similarity metrics (character-level edit distance, token overlap, or BLEU-like n-gram scoring)
- Flag translations that diverge significantly from references — helps catch model regressions or bad language pairs
- Quality score per language pair: "en→de: 87% match, en→ja: 62% match"
- Not a full MT evaluation suite — just enough to spot obvious problems

#### d) Results report
- Summary table: language pair, avg latency, throughput, quality score, error count
- Exportable as JSON or CSV for tracking across versions/hardware
- Compare against previous runs: "v1.7.5 on RTX 3060: 45 req/s" vs "v1.7.5 on CPU: 3 req/s"
- Store last result in config so it shows on next open without re-running

#### e) Regression detection
- After a model update or config change, operator re-runs the benchmark
- Compare output quality scores against the stored baseline
- Highlight any language pairs where quality dropped significantly
- "Warning: de→fr quality dropped from 85% to 61% after model change"

### Implementation notes

- The test runner should work whether or not a live session is active — it talks directly to the translation server
- Concurrency testing reveals whether the server handles parallel requests (important for multi-room scenarios)
- Keep the corpus small enough to ship with the app (~50KB JSON) but representative enough to be useful
- The corpus can also serve as a smoke test after translation model downloads — "run quick test" to verify the model works
- Consider adding a "Quick Test" (5 sentences, 1 thread) vs "Full Benchmark" (full corpus, configurable load) mode

### Files

| File | Purpose |
|------|---------|
| `test-data/translation-corpus.json` | NEW — static test sentences with reference translations |
| `Forms/FormTranslationBenchmark.vb` | NEW — benchmark dialog UI |
| `Forms/FormTranslationBenchmark.Designer.vb` | NEW — benchmark dialog layout |
| `Services/Testing/TranslationBenchmarkRunner.vb` | NEW — load test engine, metrics collection, quality scoring |
| `Forms/FormOptions.vb` or menu | Add "Translation Benchmark" menu item under Tools |

---

## Development Notes

**Solo developer context:** This plan is maintained by one person (Jeremy) and developed incrementally alongside a live deployment serving a 300-person church congregation. Every Sunday is a live test with real users who don't speak the same language as the speaker.

**Architecture direction (2026-05-31):** The desktop app is becoming a headless translation server. The phone web client is the primary user interface — anyone with a phone can create rooms, manage conversations, and receive translations without an operator. Desktop workspaces (Live, Transcribe, Translate, Bible) remain for direct operator use, but the growth direction is server-side: multiple concurrent rooms, auto-starting engines, phone-first room management. The Rooms architecture (Feature #19) is now the highest-priority work.

**Testing strategy:** The congregation is the test bed. Features ship when they work locally, then get real-world validation every week. Ship small, ship often. Visible features first.

**Feature #11 split rationale:** The original Feature #11 was split into #11A (Field Feedback), #11B (Glossary Enrichment), and #11C (Device Compatibility) — three distinct systems with different dependencies and timelines.

---

## Documentation

The following developer/user documentation needs to be written. These may live in the repo (e.g. `docs/`) or be generated into the app's Help system.

| Topic | Audience | Status | Notes |
|---|---|---|---|
| How to add a TTS engine | Developer | Partial (in CLAUDE.md) | Implement `ITtsBackend`, register in DI, add to `TtsBackendRegistry`. Options dialog auto-populates. |
| How to add a translation engine | Developer | Partial (in CLAUDE.md) | Implement `ITranslationBackend`, register in DI, add to `TranslationBackendRegistry`. Options dialog auto-populates. |
| Accepted Bible formats | User/Developer | Not started | SQLite databases with `info`, `books`, `verses` tables (MyBible schema). Auto-scanned from `Bibles/` directory. Can be downloaded via Download Manager (eBible.org USFM → SQLite conversion) or manually placed. Copyright metadata in `info` table displayed automatically. |
| Recommended specifications | User | Not started | Localised spec sheet generator (#13). Three tiers (Minimum/Recommended/Optimal) based on usage profile. Generated from `specs.json`, printable HTML output. Answers "what laptop should I buy?" |

---

## Notes

- Each feature should include its own i18n strings added to ALL existing locale files
- All new Python endpoints should include health checks compatible with the watchdog (Feature #8)
- The phone client JS must remain ES5 compatible (no const/let/arrow functions) except for specific modern APIs (Wake Lock, Web Speech) — **unless** the Kestrel migration (Feature #15) extracts the client to separate files, at which point modern JS can be considered with a transpilation step
- Test each feature with the "borrowed laptop" scenario: no admin rights, unfamiliar Windows install, no internet
- `specs.json` must be reviewed and updated each release if resource requirements change
- Cloud translation backends (Feature #14) should gracefully degrade to offline translation when offline — the offline-first principle remains core to the product
- Bible text must always show copyright/attribution per the translation's license terms
- Kestrel migration is complete — all new endpoints built on Kestrel
- Rooms (#19) is the highest-priority active work — engines auto-start at launch, phone clients manage rooms independently
- Full Rooms architecture (data model, priority queue, plugin architecture) consolidated into Feature #19
# Every Tongue — UI Redesign Plan

## Design Principles

- **Universal**: Icon-driven navigation, minimal text in chrome, full i18n support
- **Feature-focused**: Each major feature gets its own clear workspace
- **Progressive disclosure**: Simple by default, advanced settings accessible but not in the way
- **Wizard-guided**: Complex workflows (like live sessions) walk the user through setup
- **Offline-first**: Every UI element works without internet; cloud features degrade gracefully

---

## First-Run Experience

On first launch (no saved config), the app shows a **full-screen language picker**
before anything else. This sets the UI language for the entire application.

```
┌─ Every Tongue ───────────────────────────────────────────────────┐
│                                                                   │
│                      [Every Tongue logo]                          │
│                                                                   │
│                  Welcome / Bienvenido / Bienvenue                 │
│                                                                   │
│              Choose your language / Elige tu idioma               │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                                                             │  │
│  │   🇬🇧 English    🇪🇸 Español    🇫🇷 Français    🇩🇪 Deutsch   │  │
│  │                                                             │  │
│  │   🇵🇹 Português  🇯🇵 日本語      🇨🇳 中文        🇰🇷 한국어     │  │
│  │                                                             │  │
│  │   🇸🇦 العربية     🇮🇳 हिन्दी      🇮🇩 Indonesia   🇷🇺 Русский   │  │
│  │                                                             │  │
│  │   🇮🇹 Italiano   🇳🇱 Nederlands  🇵🇱 Polski     🇷🇴 Română    │  │
│  │                                                             │  │
│  │   🇹🇷 Türkçe     🇹🇭 ไทย        🇻🇳 Tiếng Việt  🇺🇦 Українська│  │
│  │                                                             │  │
│  │                    [ More languages... ]                     │  │
│  │                                                             │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  🔍 [Search / Buscar / Rechercher...                         ]    │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

**Design:**
- Large flag + native language name tiles in a responsive grid
- Most common/supported languages shown first (configurable order)
- Search box at the bottom — filters as you type, matches native name and English name
- "More languages..." expands to show all supported languages (auto-translated UI)
- Selecting a language immediately applies it and proceeds to the main window
- The choice is saved in config; can be changed later via View > Language or Options > General
- If the selected language has no locale JSON yet, the editor auto-generates one via local translation on the spot

**Implementation:** `FormLanguagePicker.vb` — shown once on first run, before FormMain loads.

---

## Window Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  Menu Bar                                                       │
│  File | Tools | Session | View | Help                           │
├─────────────────────────────────────────────────────────────────┤
│  Toolbar (icon + short label, icon-only when narrow)            │
│  [Live] [Transcribe] [Translate] [Bible] | [QR] [⚙ Options]   │
├────────┬────────────────────────────────────────────────────────┤
│        │                                                        │
│  Nav   │   Workspace (content changes per feature)              │
│  Rail  │                                                        │
│        │                                                        │
│  ┌──┐  │                                                        │
│  │▶ │  │                                                        │
│  └──┘  │                                                        │
│  Live  │                                                        │
│        │                                                        │
│  ┌──┐  │                                                        │
│  │♫ │  │                                                        │
│  └──┘  │                                                        │
│ Trans- │                                                        │
│ cribe  │                                                        │
│        │                                                        │
│  ┌──┐  │                                                        │
│  │Aa│  │                                                        │
│  └──┘  │                                                        │
│ Trans- │                                                        │
│  late  │                                                        │
│        │                                                        │
│  ┌──┐  │                                                        │
│  │B │  │                                                        │
│  └──┘  │                                                        │
│ Bible  │                                                        │
│        │                                                        │
├────────┴──────────────────────────────────────────────────┬─────┤
│  Status Bar                                               │     │
│  [● Server :5081] [Clients: 3] [GPU: CUDA] [Live 12:34]  │ [▲] │
├───────────────────────────────────────────────────────────┴─────┤
│  Log Panel (collapsible — toggle with ▲ button or View menu)    │
│  [All] [Server] [Live] [Job]                                    │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ [20:47:52] [BibleService] loaded KJV+ (en)                 ││
│  │ [20:47:52] Kestrel started on HTTP:5080 HTTPS:5081         ││
│  │ [20:47:55] Client connected: 10.0.0.42 — Android/Chrome    ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Components

1. **Menu Bar** — Standard Windows menu for discoverability and keyboard access
2. **Toolbar** — Quick-access buttons matching the nav rail features + QR code + Options (⚙) cog
3. **Nav Rail** — Narrow left strip with large icons + short labels; always visible (4 workspaces)
4. **Workspace** — The main content area; swaps based on selected feature
5. **Options Dialog** — Modal VS-style settings dialog with tree categories (Tools > Options or toolbar ⚙)
5. **Status Bar** — Persistent: server status, client count, GPU, hardware score traffic light, live timer
6. **Log Panel** — Collapsible drawer at bottom, unified log with source filter tabs

---

## Menu Bar Structure

```
File
  New Session...              (opens Live Session Wizard — Feature #2)
  ─────────────
  Export Diagnostics...       (Feature #4 — ZIP bundle for remote support)
  ─────────────
  Exit

Tools
  Transcribe File/URL...      (switches to Transcribe workspace)
  Translate Text...           (switches to Translate workspace)
  Bible Lookup...             (switches to Bible workspace)
  ─────────────
  Glossary Editor...          (opens FormFilterEditor — simple/advanced modes)
  Import Glossary Pack...     (Feature #5 — load pre-built glossary)
  Export Glossary...
  ─────────────
  Localization Editor...      (edit/create UI translations — see Localization System)
  ─────────────
  Download Manager            (Feature #5/7 — models, Bibles, TTS voices)
  Check Dependencies          (Python venvs, translation model, whisper-server)
  Verify Paths
  Verify File Integrity       (Feature #4d — checksums)
  ─────────────
  Options...                  (opens Settings dialog — see below)

Session
  Start Live                  (quick-start with current settings from Options)
  Stop Live
  ─────────────
  Show QR Code                (Feature #1 — floating QR window)
  Copy Phone URL

View
  Show Log Panel              (toggle log drawer at bottom)
  ─────────────
  Theme > System | Light | Dark
  Language > (UI languages — 18+ locales per Feature #9)
  ─────────────
  Full Screen

Help
  Quick Start Guide
  Keyboard Shortcuts
  ─────────────
  Hardware Report             (Feature #12 — score + recommendations)
  Generate Spec Sheet...      (Feature #13 — localised PDF/HTML)
  ─────────────
  Check for Updates
  About Every Tongue
```

---

## Feature Workspaces

### 1. Live Translation (default/home)

The primary feature. Clean, focused, minimal controls visible by default.

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  ┌─ Audio ──────────────────────────────────────────────────┐   │
│  │ Device: [Microphone (Realtek) ▼] [Refresh]               │   │
│  │ Language: [English ▼]   [Filters...]                      │   │
│  │                                                           │   │
│  │ Level: ████████████░░░░░░░░░░░░░  (green — good)         │   │
│  │        "Speech detected" indicator                        │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [ ▶ Start Session ]  [ ■ Stop ]  [ Save... ]  [ QR Code ]     │
│                                                                 │
│  ┌─ Live Output ────────────────────────────────────────────┐   │
│  │                                                           │   │
│  │  (WebView2 — same client view as phones see)              │   │
│  │                                                           │   │
│  │                                                           │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─ Tuning ─────────────────────────────────────────────────┐   │
│  │ Max Segment: ──●────── 15s    VAD Silence: ────●─── 800ms│   │
│  │ [Tune]                                                    │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Key elements from Feature Plan:**
- **Audio Level Monitor** (Feature #3) — real-time RMS meter with color coding (grey/green/yellow/red), VAD speech indicator, warning labels ("Move microphone closer" / "Too loud")
- **QR Code button** (Feature #1) — opens floating QR window with phone URL
- **Save Transcript** (Feature #10) — multi-format export (TXT, CSV, HTML, SRT per language)
- **"Start Session" behavior:** If essential settings are missing, opens the Session Wizard automatically

### 2. Transcribe (File/YouTube processing)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Mode: ( ) Audio File  ( ) YouTube  ( ) Video File              │
│                                                                 │
│  ┌─ Input ──────────────────────────────────────────────────┐   │
│  │ Source: [                                    ] [Browse]    │   │
│  │ Language: [Auto ▼]    Model: [large-v3 ▼]                 │   │
│  │ Time range: [00:00:00] to [00:00:00] (optional)           │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─ Output ─────────────────────────────────────────────────┐   │
│  │ Folder: [                                    ] [Browse]    │   │
│  │ Translate to: [None ▼]                                     │   │
│  │ Formats: [x]SRT [ ]VTT [x]TXT [ ]JSON [ ]CSV [ ]LRC      │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [ ▶ Start ]  [ Resume ]  [ Cancel ]                            │
│                                                                 │
│  ┌─ Progress ───────────────────────────────────────────────┐   │
│  │ Step: Transcribing chunk 3/12...                          │   │
│  │ ████████████░░░░░░░░░░░░░░░░░░░  38%                      │   │
│  │ ████████░░░░░░░░░░░░░░░░░░░░░░░  25%  (chunk)             │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [Open Output] [Subtitle Edit] [Preview SRT]                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3. Translate (text-to-text — future, Feature #14 enabler)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  From: [Auto Detect ▼]              To: [Spanish ▼]             │
│  Engine: [Local (NLLB/MADLAD) ▼]  Status: Model loaded           │
│                                                                 │
│  ┌──────────────────────┐    ┌──────────────────────────────┐   │
│  │                      │    │                              │   │
│  │  (source text)       │ => │  (translated text)           │   │
│  │                      │    │                              │   │
│  │                      │    │                              │   │
│  └──────────────────────┘    └──────────────────────────────┘   │
│                                                                 │
│  [ Translate ]  [ Clear ]  [ Copy ]  [ Swap ]                   │
│                                                                 │
│  Backend: Local | DeepL | Google | Azure | Custom                │
│  (per Feature #14 — pluggable backends with fallback)           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4. Bible

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Translation: [KJV+ ▼]    Ref: [John 3:16      ] [Go]          │
│                            Search: [            ] [Search]      │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │  (Book grid / chapter grid / verse display)              │   │
│  │                                                          │   │
│  │  Same navigation as web client but native WinForms       │   │
│  │  — or WebView2 reusing the existing web Bible UI         │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [Download Translations...] — (Feature #16f — Bible bundles)    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Options Dialog (Visual Studio–style)

Opened by: **Tools > Options**, **toolbar ⚙ button**, or **Ctrl+,**

A modal dialog with a **tree of collapsible category headings** on the left and
the corresponding settings panel on the right. Categories expand/collapse with
disclosure triangles. All current settings from the scattered tabs are
consolidated here. New sessions inherit these values as defaults.

```
┌─ Options ───────────────────────────────────────────────────────┐
│                                                                  │
│  ▼ General ──────────  ┌────────────────────────────────────┐   │
│    Display             │                                    │   │
│    Startup             │  (content for selected category)   │   │
│  ▼ Server              │                                    │   │
│    Connection          │  e.g. "General > Display":         │   │
│    Subtitle Appearance │                                    │   │
│  ▶ Paths               │  Theme: [System ▼]                 │   │
│  ▼ Translation         │  UI Language: [English ▼]          │   │
│    Engines             │  Font rendering: [ClearType ▼]     │   │
│    Per-language         │                                    │   │
│  ▶ TTS                 │                                    │   │
│  ▶ Hardware            │                                    │   │
│  ▶ Advanced            │                                    │   │
│                        └────────────────────────────────────┘   │
│                                                                  │
│  🔍 Search settings...                                           │
│                                                                  │
│                         [ OK ]  [ Cancel ]  [ Apply ]            │
└──────────────────────────────────────────────────────────────────┘
```

Categories and settings:

  General
    Display    — Theme (System / Light / Dark)
               — UI language (18+ locales, Feature #9)
    Startup    — Start with Windows
               — Check for updates on launch
               — Restore last session

  Server
    Connection — HTTP Port, HTTPS Port
               — Admin PIN
               — Allow Firewall rule (Feature #8b)
               — Auto-start server on launch
    Subtitle Appearance
               — Background color
               — Text color
               — Font family, size, bold
               — Live preview swatch

  Paths        — All 12 tool paths with Browse buttons:
                 whisper-cli, yt-dlp, ffmpeg, ffprobe,
                 YouTube model, Audio model, whisper-cpp model,
                 translation model, output root, SubtitleEdit,
                 glossary, Bibles directory
               — yt-dlp format string
               — [Verify All Paths]
               — [Download Models...]

  Translation
    Engines    — Translation enabled toggle
               — Backend selector: Local / DeepL / Google / Azure / Custom
                 (Feature #14 — pluggable backends)
               — API key fields (shown per backend)
               — [Test] button
               — Fallback to offline translation when offline toggle
               — Device: CUDA / CPU
               — Unload timer (minutes)
               — Translation port
               — [Check Dependencies]
               — Cost/usage tracking (Feature #14f)
    Per-language
               — Per-language backend overrides (Feature #14e)

  TTS          — TTS backends: piper, mms-tts, edgetts
                 (comma-separated or checkboxes)
               — Voice model management (Feature #6b — future)
               — Audio output device (for PA/NDI)
               — Output volume

  Hardware     — Hardware Readiness Score (Feature #12)
                 Overall score with traffic light (green/amber/red)
               — Component breakdown: GPU / CPU / RAM / Disk / OS
               — Tier classification: Minimum / Recommended / Optimal
               — Actionable recommendations
               — [Re-scan Hardware]
               — [Generate Spec Sheet...] (Feature #13)

  Advanced     — Live server port
               — [Reset All Settings]
               — [Export Diagnostics...] (Feature #4)
               — [Verify File Integrity] (Feature #4d)
               — Portable mode indicator (Feature #7)
```

#### Config Architecture (pending)

`AppConfig` is a flat bag with ~50+ properties, no validation, and fragile manual `SaveUiToConfig`/`LoadConfigToUi` mapping. `ServerOptions` is a separate copy that goes stale mid-session when colors/pin change. When building the Options dialog, group into sub-configs, use a single shared options object or change notification, and replace manual sync with binding/mediator.

---

## Live Session Wizard

Triggered by: **File > New Session** or **Start Session** when config is incomplete.

A multi-step dialog that walks through everything needed for a live session.
Maps to Feature #2 (Setup Wizard — Event Setup Mode).

```
Step 1 of 5: Hardware Check
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Checking your system...                                        │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Overall: ● 78/100 (GREEN — well suited)                 │   │
│  │                                                          │   │
│  │  GPU:  NVIDIA RTX 3060 (6GB)      ████████░░  80/100     │   │
│  │  CPU:  Intel i5-12400 (6 cores)   ██████░░░░  65/100     │   │
│  │  RAM:  16 GB                      ████████░░  85/100     │   │
│  │  Disk: 42 GB free                 ██████████  100/100    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Auto-configured: medium whisper model, translation enabled            │
│  (Feature #12f — smart defaults based on score)                 │
│                                                                 │
│                                    [ Skip ]  [ Next > ]         │
└─────────────────────────────────────────────────────────────────┘

Step 2 of 5: Audio Input
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Select your audio input device:                                │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ ● Microphone (Realtek HD Audio)                           │   │
│  │ ○ Stereo Mix (Realtek HD Audio)                           │   │
│  │ ○ VoiceMeeter Output (VB-Audio)                           │   │
│  └──────────────────────────────────────────────────────────┘   │
│  [Refresh]                                                      │
│                                                                 │
│  Speaker language: [English ▼]                                  │
│                                                                 │
│  Audio test: [  Test Microphone  ]                              │
│  Level: ████████████░░░░░░░░░░░░░  Good                        │
│  (Feature #3c — environment check)                              │
│                                                                 │
│                                    [ Back ]  [ Next > ]         │
└─────────────────────────────────────────────────────────────────┘

Step 3 of 5: Translation
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Enable live translation?                                       │
│  [x] Yes — translate subtitles for phone viewers                │
│                                                                 │
│  Translation engine: [Local (NLLB/MADLAD) ▼]                    │
│  Device: [CUDA ▼]                                               │
│                                                                 │
│  Status: ✓ Model loaded    ✓ Dependencies OK                    │
│          (or: ✗ Missing — [Install Now])                        │
│                                                                 │
│  Listener languages (pre-tick common ones):                     │
│  [x] French  [x] Spanish  [x] German  [ ] Portuguese           │
│  [ ] Italian  [ ] Polish  [ ] Romanian  [ ] More...             │
│  (Feature #2a step 3 — multi-select listener languages)         │
│                                                                 │
│                                    [ Back ]  [ Next > ]         │
└─────────────────────────────────────────────────────────────────┘

Step 4 of 5: Display & Network
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  How should subtitles look on phones?                           │
│                                                                 │
│  Background: [■ Black]   Text: [■ White]                        │
│  Font: [Segoe UI ▼]  Size: [12 ▼]  [ ] Bold                    │
│                                                                 │
│  Preview:                                                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  ██████████████████████████████████████████████████████   │   │
│  │  ████  The quick brown fox jumps over the lazy dog  ████   │   │
│  │  ██████████████████████████████████████████████████████   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Network: ✓ Connected to "Church-WiFi" (192.168.1.5)            │
│  (Feature #2a step 4 — verify phones can reach this network)    │
│                                                                 │
│                                    [ Back ]  [ Next > ]         │
└─────────────────────────────────────────────────────────────────┘

Step 5 of 5: Ready
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  Everything is set up!                                          │
│                                                                 │
│  Audio:       Microphone (Realtek HD Audio)                     │
│  Language:    English                                           │
│  Translation: Enabled (Local, CUDA) — 4 languages               │
│  Display:     White on Black, Segoe UI 12pt                     │
│  Server:      https://192.168.1.5:5081                          │
│  Hardware:    78/100 (Green)                                    │
│                                                                 │
│  ┌──────────────────────┐                                       │
│  │                      │  Phones scan this QR code             │
│  │    [QR CODE IMAGE]   │  or visit the URL above.              │
│  │                      │  (Feature #1)                         │
│  └──────────────────────┘                                       │
│                                                                 │
│  [ Copy URL ]  [ Print QR ]                                     │
│                                                                 │
│  Save as profile: [Sunday Service     ] [Save]                  │
│  (Feature #2b — named event profiles)                           │
│                                                                 │
│                                    [ Back ]  [ Start! ]         │
└─────────────────────────────────────────────────────────────────┘
```

New sessions inherit all defaults from the **Options dialog**. The wizard can optionally override them per-session.

---

## Toolbar Icons

Using simple, universally recognizable symbols (no culturally-specific imagery):

| Feature    | Icon concept           | Tooltip (localized)      |
|------------|------------------------|--------------------------|
| Live       | Play triangle + waves  | Live Translation         |
| Transcribe | Audio waveform + text  | Transcribe               |
| Translate  | "Aa" with arrow        | Translate Text           |
| Bible      | Open book              | Bible                    |
| QR Code    | QR grid pattern        | Show QR Code (Feature #1)|
| Options    | Gear                   | Options (Ctrl+,)         |

---

## Status Bar

Always visible at the bottom. Shows at a glance:

```
[● Server: Running :5081] [Clients: 47] [●●●●○ CPU:32% GPU:68%] [Live ● 00:12:34] [▲ Log]
```

- Green dot when server running, gray when stopped
- Client count updates in real-time
- Health dots from Feature #15h metrics (5 dots: green/amber/red per category)
- Hardware score traffic light (Feature #12) — green/amber/red circle
- Live indicator pulses when session is active, shows elapsed time
- Log toggle button to expand/collapse the log drawer
- Click status bar to expand metrics detail panel (Feature #15h)

**Metrics detail panel** (expandable overlay, per Feature #15h):
- Connection count over time
- Bandwidth usage
- Broadcast latency
- CPU/GPU usage bars
- Per-language client breakdown
- Threshold alerts (warning/critical)

---

## Log Panel

A collapsible drawer at the bottom (like VS Code's terminal panel):

- **Unified log** — all sources (server, live, job) color-coded by source
- Filter tabs: All | Server | Live | Job
- Can be collapsed to just the status bar
- Toggle via View > Show Log Panel, toolbar button, or status bar ▲ button
- [Clear] [Copy to Clipboard] buttons

---

## QR Code Window (Feature #1)

Floating, borderless, always-on-top, draggable:

```
┌──────────────────────────┐
│                          │
│    [QR CODE 300x300]     │
│                          │
│  https://192.168.1.5:5081│
│                          │
│  [Copy URL] [Save Image] │
│           [✕ Close]      │
└──────────────────────────┘
```

- Regenerates automatically when server restarts or IP changes (Feature #8b)
- Accessible from: toolbar QR button, Session menu, wizard step 5, Live workspace

---

## Crash Recovery Integration (Feature #8)

Not a visible workspace, but integrated throughout:

- **Watchdog timer** — 30-second health checks on all components (live server, translation server, Kestrel, TTS). Status reflected in the status bar health dots.
- **Session state persistence** — auto-saves transcript every 30s. On restart, offers "Resume previous session?"
- **Network change detection** — when IP changes, auto-restarts server, regenerates QR, notifies operator via status bar
- **Auto-reconnect** — phone clients already have reconnection logic; verify it handles server restart, network blip, laptop sleep/wake

---

## Glossary Editor (Feature #5)

Accessed via: Tools > Glossary Editor (opens FormFilterEditor)

**Enhancements for the redesign:**
- **Simple Mode / Advanced Mode toggle** — Simple: two-column "English term" / "Correct translation" table. Advanced: full trigger/source-lang/fixes model (current UI).
- **Import Glossary Pack** button — loads pre-built packs (e.g., `christian-theological.json`)
- **Export** button — shareable glossary files
- **Glossary Suggestion Review** tab (Feature #11B) — review/approve/reject listener-submitted corrections

---

## Feedback & Diagnostics Integration

### Post-Session Prompt (Feature #11A section b)
After stopping a live session, show an optional prompt:
- "How did this session go?" (1-5 rating, free text, audio conditions)
- Dismissable — not blocking

### Export Diagnostics (Feature #4)
Via File > Export Diagnostics or Options > Advanced:
- Collects: system info, hardware score, config, logs, checksums
- Creates ZIP for email to dev team

### Session Report (Feature #11A section c)
Auto-generated after each session:
- Listener count by language, average rating, glossary suggestions, device breakdown
- Stored in `feedback/reports/`

---

## Portable Mode (Feature #7)

When running from USB (`portable.flag` detected):
- Config stored relative to exe (not AppData)
- "Start with Windows" option hidden
- Firewall prompt skipped
- Status bar shows "Portable Mode" indicator
- File integrity check runs automatically on first launch from new location
- Wizard skips irrelevant steps

---

## Localization System

### Overview

A two-tier localization system: an **in-app Localization Editor** for end users and
translators, backed by local translation (NLLB/MADLAD) for auto-translation, plus a **CLI script** for developer
batch operations. Both read/write the same `locales/*.json` files and use the same
`auto`/`human` tracking mechanism.

### Tracking: auto vs human

Each locale JSON entry can be tracked as auto-translated or human-verified:

- `auto` — machine-translated via local translation, regenerated on demand
- `human` — submitted by a native speaker, permanently locked from auto-overwrite

### In-App Localization Editor (Tools > Localization Editor)

A dialog built into Every Tongue itself (`FormLocalizationEditor.vb`).
This is the primary way translators interact with localizations.

```
┌─ Localization Editor ────────────────────────────────────────────┐
│                                                                   │
│  Language: [Spanish (es) ▼]  [+ New Language...]                  │
│  Progress: ████████████░░░░  137/162 human (85%)                  │
│  Filter:   [All ▼]  🔍 [Search keys...        ]                  │
│                                                                   │
│  ┌───────────────────┬──────────────────┬──────────────────┬───┐  │
│  │ Key               │ English          │ Translation      │ ● │  │
│  ├───────────────────┼──────────────────┼──────────────────┼───┤  │
│  │ Btn_Start         │ Start Server     │ Comenzar serv.   │ 🟢│  │
│  │ Btn_Stop          │ Stop Server      │ Detener servidor │ 🟢│  │
│  │ Btn_Export        │ Export...        │ Exportar...      │ 🟡│  │
│  │ Msg_Welcome       │ Welcome to ...   │ Bienvenido a ... │ 🟢│  │
│  │ Msg_NewFeature    │ Cloud sync is... │ (missing)        │ 🔴│  │
│  └───────────────────┴──────────────────┴──────────────────┴───┘  │
│                                                                   │
│  🟢 = human   🟡 = auto   🔴 = missing                            │
│                                                                   │
│  [Auto-Fill Missing]  [Auto-Fill All]                              │
│  [Export Template CSV...]    [Import Template CSV...]              │
│  [Reset Selected to Auto]                                         │
│                                                                   │
│                              [ Save ]  [ Close ]                  │
└───────────────────────────────────────────────────────────────────┘
```

**Editor features:**

- **Language selector** — dropdown of existing locales + "New Language" to create one from scratch
- **New Language** — prompts for locale code and display name, creates a new `locales/{code}.json` with all keys
  auto-translated via local translation in one pass
- **DataGridView** — key, English source, current translation, status indicator
- **Inline editing** — click a translation cell to edit; saving marks it `human`
- **Filter dropdown** — All / Missing only / Auto only / Human only
- **Search** — filter keys by name or English text
- **Auto-Fill Missing** — translates only missing/empty keys using the app's
  built-in local translation service, marks them `auto`
- **Auto-Fill All** — retranslates all `auto` keys (leaves `human` untouched)
- **Export Template CSV** — for offline editing or sending to a volunteer
- **Import Template CSV** — loads a completed CSV, marks imported entries `human`
- **Reset Selected to Auto** — demotes selected rows back to `auto` (bad human translation)
- **Save** — writes the locale JSON file

**Local translation integration:**

The editor calls the same `ITranslationService` already used for live translation.
If the translation model is loaded, translations happen instantly on-device. If not loaded, the editor
offers to load it (respecting the unload timer from Options). This means the app can
generate its own UI translations in any of the 200+ languages the local engine supports, without
any external API or internet connection.

### CLI script (developer tool)

`scripts/translate.py` provides batch operations for CI/CD and development:

| Command | What it does |
|---|---|
| `--auto` | Fill missing + refresh `auto` keys across all locales via local translation |
| `--auto --lang ko,ar` | Same but only for specific locales |
| `--export-template es` | Generate CSV template for a volunteer |
| `--import file.csv --lang es` | Import volunteer translations, mark as `human` |
| `--reset-keys "X,Y" --lang es` | Demote specific keys back to `auto` |
| `--status` | Summary table: per-language counts of human vs auto vs missing |
| `--generate-js` | Regenerate `app.js` i18n block from all locale JSON files |

### Volunteer workflow

1. Open **Tools > Localization Editor**, select language, click **Export Template CSV**
   (or run `scripts/translate.py --export-template {lang}`)
2. Volunteer receives CSV:
   ```
   key,english,current,your_translation
   Btn_Start,Start Server,Iniciar servidor,
   Btn_Stop,Stop Server,Detener servidor,
   Msg_NoFile,No file selected,Ningún archivo seleccionado,
   ```
3. Volunteer fills in `your_translation` column (partial is fine — blank = keep current)
4. Open editor, click **Import Template CSV** — imported entries marked `human`
   (or run `scripts/translate.py --import file.csv --lang es`)

### New features / new keys

When a developer adds a new key to `locales/en.json` (English master):
- Opening the Localization Editor shows the key as 🔴 missing for all languages
- Clicking **Auto-Fill Missing** translates it via local translation with `comment=auto`
- Or the CLI `--auto` run does the same in batch
- Existing `human` entries are untouched
- A translator can later edit the auto value to upgrade it to `human`

### app.js i18n generation

Both the in-app editor (on Save) and the CLI (`--generate-js`) regenerate the i18n
object in `app.js` from the locale JSON files, using a key mapping. This eliminates
separately maintained JS translations — one source of truth, zero drift.

---

## Feature Plan Coverage Matrix

| Feature Plan Item | Where in UI Redesign |
|---|---|
| #1 QR Code Connection | Toolbar button, wizard step 5, Live workspace, floating window |
| #2 Setup Wizard | Session Wizard (5 steps via File > New Session), defaults from Options |
| #3 Audio Level Monitor | Live workspace — level meter + VAD indicator, wizard step 2 |
| #4 Diagnostic Bundle | File > Export Diagnostics, Options > Advanced |
| #4d File Integrity | Tools > Verify File Integrity, Options > Advanced |
| #5 Glossary Management | Tools > Glossary Editor (simple/advanced modes, import/export) |
| #6 TTS (Done) | Options > TTS section, audio output device config |
| #7 Portable USB | Portable mode detection, adapted wizard, relative paths |
| #8 Crash Recovery | Status bar health dots, watchdog, session persistence, network detection |
| #9 Multi-Language UI | Options > General (18+ locales), View > Language menu, `scripts/translate.py` auto+human pipeline |
| #10 Session Recording | Live workspace Save button, multi-format export dialog |
| #11A Field Feedback | Post-session prompt, phone client feedback button |
| #11B Glossary Enrichment | Glossary Editor review tab, import/export reviewed suggestions |
| #11C Device Compatibility | Phone client — connection-time warnings, feature indicators, suitability score |
| #12 Hardware Readiness | Options > Hardware section, wizard step 1, status bar, Help menu |
| #13 Spec Sheet Generator | Options > Hardware > [Generate Spec Sheet], Help menu |
| #14 Pluggable Translation | Options > Translation (backend selector, API keys, per-language overrides) |
| #14 Translate workspace | Nav rail "Translate" — text-to-text with backend selection |
| #15 Kestrel (Done) | Underlying server infrastructure — no direct UI, feeds status bar |
| #16 Bible (Done) | Nav rail "Bible" — native workspace mirroring web client |

---

## Stub Policy

Features that are not yet implemented but have a place in the UI should be present
as **disabled or placeholder items** so the overall layout is established from day one.
This avoids layout shifts as features are added later.

### Stub behavior

| UI Element | Stub behavior |
|---|---|
| **Menu items** | Visible but disabled (greyed out), tooltip: "Coming soon" |
| **Toolbar buttons** | Visible but disabled, same tooltip |
| **Nav rail items** | Visible, clicking shows a placeholder workspace with feature name + "Coming soon" message |
| **Options categories** | Category visible in tree, content area shows "This feature is not yet available" |
| **Wizard steps** | Steps that depend on unimplemented features show current defaults and skip gracefully |
| **Status bar segments** | Placeholder with "—" until the backing feature is wired up |

### What is implemented vs stub

| Item | Status | Notes |
|---|---|---|
| **Live workspace** | Implemented | Extract from current Live tab |
| **Transcribe workspace** | Implemented | Extract from current Job tab |
| **Translate workspace** | Implemented | Text-to-text translation with language selectors, sentence splitting, backend selection via registry |
| **Bible workspace** | Implemented | BibleService + web client exist; native desktop = WebView2 |
| **Options dialog** | Implemented | VS-style tree dialog, consolidate from current tabs |
| **Log panel** | Implemented | Unify current job log + server log |
| **Session Wizard** | STUB | Shell with steps, wired to existing settings where possible |
| **QR Code window** | Implemented | FormQrCode with QRCoder, accessible from session wizard and menu |
| **Audio Level Monitor** | STUB | Meter UI placeholder in Live workspace, no backend yet |
| **Hardware Score** | Implemented | Options → Hardware panel with HardwareScanner, weighted scoring, traffic light, recommendations |
| **Diagnostics Export** | Implemented | File → Export Diagnostics creates ZIP with system info, logs, config, integrity check |
| **Glossary Simple Mode** | STUB | FormFilterEditor toggle exists but simple mode not built |
| **Event Profiles** | STUB | Wizard step 5 "Save as profile" disabled |
| **Spec Sheet Generator** | Moved | Reclassified as documentation task (see Documentation section) |
| **File Integrity Check** | Implemented | Part of Diagnostics Export (#4d), build-time manifest + runtime verification |
| **Portable Mode** | STUB | Detection logic placeholder |
| **Feedback prompt** | STUB | Post-session dialog placeholder |
| **Localization Editor** | Implemented | Tools menu, DataGridView editor, local translation auto-fill, CSV import/export |
| **First-Run Language Picker** | Implemented | Flag grid, search, locale JSON downloaded via Download Manager |

---

## Implementation Approach

### Phase 1: Shell + Navigation
- `FormLanguagePicker.vb` — first-run language selector with flag tiles, search, local translation auto-generation
- New FormMain with MenuStrip, ToolStrip, nav rail (Panel with styled buttons), workspace Panel, StatusStrip, log panel (SplitContainer)
- Nav rail switches workspace content by showing/hiding UserControls
- All workspaces created — implemented ones have real content, others show stub placeholder
- All menu items created — implemented ones wired up, stubs disabled with tooltip
- All toolbar buttons created — same stub/live split

### Phase 2: Extract Existing Features into UserControls
- `LiveWorkspace.vb` — from current Live Translation tab (audio level meter = stub placeholder)
- `TranscribeWorkspace.vb` — from current Main/Job tab
- `FormOptions.vb` — VS-style Options dialog with tree categories, consolidated from current Settings, Paths, Subtitle Server tabs (Hardware section = stub)
- `LogPanel.vb` — unified log from current job log + server log
- `BibleWorkspace.vb` — WebView2 hosting the existing web Bible UI
- `TranslateWorkspace.vb` — STUB: language selectors + two text boxes + disabled Translate button
- `FormLocalizationEditor.vb` — DataGridView editor for locale JSON files, local translation auto-fill, CSV import/export, auto/human tracking
- Move all event handlers from FormMain into the respective UserControls
- FormMain becomes a thin shell that hosts UserControls and coordinates between them

### Phase 3: Session Wizard + QR Code
- `FormSessionWizard.vb` — 5-step wizard dialog (hardware step = stub until Feature #12)
- QR code generation (QRCoder NuGet package)
- `FormQrCode.vb` — floating QR window
- Validates dependencies at each step
- Event profile save/load in AppConfig (stub — save button disabled initially)

### Phase 4: Fill in Stubs
- `HardwareScanner.vb` — WMI + nvidia-smi, scoring, Options > Hardware section goes live
- Audio level monitor — `/audio-level` endpoint + polling + meter control
- Diagnostics export — system info collector + ZIP bundler
- File integrity — checksums.json generation + verification UI
- Translate workspace — wire to local translation backend, then pluggable backends (Feature #14)
- Glossary simple mode — two-column editing UI
- Event profiles — save/load named profiles in AppConfig

### Phase 5: Polish
- Keyboard shortcuts (Ctrl+1..4 for workspaces, F5 for start)
- Theming applied consistently across all workspaces
- `scripts/translate.py` — CLI batch translation for CI/CD, mirrors in-app editor logic
- Run auto-fill to propagate new UI strings to all locales
- Export templates for community translators to upgrade `auto` → `human`
- Status bar metrics detail panel (Feature #15h)
- Portable mode detection and path adaptation
