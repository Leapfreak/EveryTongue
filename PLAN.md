# EveryTongue — TODO (updated 2026-06-13, v1.9.13 — RELEASED)

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

### Benchmark Suite — COMPLETE (v1.8.2)
Full benchmarking form with 6 test types across 4 tabs: Translation Pipeline, Translation Concurrency, STT Comparison, STT Concurrency, TTS Comparison, TTS Concurrency. Resource monitoring (CPU/RAM/GPU/VRAM/temp), model identification in all outputs, unified CSV export with auto-save, debug logging throughout.

### Filter Editor — COMPLETE (v1.8.2)
Unified 3-tab filter editor (Hallucinations, Profanity, Glossary) with shared language selector. Glossary restructured to per-source-language dict format. CheckedListBox enable/disable for all items. Friendly language names throughout via LanguageCodeService. FormLanguageChooser searchable picker. ComboBox column for target languages. Filter hit logging at info level. Menu: Tools > Filter Editor.

### Log Workspace & Resilience — COMPLETE (v1.8.4)
Log nav button for full-screen Log workspace with bottom toolbar (copy/clear/search). Whisper-server recovery in ConversationAudioHandler: detects whisper subprocess death via `whisper_server_running` health field, auto-reloads model via `/load-model`. Bible panel click-outside fix for detached DOM nodes.

### Structured Logging System — COMPLETE (v1.8.5)
Replaced flat `AppLogger.Log(msg)` with numbered, categorised events. 120+ event IDs across 20 categories. All 285+ legacy calls migrated. DataGridView log viewer with category/level filtering, FormLogConfig dialog with Minimal/Normal/Verbose presets. Python sidecar logs parsed by level with per-server base event IDs. Session summary on shutdown. Rate limiter collapses repeated events.

### Config Architecture Refactor — COMPLETE (v1.9.0–v1.9.4)
All 9 phases: engine independence (per-engine config blocks, descriptors, `EngineConfigResolver`), template libraries (engine/speaker/display/filter-set), ConferenceTemplate-as-session with Online/Offline gate (no auto-fallback), runtime consumption (speakers, display templates, per-session filter sets), wizard convergence. Plan doc CONFIG_CHANGES.md deleted after completion — history in git.

### Architecture Audit — COMPLETE (v1.9.5)
Three-lens audit (engine independence, layering, correctness), all items fixed: async-sub exception containment, ConcurrentDictionary room state, Bible workspace async, safe JSON payloads, Speechmatics knowledge extracted from shared code (clause coordinator, `BuildStartJsonExtras`, registry metadata for model scan/paths/companion-translation), EndpointRegistration split into per-area Partial Module files, live-server local engines routed through the engines registry. ARCHITECTURE_AUDIT.md deleted after completion — history in commits d684db2..5e30f98.

### Legacy Removals + Per-Engine API Keys — COMPLETE (v1.9.6–v1.9.7)
`GoogleCloudSttApiKey` flat field retired via one-time migration into `SttApiKeys`; SessionTemplate retired (ConferenceTemplate-as-session won). `AppConfig.TranslationApiKeys` per-engine store mirrors the STT pattern; DeepL + Azure Translator registered and selectable; one generic `ConfigureCloudApiKeys` pass keys all cloud translation backends at server start and on Options save; shared Google key preserved via `CompanionTranslationKey` fallback (dedicated translation key overrides).

### Deepgram + Gladia Online STT Engines — COMPLETE (v1.9.11)
Two new online streaming STT engines via the existing plugin registries — zero shared-code changes. **Deepgram** (key `deepgram`): `live-server/engines/deepgram.py` streams 16 kHz PCM to `wss://api.deepgram.com/v1/listen` (`Authorization: Token` header) using the bundled `websockets` package; interim results → update, is_final accumulated → commit on speech_final; descriptor knobs Model (default `nova-3`) + EndpointingMs; "auto" language → `language=multi` on nova-3, omitted otherwise. **Gladia** (key `gladia`): `live-server/engines/gladia.py` does the v2 two-step handshake (POST `https://api.gladia.io/v2/live` with `x-gladia-key` → session websocket URL → binary PCM frames); partial/final utterance messages → update/commit; "auto" → empty `language_config.languages` + code_switching; descriptor knob EndpointingMs. .NET side: registry entries with `RequiresApiKey=True`/`SidecarMode="online"`/`ModelScanPattern="-"` + `DeepgramConfig`/`GladiaConfig` blocks in `Services/Stt/Configs/` (`BuildStartJsonExtras` carries `deepgram_model`/`deepgram_endpointing_ms`/`gladia_endpointing_s` to /start). No new pip deps. Untested against live vendor endpoints (no API keys on dev box).

### Azure AI Speech Online STT Engine — COMPLETE (v1.9.12)
Third engine added via the same plugin registries as Deepgram/Gladia — zero shared-code changes. **Azure AI Speech** (key `azure-speech`): `live-server/engines/azure_speech.py` uses the official `azure-cognitiveservices-speech` SDK (PushAudioInputStream @ 16 kHz s16 mono + continuous recognition); `recognizing` → update, `recognized` → commit, `canceled` → `_thread_error`. "auto" → continuous language identification (`SpeechServiceConnection_LanguageIdMode=Continuous`, max 10 candidate locales from the AutoDetectLanguages CSV, default en/es/fr/de/it/pt/nl/pl); explicit whisper ISO-1 codes map to BCP-47 via a curated dict + `xx → xx-XX` heuristic. SDK import is LAZY (inside the streaming thread) so the live-server boots without the package; `azure-cognitiveservices-speech` added to `live-server/requirements.txt` and to `GetMissingPythonPackages` when `azure-speech` is the selected backend (standard Download Manager flow). .NET side: registry entry (`RequiresApiKey=True`/`SidecarMode="online"`/`ModelScanPattern="-"`) + `AzureSpeechConfig` block (Region default `westeurope`, SegmentationSilenceMs, AutoDetectLanguages; `BuildStartJsonExtras` carries `azure_region`/`azure_segmentation_ms`/`azure_autodetect_languages` to /start). Untested against the live Azure endpoint (no API key on dev box).

### Cloud Translation Parity, Cost & Latency — COMPLETE (v1.9.8–v1.9.9, plan #14 a–h all done)
Cloud backend output now gets the same glossary fixes and profanity masking as NLLB — `GlossaryPostProcessor`/`ProfanityPostProcessor` port the Python filter semantics and run in `TranslationOrchestrator`, gated by `ITranslationBackend.AppliesFiltersInternally` (per-room filter sets honoured, global files fallback). `TranslationUsageTracker` counts billable characters per backend per month with optional budgets (warning-only, never blocks) and rolling latency averages, shown on the Options Translation page. DeepL targets now translate concurrently. **v1.9.9:** cloud engines honoured in ALL pipelines — the Translate workspace and Transcribe job pipeline route through the orchestrator when the effective engine is cloud (previously NLLB-only dead-ends); NLLB-selected behavior byte-identical. See [#14](#14-pluggable-translation-backends-cloud-apis).

### Engine Expansion — COMPLETE (v1.9.10–v1.9.13, released to GitHub as v1.9.13)
Ten engines added, all registry-driven with per-engine keys and cross-vendor key sharing. **Translation (v1.9.10)**: DeepSeek + OpenAI (shared `OpenAiCompatibleBackend`), LibreTranslate (configurable endpoint, self-hostable), Amazon Translate (AWSSDK, composite `accessKeyId:secret` key, region in endpoint field) → 9 translation engines total (see [#14](#14-pluggable-translation-backends-cloud-apis)). **STT (v1.9.11–v1.9.12, entries above)** → 9 STT engines total. **TTS (v1.9.13)**: Azure AI Speech TTS (official Edge voice catalogue, with SLA), Google Cloud TTS, OpenAI TTS, with `TtsApiKeys`/`TtsEndpoints` plumbing and entry-declared key fallbacks (Azure key shared STT↔TTS; Google key shared STT↔Translate↔TTS; OpenAI key shared Translate↔TTS) → 6 TTS engines total (see [#6](#6-text-to-speech--server-side-engine)). UI plumbing verified surface-by-surface (Options combos/key/endpoint fields, template manager, descriptor editor, connectivity gate, Download Manager) — all registry-enumerated, zero hardcoded engine lists. **⚠ All v1.9.10–v1.9.13 cloud engines were implemented from vendor docs and are NOT yet smoke-tested against live endpoints (no API keys on the dev box) — real-key sessions on the test machine are the actual verification.** Release: GitHub v1.9.13 (installer + app zip + update manifest); setup.iss hardened (CUDA DLL and .pyc/.log excludes).

## User-Reported Issues & Tasks
- [x] Implement stubs — most done (QR Code, Hardware Score, Diagnostics Export, File Integrity, Translate workspace). Remaining stubs: Session Wizard, Audio Level Monitor, Event Profiles, Spec Sheet Generator, Portable Mode, Feedback prompt
- [x] Connected Clients dialog — popup form showing all connected phones with model, OS, browser, language, TTS, connection time
- [ ] Audio routing: NDI or Direct Audio output

## Suggested Next Priorities
1. ~~**Structured Logging System**~~ — DONE (v1.8.5)
2. ~~**Config refactor runtime consumption (Phases 6–9)**~~ — DONE (v1.9.0–v1.9.4; plan doc CONFIG_CHANGES.md deleted after completion, history in git).
3. ~~**Architecture audit backlog**~~ — DONE (v1.9.5, commits d684db2..5e30f98; ARCHITECTURE_AUDIT.md deleted after completion).
4. **Smoke-test the new cloud engines with real API keys** (test machine) — Deepgram/Gladia/Azure Speech STT, DeepSeek/OpenAI/LibreTranslate/Amazon translation, Azure/Google/OpenAI TTS were built from vendor docs only; verify auth, response shapes, and fallback behavior per engine, fix against real responses.
5. **Regenerate CDN locale packs** — ~100 new string keys added in v1.9.x exist only in `locales/en.json`; the downloadable packs on the GitHub CDN need regenerating (carried over from config refactor).
6. Audio Level Monitor — operator feedback, prevents bad audio
7. Setup Wizard expansion — integrates QR, audio monitor, hardware score
8. Cross-platform headless server (Linux/Docker)

---

## Structured Logging System — COMPLETE (v1.8.5)

All 5 phases done. Replaced flat `AppLogger.Log(msg)` with numbered, categorised events. `LogCategory` enum (20 categories), `LogEvents` module (120+ event IDs), `LogEventRegistry`, `LogRoutingConfig` with Minimal/Normal/Verbose presets. `FormLogConfig` dialog for per-category routing. DataGridView log viewer with category/level filtering, color-coded rows, tooltips, pause/resume. All 285+ legacy calls migrated to structured event IDs. Python sidecar logs parsed by level (BaseEventId + offsets). Session summary on shutdown. Rate limiter collapses repeated events. Key files: `Services/Infrastructure/LogEvents.vb`, `LogRoutingConfig.vb`, `LogEventRegistry.vb`, `AppLogger.vb`, `Forms/FormLogConfig.vb`.

## Immediate TODO
- [x] Filter Editor overhaul — shared language combo, per-language glossary, checkboxes, friendly names, filter hit logging (#5)
- [x] Log workspace — full-screen Log with nav button, bottom toolbar, whisper-server recovery (v1.8.4)
- [x] Structured Logging System — event IDs, categories, configurable routing (v1.8.5)
- [ ] Audio Level Monitor — operator feedback, prevents bad audio (#3)
- [ ] Rooms desktop dashboard — active rooms overview (#19g)

---

## Robust VAD Pipeline — COMPLETE (v1.8.0–v1.8.1)

All 9 phases implemented and tested. Frame-level Silero VAD with 4-tier commit system (SOFT 400ms / DURATION 8s / HARD 750ms / FORCE 25s). Pipeline stable across 45+ consecutive inferences. Key files: `live-server/vad/` package (pipeline.py, state_machine.py, frame_vad.py, buffers.py, merger.py, segment.py).

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
- Mesh WiFi / mDNS service discovery for automatic server finding
- Session recording & per-room transcript export
- Plugin auto-discovery from `plugins/` folder
- Plugin Manager UI with model management
- **Online/Offline mode — offline-detection prompt** — when a session is in Online mode and a cloud call fails or connectivity drops, *prompt* the operator ("Looks like you're offline — switch to offline engines?") rather than silently switching. The Online/Offline mode itself is an explicit user-set switch with **no auto-fallback** (being designed in the config refactor); this prompt is a later enhancement layered on top.
- **v2.0 legacy removals (from config refactor)** — ✅ DONE (v1.9.6, 2026-06-13). `GoogleCloudSttApiKey` read-bridge retired: live fallback removed from `GetSttApiKey`/`SetSttApiKey`; replaced by a one-time migration in `ConfigManager.ApplyDefaults` (legacy value copied into `SttApiKeys["google-cloud-stt"]` if empty, then cleared; property is now deserialize-only via `JsonIgnore(WhenWritingDefault)` so it vanishes from config.json on next save). The five confirmed-dead settings (`SkipDownloadIfExists`, `Hotwords`, `FreqThreshold`, `PrintRealtime`, `TranslationUnloadMinutes`) were already absent from the codebase — nothing to remove. SessionTemplate retired: `Models/Templates/SessionTemplate.vb` deleted (`ConnectivityMode` enum moved to its own file — still live), `TemplateLibraryStore` session slot removed (stale `session-templates.json` on disk is ignored harmlessly), `SessionResolver.Resolve`/`ResolvedSession`/`ResolveDisplay` deleted; `SessionResolver.ResolveFilterSet` kept (live in ConferenceController). Kept untouched: `ParallelJobs`/`ChunkSizeSec`/`PollIntervalMs`/`ChunkTimeoutMin`/`KeepChunkFiles`/`KeepPreview`/`PathModelAudio`/`PathOutputRoot`/`TranslationDevice` (live consumers).
- **Cloud STT in the Transcribe workspace (batch engines)** — the vendors support it (Speechmatics Batch API `POST /v2/jobs/` can even return SRT directly; Google has batch recognize), but our `live-server/engines/` modules are realtime-streaming only. Work: a Speechmatics batch client (upload converted WAV → poll job → write returned SRT → existing translation step; bypasses chunking/VAD entirely), an engine combo on the Transcribe tab (gated by `WorkspaceCapabilities` + registry `RequiresApiKey`), job progress reporting. Google batch later (GCS upload requirements, SRT assembly from word timings). Deferred from config-refactor Phase 4 (see CONFIG_CHANGES.md).

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
| [5](#5-filter-editor--glossary-profanity--hallucination-management) | Filter Editor — Glossary/Profanity/Hallucination | **Done** (core) | 2 |
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
| [14](#14-pluggable-translation-backends) | Pluggable Translation Backends (Cloud APIs) | COMPLETE (a-h) | 4 |
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

## 5. Filter Editor — Glossary, Profanity & Hallucination Management

**Status:** Core editing done. Unified 3-tab Filter Editor with shared language selector, per-language glossary, checkbox enable/disable, friendly language names, and filter hit logging.

**What exists (v1.8.2):**
- **Unified language selector** — one combo at the top selects source language for all three tabs
- **Hallucinations tab** — CheckedListBox with enable/disable per phrase, add/remove, save & hot-reload to live-server
- **Profanity tab** — CheckedListBox with enable/disable per word, add/remove, save & hot-reload to translate-server
- **Glossary tab** — per-source-language entries (dict format), DataGridView with enable checkbox, trigger/comment fields, fixes grid with ComboBox target language column
- **FormLanguageChooser** — searchable modal picker for adding languages (FLORES or ISO 639-1 modes)
- **Friendly language names** throughout via `LanguageCodeService` — users never see raw codes
- **Filter hit logging** — info-level logs when glossary, profanity, or hallucination filters trigger
- **JSON formats** — hallucinations: `{"iso1": [{text, enabled}]}`, profanity: `{"flores": [{word, enabled}]}`, glossary: `{"flores": [{trigger, comment, enabled, fixes}]}`
- 26 glossary entries (12 Catalan, 16 Spanish including shared entries)
- Menu: Tools > Filter Editor

**What to improve:**

### a) Glossary Packs / Import-Export
- Pre-built glossary pack: `glossary-packs/christian-theological.json` containing common Christian terms across major European languages
- Import button: load a pack file and merge entries (skip duplicates by trigger)
- Export button: save current glossary as a shareable file

### b) Simplified Editing UI
- Add a "Simple Mode" view: two-column table "English term" → "Correct translation in [language]"
- Auto-generates the underlying trigger/fixes structure
- Keep current UI as "Advanced Mode"

### c) Community Contribution Workflow
- Handled by Features #11A and #11B — listeners flag phrases on their phones (#11A glossary suggestion via long-press), suggestions flow through the language manager review workflow (#11B)

**Complexity:** Medium. Import/export is straightforward; simplified UI is the main design challenge.

---

## 6. Text-to-Speech — Server-Side Engine

**Status:** Done. TtsOrchestrator with 6 backends (Piper, MMS-TTS, EdgeTTS, Azure AI Speech TTS, Google Cloud TTS, OpenAI TTS), TtsCache ring-buffer, fire-and-forget pipeline, hybrid browser/server approach, Bible verse TTS, local NAudio output. Not done: Piper voice model download UI (b), earphone mode (d).

**v1.9.13:** Added three online key-requiring TTS engines — Azure AI Speech TTS (`azure-tts`, region endpoint, shares the Azure AI Speech STT key), Google Cloud TTS (`google-tts`, shares the Google Translate/STT key), OpenAI TTS (`openai-tts`, gpt-4o-mini-tts "alloy", shares the OpenAI translation key). Per-engine TTS key plumbing mirrors translation: `AppConfig.TtsApiKeys`/`TtsEndpoints`, `TtsBackendRegistry.ResolveTtsApiKey`/`ConfigureCloudTtsKeys` (entry-declared `FallbackApiKey`), Options TTS page key/endpoint fields, shared Edge/Azure neural voice catalogue (`NeuralVoiceCatalog`).

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

**Status:** COMPLETE (a–h done, v1.9.8). Vendor multi-text batch APIs intentionally N/A — the streaming pipeline submits one short commit per request, so there is no multi-text batch to send; cross-target parallelism is implemented instead.

**Done:** `ITranslationBackend` interface, `TranslationOrchestrator` with fallback chain, `DeepLBackend`/`GoogleBackend`/`AzureBackend` in `CloudTranslationBackend.vb`, `SidecarTranslationBackend`, `TranslationBackendRegistry` with Options UI, `language-codes.json` (161 languages) with `LanguageCodeService`, per-language backend overrides via `LanguageOverrides`. **v1.9.7:** DeepL + Azure Translator registered in the backend registry (selectable in Options); per-engine `TranslationApiKeys` store with Options key field (shown only for `RequiresApiKey` engines); one generic `ConfigureCloudApiKeys` pass keys all cloud backends at server start + Options save (GoogleBackend's IOptions special case removed); Google STT key still powers Google Translate via `CompanionTranslationKey` fallback.

**(h) Glossary Integration — DONE:** `Services/Translation/GlossaryPostProcessor.vb` is a faithful .NET port of the Python `Glossary` class (case-insensitive substring trigger on source text, per-target `{wrong: right}` fixes, punctuation-aware plain/word-boundary replacement, mtime-keyed file cache, per-request filter set with fallback to global glossary). Applied in `TranslationOrchestrator.ApplyLocalFilters` to any backend with `ITranslationBackend.AppliesFiltersInternally = False` (all cloud backends); the sidecar is skipped because Python already applies filters. Global glossary path flows via `ServerOptions.GlossaryFilePath`. **v1.9.8:** profanity parity follow-up done — `ProfanityPostProcessor.vb` mirrors the Python `_filter_profanity` (target-FLORES-keyed word lists, legacy string + `{word, enabled}` formats, one case-insensitive `\b(...)\b` regex per language, matches masked as `[...]`, mtime-keyed cache, per-request set with global fallback via `ServerOptions.ProfanityFilePath`), applied right after the glossary in `ApplyLocalFilters`.

**(f) Cost Awareness — DONE (v1.9.8):** `Services/Translation/TranslationUsageTracker.vb` counts characters submitted to cloud backends (source length × target count — what vendors bill) per backend key per calendar month, persisted to `%AppData%\EveryTongue\translation-usage.json` with debounced writes (dirty flag + 30s timer + flush on shutdown; the translate path never touches disk). Counted at the orchestrator cloud seam (`InvokeBackendAsync`, gated on `RequiresInternet`, backend name → registry key via `TranslationBackendRegistry.FindByBackendName`). Budgets in `AppConfig.TranslationMonthlyCharBudgets` (0/absent = no budget); crossing a budget logs ONE `TRANS_BUDGET_EXCEEDED` (4011) warning per backend per month — translation is never blocked. Options Translation page shows a per-engine usage label ("This month: N characters / budget N (avg N ms)") and a budget field, visible only for `RequiresApiKey` engines, saved per engine like the API key.

**(g) Latency Considerations — DONE (v1.9.8):** Stopwatch around every cloud backend call at the orchestrator seam feeds per-backend in-memory rolling averages in the usage tracker; the average shows in the Options usage label. The existing `TRANS_RESULT` line in `ConferenceController.TranslateTargetsAsync` already carries per-translate elapsed ms. Cross-target parallelism: DeepL's per-target loop now issues bounded concurrent requests (SemaphoreSlim(4) + Task.WhenAll, result order preserved); Google was already parallel and Azure already sends all targets in one call. Vendor multi-text batching is N/A for the streaming pipeline (one commit text per request — there is never a batch of texts to send).

**SidecarTranslationBackend DI note — resolved:** not a gap; it is registered dynamically by design (`FormMain` calls `orchestrator.RegisterBackend` once the Python sidecar starts, since the backend wraps the FormMain-owned legacy `TranslationService` which only becomes available at sidecar startup; nothing resolves it through DI).

**Cloud engines honoured in ALL desktop pipelines — DONE (v1.9.9):** the Translate workspace (`TranslateController.RunTranslateAsync`) and the Transcribe job subtitle translation (`PipelineRunner.TranslateSubtitlesAsync`) now route through the `TranslationOrchestrator` (via `ServerController.GetTranslationOrchestrator()`) whenever the EFFECTIVE configured engine is a cloud backend — keys, fallback chain, glossary/profanity post-processing, usage counting and latency tracking all come from the orchestrator. New registry helpers: `TranslationBackendRegistry.ResolveEffectiveBackendKey` (user selection + STT companion auto-select, single source of truth shared with `FormMain.StartTranslationService`) and `TryActivateConfiguredCloudBackend` (syncs config → orchestrator active backend, covers cloud-only machines where the NLLB sidecar never starts). NLLB selected → behavior unchanged (same direct sidecar HTTP calls, same sentence splitting, FLORES codes); server down → graceful NLLB-direct fallback with existing localized statuses. Rooms/conference/Bible/benchmark already used the orchestrator.

**Four new cloud engines — DONE (v1.9.10):** DeepSeek, OpenAI, LibreTranslate, and Amazon Translate added as registry-driven cloud backends (keys `deepseek`, `openai`, `libretranslate`, `amazon-translate`). **DeepSeek + OpenAI** share one `OpenAiCompatibleBackend` (`Services/Translation/OpenAiCompatibleBackend.vb`) — POST `{base}/chat/completions` with Bearer key, temperature 0, system prompt "translate from X to Y, output ONLY the translated text", language names via `LanguageCodeService.GetDisplayNameForCode`, reply defensively stripped of surrounding quotes; models are `deepseek-chat` (base `https://api.deepseek.com`) and `gpt-4o-mini` (base `https://api.openai.com/v1`); health = GET `{base}/models`. **Per-engine endpoint setting** (generic, mirrors `TranslationApiKeys`): `AppConfig.TranslationEndpoints` + `Get/SetTranslationEndpoint`, registry `Entry.RequiresEndpoint`/`DefaultEndpoint`, overridable `CloudTranslationBackend.ConfigureEndpoint`, Options Translation page "Endpoint / Region:" field shown only for `RequiresEndpoint` engines with the default as placeholder, pushed in the same `ConfigureCloudApiKeys` pass at server start + Options save. **LibreTranslate** (`LibreTranslateBackend.vb`): POST `{endpoint}/translate`, ISO 639-1 codes via the new shared `FloresToVendorIso` helper (google column → ISO 639-1, region suffix stripped), default endpoint `https://libretranslate.com`, key optional (self-hosted instances are keyless — `IsAvailable` = endpoint resolved), health = GET `{endpoint}/languages`. **Amazon Translate** (`AmazonTranslateBackend.vb`, NuGet `AWSSDK.Translate` 3.7.501.59): credentials in the standard key field as composite `accessKeyId:secretAccessKey` (split on first ':'; malformed → unavailable + one TRANS_ERROR), endpoint field holds the AWS REGION (default `us-east-1`), `AmazonTranslateClient` with `BasicAWSCredentials` + `RegionEndpoint.GetBySystemName`, client disposed/recreated on credential/region change, unmapped language codes skip the target with a TRANS_ERROR (never throw), health = `ListLanguagesAsync`. All four flow through the existing orchestrator seam automatically (glossary/profanity post-processing, usage counting, latency, fallback chain) — no consumer code changes; `GoogleBackend` now shares `FloresToVendorIso` too.

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

### Done: Priority Queue Pipeline

`PriorityWorkQueue(Of TResult)` in `Services/Scheduling/` — bounded concurrency, starvation prevention (age-based priority promotion), cancellation, metrics. Used by `TranslationOrchestrator` and `TtsOrchestrator`.

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

**Status:** Done (v1.8.2). Full benchmark form (FormTranslationBenchmark) with 6 test types: Translation Pipeline, Translation Concurrency, STT Engine Comparison, STT Concurrency, TTS Engine Comparison, TTS Concurrency. Includes test corpus, quality scoring, resource monitoring, model identification, unified CSV export with auto-save to AppData, debug logging.

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

**Status:** Mostly implemented. Nav rail, 4 workspaces (Live, Transcribe, Translate, Bible), FormOptions (VS-style tree dialog), log panel, status bar, FormLanguagePicker, toolbar icons — all built. FormLocalizationEditor with auto/human tracking, CSV import/export built.

**Remaining stubs:** Session Wizard (5-step), Audio Level Monitor (meter UI), Glossary Simple Mode, Event Profiles, Portable Mode, Feedback prompt, Spec Sheet Generator.

**Config Architecture TODO:** Group AppConfig into sub-configs, replace manual SaveUiToConfig/LoadConfigToUi with binding/mediator.
