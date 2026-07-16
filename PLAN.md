# EveryTongue — TODO (updated 2026-07-17, v2.8.2 — released everywhere: git, ghcr :2.8.2/:latest, GitHub Release. v2.8.0 = three-page IA restructure; **v2.8.1 = CRITICAL fix**: the IA surgery doubled `<div id="panel">` in index.html, nesting the whole room UI (captions/picker/Bible) inside the hidden settings panel — captions received+rendered but INVISIBLE. Found via field instrumentation in 4 runs (received/rendered counters → visual-state heartbeat → ancestor chain named `container[0x0] inside panel[0x0 none]`). **FIELD-CONFIRMED FIXED** (session 20260716_2025: 43-pass integrity, pickLang→picker_hidden, captions ONSCREEN). **v2.8.2 = cleanup**: diagnostics now opt-in via `?diag=1` (or localStorage etDiag=1) — badge/heartbeats/picker-logs off by default, anomaly reporting (render errors, skipped commits, ws errors) always-on; one-shot surgery scripts deleted. `tools/render-test.js` (real app.js in headless Edge, runs with diag=1) asserts PHYSICAL visibility — container parent must be BODY + nonzero on-screen rect. LESSONS: scripted HTML surgery needs a tag-balance check; "in DOM = rendered" was the harness blind spot; **the startup integrity line (`43 pass, 0 fail`) is the deploy truth-test — a 1-fail wwwroot deploy cost 3 debugging rounds because the warning scrolled past. Follow-up: integrity failures on wwwroot should self-heal or block loudly.**)

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

### Engine Expansion — COMPLETE (v1.9.10–v1.9.13)
Ten engines added, all registry-driven with per-engine keys and cross-vendor key sharing. **Translation (v1.9.10)**: DeepSeek + OpenAI (shared `OpenAiCompatibleBackend`), LibreTranslate (configurable endpoint, self-hostable), Amazon Translate (AWSSDK, composite `accessKeyId:secret` key, region in endpoint field) → 9 translation engines total (see [#14](#14-pluggable-translation-backends-cloud-apis)). **STT (v1.9.11–v1.9.12)**: Deepgram, Gladia, Azure AI Speech → 9 STT engines total. **TTS (v1.9.13)**: Azure AI Speech TTS (official Edge voice catalogue, with SLA), Google Cloud TTS, OpenAI TTS, with `TtsApiKeys`/`TtsEndpoints` plumbing and entry-declared key fallbacks (Azure key shared STT↔TTS; Google key shared STT↔Translate↔TTS; OpenAI key shared Translate↔TTS) → 6 TTS engines total (see [#6](#6-text-to-speech--server-side-engine)). UI plumbing verified surface-by-surface — all registry-enumerated, zero hardcoded engine lists.

### Per-Room Translation Engines — COMPLETE (v1.9.19–v1.9.21)
A conference room runs its OWN translation engine, overriding the global default (global is only the default for non-conference paths). `TranslationOrchestrator.TranslateAsync` gained a `backendOverride`; `ConferenceController` stores `_roomTranslationKey` per room. The local **NLLB sidecar reloads to the room's chosen model** on demand (room wins; single model at a time, concurrent rooms share + warn); cloud engines run independently per room. **Speechmatics translation is now a selectable engine** ("Speechmatics (inline)", `InlineWithStt` metadata) rather than the old global checkbox — inline when the room's STT is Speechmatics, else falls back to the global default. Verified on Jezer (session 20260613_113138: room got real NLLB-3.3B via reload, single-engine output, zero errors).

### Robustness & Release Hardening — COMPLETE (v1.9.14–v1.9.18)
Installer no longer bundles CUDA/AWS DLLs or `.pyc`/`.log`; AWS SDK is an on-demand Download Manager component; resilient GitHub API usage (cached release lookups survive 403 rate limits); `tool-versions.json` removed from the integrity manifest; cloud-engine config never crashes startup when an optional SDK is missing; glossary/profanity applied to Speechmatics-inline translations; no false "live server exited unexpectedly" on graceful room close.

### v2.0.0 — RELEASED (2026-06-13)
Milestone bundling all of the above: pluggable engines across STT/translation/TTS, per-room translation, cloud parity (glossary/profanity + usage/latency), and release hardening. GitHub v2.0.0 (installer + app zip + AWS SDK component + update manifest).
**⚠ Still untested against live vendor endpoints (no keys on dev/Jezer yet):** Deepgram/Gladia/Azure STT, DeepSeek/OpenAI/LibreTranslate/Amazon translation, Azure/Google/OpenAI TTS. The Speechmatics + NLLB-3.3B + Piper stack is proven.

### v2.0.1–v2.0.16 — RELEASED (2026-06-14/15)
Field-hardening + UX from Jezer logs. Fresh-install component delivery (per_page=100 asset discovery, faster-whisper-after-Python, whisper-cli DM entry, CUDA-12 PATH); NLLB int8 variants; lazy conversation-rooms warm-up; audio-device-by-name; startup orphan-sidecar cleanup; Download-Manager CUDA checks now require runtime DLLs (not just the exe); host "Clear Captions" button; removed dead whisper dials (host panel + Conference Templates); **whisper-server crash fixed** (`GGML_NO_BACKTRACE`, was `GGML_ASSERT`/exit 3221226505 on both CUDA & Vulkan); whisper-cpp-cuda now runs the CUDA binary; speaker switch builds the correct backend; empty template paths inherit the machine baseline; **STT-templates vs room-templates decluttered** (companion templates hidden from pickers) + button labels fixed; **Default Speaker** per conference template (rooms boot pre-selected).

### v2.1.0–v2.1.6 — RELEASED (2026-06-15/16)
- **System-wide Dictation (voice typing)** — NEW feature. Speak → text typed into whatever textbox has focus in any app; tray submenu (start/stop, output language, mode) + global hotkeys; optional translate-while-dictating; Tools→Options→Dictation. Stable Win32 only (SendInput/RegisterHotKey/GetAsyncKeyState — no global keyboard hook). **VERIFIED working on Jezer** (continuous + SendInput + transcribe-only). Still to confirm on hardware (built, not suspected broken): translate-while-dictating, push-to-talk hold, clipboard insertion mode.
- **Conversation-room translation regression FIXED** (v2.1.6) — rooms route to the ACTIVE engine (Local/NLLB) but the readiness gate checked *any* backend; a keyed cloud engine masked the lazy NLLB sidecar being down, so it never started → original text passed through. Now checks the active engine + warms translation with STT. Verified bidirectional spa↔eng on Jezer. Also: each participant now sees the whole feed in their own language (speaker's own line was stuck in the spoken language). Added `ROOM_TRANSLATION_ROUTING` (5107) diagnostic. **LESSON: when something "isn't working", verify the sidecar PROCESS is running (its log has activity) before theorizing about logic.**

### v2.2.0–v2.2.1 — RELEASED (2026-06-19): concurrent multi-engine translation
Removed the single-offline-model limitation. **Different rooms can now run different offline translation models simultaneously**; rooms on the same model+precision share one sidecar (no double-load); the model's sidecar is freed (VRAM) when the last room using it leaves; GPU-OOM still falls back to CPU. New `TranslationSidecarPool` (one translate-server per model+compute signature, refcounted by room, own port + `translate-server-<port>.log`). Global default stays the "Local" sidecar; others register as per-model backends and route via the existing `backendOverride`. v2.2.1 extends per-room engine selection to **conversation rooms** (lobby dropdown + `GET /api/translation-engines`). Cloud was already concurrent; offline is now at parity. **Not yet hardware-tested on Jezer.**

### v2.3.0 — BUILT + PUBLISHED (2026-06-19): engine-readiness indicators
People were speaking before STT was capturing → losing the start of sentences, with no signal that models were still loading. v2.3.0 relays **transient** "preparing → ready" status to rooms and desktop features (shown while loading, **removed once ready** — no clutter). Where there's a controllable input, it's **disabled until the speech engine is actually capturing**; translation readiness is a **separate, non-blocking** note (cloud/inline engines = instant, no note). New `Services/Rooms/RoomReadinessNotifier.vb` (DI singleton): per-room state, `Watch(roomId, sttProbe, transProbe)` polls each engine's real readiness and broadcasts the room-scoped WS message `{"type":"roomStatus","scope":"stt"|"translation","state":"preparing"|"ready"}` (fail-open on timeout so a dropped message never traps a mic); `ResendStateToClient` covers late joiners; `ClearRoom` on close. **Only the affected see it** — messages are room-scoped (`ISubtitleService.BroadcastRawToRoom`/`SendRawToClient`, never cross-room) and only "preparing" is ever emitted (a 2nd room reusing the already-warm shared live-server, existing members on join, and joiners of an already-ready room all see nothing). **Conversation rooms** warm on JOIN (not first audio, else the mic-gate would deadlock the lazy warm-up): `SubtitleHub` → `ConversationAudioHandler.BeginWarmUp`. **Conference rooms**: `ConferenceController.StartReadinessWatch` at room create/restart — no web mic to gate, so the banner is informational. Web client (`app.js`): `handleRoomStatus` + transient `#roomStatusInd` banner, `setPttEnabled` gates the mic, 90s client safety-timer. Desktop: `DictationService.ReadinessChanged` → tray balloon `Dict_Preparing`→`Dict_Ready`; the Translate workspace already had readiness status. `LogEvents.ROOM_READINESS=5108`. **Field-validated 2026-07 (preparing→ready observed in the Lite container PTT test and live web-mic services); shipped in releases ≥ v2.6.6.**

## User-Reported Issues & Tasks
- [x] Implement stubs — most done (QR Code, Hardware Score, Diagnostics Export, File Integrity, Translate workspace). Remaining stubs: Session Wizard, Audio Level Monitor, Event Profiles, Spec Sheet Generator, Portable Mode, Feedback prompt
- [x] Connected Clients dialog — popup form showing all connected phones with model, OS, browser, language, TTS, connection time
- [ ] Audio routing: NDI or Direct Audio output

## Suggested Next Priorities (post-2.1.x)
1. **Smoke-test the new cloud engines with real API keys** (test machine) — Deepgram/Gladia/Azure Speech STT, DeepSeek/OpenAI/LibreTranslate/Amazon translation, Azure/Google/OpenAI TTS were built from vendor docs only; verify auth, response shapes, and fallback behavior per engine, fix against real responses. (The Speechmatics + NLLB-3.3B + Piper stack is proven.)
2. **Confirm the remaining Dictation modes on hardware** — translate-while-dictating (set a tray output language), push-to-talk hold, clipboard insertion. Core dictation (continuous + SendInput) is verified.
3. **Regenerate CDN locale packs** — string keys added across v1.9.x → v2.3.x (incl. Dictation `Dict_*`/`Opt_Dict*` plus readiness `Dict_Preparing`/`Dict_Ready`, Default-Speaker `Tmpl_*`, Manage-button labels) exist only in `locales/en.json`; the downloadable packs on the GitHub CDN need regenerating.
4. Audio Level Monitor — operator feedback, prevents bad audio (#3)
5. Rooms desktop dashboard — active-rooms overview + Live-tab room dropdown (#19g)
6. Setup Wizard expansion — integrates QR, audio monitor, hardware score (#2)
7. Cross-platform headless server (Linux/Docker)

**Done since 2.0.0:** fresh-install/CUDA delivery fixes, whisper-server crash fix, Default Speaker, STT-template/room-template declutter, **System-wide Dictation (verified)**, **conversation-room translation regression fix (verified)**, **concurrent multi-engine offline translation (v2.2.x)**, **engine-readiness indicators (v2.3.0)**.

Done in the 1.9.x→2.0 line: Structured Logging (v1.8.5), Config refactor (v1.9.0–4), Architecture audit (v1.9.5), legacy removals + per-engine keys (v1.9.6–7), cloud parity/cost/latency (v1.9.8–9), engine expansion (v1.9.10–13), robustness/hardening (v1.9.14–18), per-room translation engines (v1.9.19–21), v2.0.0 milestone.

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

## STT / Translation Improvements — from GLS 2026 field log (2026-06-27)
Analysis of the 2026-06-27 GLS 2026 conference (room yuqzb2, Speechmatics es/ca, Google translation, EdgeTTS). Session was clean — only 2 errors, both benign end-of-session. Prioritised:

- [ ] **P1 — English-pivot for weak target languages (translation quality).** Two Swedish guests abandoned the translation ~9 min in (10:09); at that point the Catalan source was clean and English was excellent, but Swedish was translated **directly** from Catalan (`cat→swe`) — a weak Google pair — while `cat→eng`/`cat→spa` are strong. Fix: for minority/low-resource targets, route **source→English→target** (reusing the already-computed English output) instead of direct. `CloudTranslationBackend.vb:211` currently loops each target directly with no pivot; add pivot logic in `TranslationOrchestrator` + backend. Helps any minority language a guest selects, not just Swedish. Optional: route specific Nordic targets to DeepL (stronger on Swedish).

- [x] **✅ DONE (was P0) — UPSTREAM CONFIG is the PRIMARY fragmentation lever (audio A/B DONE 2026-07-08, IMPLEMENTED — `tools/sm_config_test.py`).** Ran the worst-case Catalan pauser (5 min real audio) through Speechmatics at several `(max_delay, EOU)` settings. **Raising the EOU silence trigger 0.7→1.0-1.5s roughly HALVES mid-phrase fragmentation at the source (27.4%→15.2% function-word cuts) AND improves transcription accuracy** (more context recovers words the baseline lost) — no downstream code, every language. `max_delay` is NOT the fragmentation lever (`4.0/0.7`≈baseline); it mainly helps accuracy/fullness. IMPLEMENTED: `SpeechmaticsEouSilenceMs` default 800→1400; new configurable `SpeechmaticsMaxDelayMs` (was hardcoded 1.0s) default 2500; wired `SpeechmaticsConfig`→`speechmatics.py`; both Advanced UI dials. **CORRECTS Phase 0:** the log-reconstructed "~3.6s indistinguishable pauses" were INFLATED — real audio shows EOU tuning works well, so upstream prevention is far more effective than the log analysis claimed. This demotes ALL the downstream work below (lists/SaT) to *residual-only*. **OPEN RISK (validating with a fast-speaker clip):** defaults are tuned to the WORST pauser; higher EOU may hurt FAST "reader" speakers (fewer boundaries → longer/delayed commits, `maxChars` mid-sentence cuts, up to 8s lag). Evidence: EOU=1.0 gives the SAME benefit as 1.4 (15.2 vs 15.4%), so 1400 was over-tuned. **SHIPPED defaults (2026-07-08): EOU 1000 / max_delay 2000** — both `AppConfig` and `speechmatics.py` (`DEFAULT_MAX_DELAY_S=2.0`) aligned. Layered on top: the **per-speaker EOU auto-tune (Phases 1-3, BUILT)** — starts at the configured baseline, measures p85 inter-word pace, and widens EOU to match each speaker (converges ~1-2 min; validated 0.7→1.0→1.35 for the slow Catalan speaker in logs 20260708). **FIELD-MACHINE STEP:** Jezer's saved config overrides new defaults — set EOU=1000 in Options there.
- [x] **✅ DONE (was P1) — STT fragmentation fixes (Phase 0 analysis DONE — `tools/analyze_grace_sweep.py`). Resolved by the upstream-config win + buffer-to-pause + SaT above.** Analysed **3,871 clause-locks across 4 clean services** (27th full; 21st/28th/5th filtered to ≥11:30 to drop pre-service mic checks). **Course-correction: grace-timeout tuning is the WEAKEST lever** — only 13.7% of clauses are grace-timeouts, and raising graceMs rescues just 3% (1400ms) / 7% (1600) / 11% (1800) of tiny fragments (the rest follow genuine long pauses no timer can bridge). The two real levers are timing-independent:
  - **(a) ~~punctuation-lock over-segmentation via per-language function-word lists~~ — BUILT then DELETED (2026-07-08).** Superseded by SaT (see the shipped entry below). Per the user's call ("never liked those lists, lockPunct was a bandaid too far down the chain, kill them"), `lockPunct` + the function-word merge + `function-words.json` + `FunctionWordProvider` + the `Clause*` lock/minlock/enders/funcwordhold config+UI+descriptor + event 5012 were all removed; the accumulator is now a pure buffer-to-pause merger. Original description kept for history:** 86% of clauses lock on punctuation, and 12.9% of those (430) ended in a dangling function word ("Si Dios no ocupa el.") — `lockPunct=True` trusted Speechmatics' spurious mid-phrase periods and cut sentences mid-phrase → garbage translations. **Prototyped (`tools/prototype_punctlock_fix.py`) then built:** suppress the punctuation-lock when the clause ends on a function word (prepositions/conjunctions/articles — NOT pronouns like "mi/tu", which can end a sentence e.g. the communion words "…de mi."); **extend the grace hold** (new `ClauseFunctionWordHoldMs`=4000, since continuations arrive ~2.9s later, past the 1200ms grace); seam cleanup strips the spurious period + dedupes the repeated seam word ("y la." + "La sangre" → "y la sangre"). **Data-driven, per-language:** the word LISTS live in **`function-words.json`** (app root, keyed by detected-language code es/ca/en/…, `Content`-packaged like `hallucinations.json`), loaded by `FunctionWordProvider` and resolved by each clause's DETECTED language — NO language data hardcoded in VB (an unlisted language simply no-ops). Only the timing dial `SpeechmaticsClauseFunctionWordHoldMs` (=4000, language-agnostic) stays in config. Simulation over 4 services: **~232 mid-phrase cuts recovered, 2 borderline over-merges, 6.3% fewer clauses.** Skipped the cosmetic lowercase-continuation (would break proper/Bible nouns). Only active when clause-hold is ON. **TODO: verify on a live Catalan/Spanish service; add more language lists to function-words.json as needed.**
  - **(b) Tiny standalone fragments.** ~12% of clauses are ≤10-char grace-timeouts ("Summit.", "No?") — too short to reach `minLockChars=12`, so they time out alone. Fix: **length-based merge** — hold sub-N-char clauses and append to the adjacent commit before translating (grace-independent).
  - **(c) Minor — bump baseline `graceMs` 1200→~1400.** Natural mid-thought pauses reach ~p90=1300 ms, so 1200 is slightly tight. Trivial tweak, small benefit.
  - **BUILDING — continuous per-speaker EOU auto-tune (server-side, default ON).** The 2026-07-08 audio A/B proved server EOU is a STRONG lever (halves fragmentation) and its optimum varies by pace (slow ~1.35s, fast ~0.7s) — no fixed value serves both (fast reader BALLOONS/loses text at EOU≥1.0). DESIGN (all validated on 3 real Catalan clips, `tools/sm_*.py`):
    - **Pace detection** — the live-server measures inter-word gaps (Speechmatics returns word timings); p85 gap → EOU bucket (<100ms→700, 100-400→1000, >400→1350). Validated: Andreu p85=646→1350, Chiyana 240→1000, Grazi 30→700 (correct picks). `tools/sm_pace_test.py`.
    - **Per-speaker, NOT per-time** — a rolling TIME window blends speakers and biases to the longest talker (user caught this). Must key by speaker + reset on change.
    - **Speaker-change signal = DIARIZATION** (default automatic). Silence-gap REJECTED — prayers are two people back-to-back with no gap. Diarization NOISE tested clean: 3 single-speaker clips → 1 voice, 0 spurious changes each (`tools/sm_diar_noise.py`). Speaker label is in `results[].alternatives[0].speaker` ("S1"). Manual ActiveSpeaker = optional high-end override (stable identity → per-speaker memory; diarization labels drift so give change-detection only).
    - **Reset triggers** (all clear the pace/speaker state, start fresh): (1) diarization voice change, (2) **host pause** (find the room/translation pause hook), (3) **host manual language change** (from the web-client host tools — a deliberate context switch; and it already restarts the SM session, so a natural hook). All three are "operator/engine says the context changed → don't carry the old speaker's pace over."
    - **Apply mechanism**: reconnect the WS session with the new EOU; audio keeps buffering in the queue during reconnect (no loss). Hysteresis: min ~45s between retunes; only on bucket change.
    - **LOG speaker changes + retunes** (with context) so noise can be audited after a live service — user requirement.
    - **Short speakers (prayers 30-60s)**: not enough data to converge → run on default 1000; diarization stops them inheriting the previous long speaker's extreme EOU.
    - Behind a config toggle, default ON. Speaker profiles remain the manual per-pace override.

- [x] **DONE (2026-07-08) — System-wide SaT sentence segmentation SHIPPED; lockPunct + function-word lists DELETED.** Behind `SpeechmaticsUseSat` (default OFF; ticking it implies Hold & merge). The Speechmatics path is now: **EOU auto-tune (upstream) → hold-and-merge accumulator buffers fragments until a real pause → SaT segments the merged clause → translate each sentence.** DESIGN: on grace-timeout flush the accumulator posts the buffer to live-server `/segment`, which strips sentence-ending punctuation (keeps commas), runs SaT (wtpsplit) to re-decide boundaries, restores a period per sentence, returns the list; each sentence is translated separately. List-free, engine-agnostic — validated it merges Spanish "…ocupa el." cuts AND German separable-verb ("Fenster." + "auf…"), keeps real boundaries + tiny "Vale." separate. FILES: `live-server/sat_segmenter.py` (NEW, lazy-load, auto-discovers tools/sat-libs+sat-cache, front-inserts sat-libs so its transformers/tokenizers win — torch stays embedded, no conflict), `/segment` endpoint + `/start` warm-up in server.py, `LiveStreamRunner.Segment(Async)`, `CloudStreamingSttBackend.Segment`, `SpeechmaticsClauseCoordinator` segment-delegate + SaT-aware thresholds/flush, `ConferenceController` delegate wiring, `CONF_CLAUSE_SAT_SEGMENT=5013`, config (`SpeechmaticsUseSat`/`SatThresholdPercent`=10/`SatModel`) + auto-rendered Options fields. ~8ms/commit, threshold 0.10 default. **MECHANISM validated live** (logs 20260708_1633/1700/1943, event 5013): it splits multi-sentence buffers into single sentences and (eyeballed) doesn't over-split real sentences — which is well-motivated (NLLB prefers single sentences). **But the end-to-end BENEFIT vs SaT-off is NOT A/B'd** — same gap as the vocab: "it does something sensible" ≠ "measurably better output." A clean SaT-off vs on A/B on the same audio is the only way to confirm the quality gain. **DOWNLOAD:** Download-Manager component "SaT Sentence Segmentation" self-installs via embedded Python — `pip install wtpsplit==2.2.1 transformers==5.13.0 tokenizers==0.22.2 --target <app>\sat\sat-libs` (torch/onnxruntime already in python-embed are skipped) + HuggingFace model → `<app>\sat\sat-cache`; no hosted asset, verified end-to-end on-device. **lockPunct + function-word lists DELETED** — accumulator is now pure buffer-to-pause (Grace/MaxChars/MaxMs only); removed `function-words.json`, `FunctionWordProvider.vb`, the `Clause*` lock/minlock/enders/funcwordhold config+UI+descriptor, event 5012, 7 locale keys. Fallback when the SaT model is absent = plain buffer-to-pause (merges fragments, no splitting). **REMAINING: deploy build + SaT download to Jezer.** — NOTE: an earlier same-day gate test wrongly concluded SaT "does NOT clear its gates" by testing a strawman (SaT `split()` on *punctuated* text); the shipped design strips punctuation first at the pause, which is why it works. Original gate findings (still-true latency/threshold facts) below: **(1)** SaT is fast (~8ms/commit) and preserves punctuation — but fed Speechmatics' *spurious* mid-phrase period it **keeps** that bad boundary at every threshold, so `split()` re-segmentation does NOT recover cuts on punctuated text (the bakeoff's ~55% was measured on *stripped* text, not the live reality). **(2)** The only signal that works is `predict_proba` at a **seam** (held commit + continuation joined): spurious cuts score 0.000 incl. German, real boundaries score high — genuinely list-free and engine-agnostic **for the merge decision**. **(3) BUT** SaT cannot make the *hold* decision: from a truncated buffer `"…ocupa el"` it scores the end 1.000 every time (text trivially ends there), so it can't flag incompleteness *before* the continuation arrives. Since spurious continuations arrive ~2.9s later, you must **hold** that long, and deciding *which* commits to hold (vs. flushing immediately for low latency) still needs the cheap incompleteness heuristic = the word list (ca/es) or "hold everything" (~3s latency on every sentence). **SaT can't break that circle → it can't retire the hold trigger, only polish the seam.** (This concern was resolved by the buffer-until-pause design: SaT segments the already-merged clause, so it doesn't need to make the hold decision — the accumulator's grace timeout does.) ~~The endgame for the residual fragmentation.~~ — now shipped.
- [x] ~~**COMMITTED, do LAST — System-wide SaT segmentation → RETIRE the function-word lists.**~~ **(DONE 2026-07-08 — shipped in the entry above; function-word lists deleted.)** Original rationale for history: the endgame for the residual fragmentation. Replace the per-language function-word lists (lever a) with ONE engine-agnostic multilingual sentence-segmentation model (**SaT / wtpsplit**), **toggleable per engine**. WHY: the lists are per-language hand-curation with traps (German separable verbs, sentence-final pronouns, missing words like `durante`); SaT needs **NO lists**, covers all ~50 languages, and is **engine-agnostic** — one downstream layer that works on ANY STT's text (whisper/cloud/Speechmatics), not just the Speechmatics clause accumulator. EVIDENCE (offline, `tools/sat_bakeoff*.py`, `tools/sat-libs`): full-context SaT-3l-sm matched the lists on recovery (~55%) at ~4pts worse precision *for listed languages only*, higher ceiling (70%). The EOU auto-tune halving the problem upstream shrinks the residual SaT handles → the small precision gap matters even less. GATES before shipping (it's a live-path MODEL): (1) **latency** — inference per commit fast enough live; (2) **punctuation restoration** — SaT strips punctuation to re-segment, must restore it for readable subtitles; (3) confirm it doesn't degrade whisper (already segments well). Torch already present in python-embed; ONNX path could drop that weight. SEQUENCE: after the EOU auto-tune ships + is validated live; then retire lists once SaT proven at-least-as-good in a real service. NOT a parked fallback — this is the committed direction; lists are the interim.
  - **✅ RETIRED `lockPunct` + the lists (2026-07-08). KEPT the hold-and-merge accumulator** — the buffering is still needed because SaT segments the *merged* clause. So the outcome differs slightly from the original prediction (which floated removing the accumulator too): the accumulator survives as a pure buffer-to-pause merger (Grace/MaxChars/MaxMs), while `lockPunct` + grace-function-word-suppression + extended hold + the per-language lists are gone. Net: the v1.8.9 boundary-heuristic subsystem is replaced by three cleaner pieces — EOU auto-tune (upstream) + buffer-to-pause (assembly) + SaT (downstream segmentation, all engines).

- [x] **P2 — Auto-reconnect on Speechmatics connection drop ✅ DONE (2026-07-09).** `speechmatics.py` `_run` now reconnects on an unexpected session exception instead of `break`ing: up to `RECONNECT_MAX_ATTEMPTS=8` with linear backoff (`RECONNECT_BACKOFF_S=2s`, capped 15s), reset on a clean session; the audio-capture thread keeps filling `_audio_queue` during the gap so no audio is lost. Intentional stop (`_stop_event`) still exits immediately; only after N consecutive fails does it give up (sets `_thread_error`). A mid-service drop should now self-heal instead of killing the pipeline. **⚠️ NOT YET EXERCISED LIVE:** no post-change log has an actual drop, so the reconnect path is unproven in the wild — the critical `send_audio`-swallows-the-drop bug was caught by CODE REVIEW (it would have made this whole fix a no-op), not by observing a real reconnect. Needs a real drop (or a forced one) to confirm end-to-end.

- [x] **P3 — Biblical custom dictionary (Speechmatics `additional_vocab`) ✅ DONE (2026-07-09), generated on-device via its own Download Manager tab.** `speechmatics.py` `_load_biblical_vocab(lang)` auto-selects by session language and passes `additional_vocab` (list of `{"content":…}` dicts — verified against the real SDK type `Optional[list[dict[str,Any]]]`) to `TranscriptionConfig`; logs the list on pass (`additional_vocab passed to engine: N biblical terms for lang=… (e.g. …)`). Config toggle `SpeechmaticsBiblicalVocab` (default ON) → `/start` `speechmatics_biblical_vocab` → factory; auto-rendered Options toggle. **NOT bundled** — generated on-device from the installed Bibles for **ANY language** (not just es/ca/en): `live-server/vocab/build_vocab.py` scans **every** `Bibles/<iso3>/*.sqlite3` (MyBible `verses` table), maps the dir (ISO 639-3) → session code (ISO 639-1) via the app's canonical `wwwroot/data/language-codes.json` (no hardcoded codes), merges proper-noun candidates across each language's Bibles, and writes `biblical-vocab-<iso1>.json`. Engine `_load_biblical_vocab` loads whatever list matches the session language (silent [] when none). New **"Biblical Vocabulary" Download-Manager tab** (its own tab, not under Bibles): `DependencyManager.GenerateBiblicalVocabAsync` runs the extractor with the lang-codes path; `AnyBibleInstalled`/`BiblicalVocabInstalledCodes` drive the status (button enables when any Bible is present); language names via `LanguageCodeService.GetDisplayNameForCode`. `BiblesRootDir` falls back to the app's `Bibles` folder when the configured path is stale/missing. Verified: generation produces ca/de/en/es (1000 each, incl. German); the list is passed to the engine (session log `additional_vocab passed to engine: 1000 …`); and Speechmatics **accepts** the 1000-entry default-ON vocab (session 20260708_194337 ran clean — the size smoke-test passed). **⚠️ BENEFIT UNPROVEN (honest correction 2026-07-09):** an earlier claim that the vocab "fixed the Elies garble" was WRONG. The `Elies→aliens` garble appears ONLY in WHISPER logs (20260608/09); **NO Speechmatics session — vocab on OR off — has ever produced "aliens"**, so Speechmatics recognises "Elies" natively and the vocab almost certainly did nothing for that name. There is currently **no evidence the vocab improves Speechmatics recognition at all**. To settle it: a vocab-OFF vs ON A/B on the SAME audio, looking at names Speechmatics *actually* garbles (not "Elies"). Until then: **built + running, value unconfirmed.** **REMAINING (follow-on, gated on proving value):** trim `glossary.json` recognition maps; parked cross-language name glossary (Elies=Elías=Elijah).
  - [ ] **BETTER DESIGN (2026-07-09, user idea) — CONTEXT-SCOPED vocab: send names from the BOOK being preached, not the whole Bible.** The whole-Bible 1000-name list misfires because it boosts names from books that aren't in play (rank-798 "Haixum" from Nehemiah replacing common "això" while preaching 1 Kings). A book-scoped list is contextually relevant AND tiny (~20–100 names/book → far below the false-positive threshold). FLOW: (1) **detect the scripture reference** in the live transcript — reuse the EXISTING reference detection that already drives the Bible panel (`BibleService`/`BibleController`, `BookMap` short/long-name→`book_number`); (2) resolve the **book** + the **session language** (→ the right Bible DB via the existing iso3→iso1 mapping); (3) extract names from **ONLY that book** — `build_vocab.py` + a `WHERE book_number = ?` filter on `verses` (same heuristic, one book); (4) send those book-scoped names as `additional_vocab`; (5) when the sermon moves to a **new book**, regenerate + resend (a reconnect, like the EOU retune — book changes are infrequent so the cost is low). WHY BETTER: preaching 1 Kings 17 (Elijah/Zarephath) sends only 1 Kings names (Elies, Sarepta, Acab…) and NOT Nehemiah's Haixum → cross-book false positives disappear, while the genuinely-relevant obscure names (Sarepta) are still boosted. REUSE: existing reference detection, `books`/`verses` schema, `build_vocab.py` extraction, the language map. OPEN: timing (preachers often announce "turn to 1 Kings 17" before reading → detect early, load ahead); per-book cache keyed by (book, language); fallback to a small core list (or nothing) before any reference is detected; supersedes the whole-Bible list (keep that OFF by default). Still gated on the vocab helping at all (the off/on A/B) — but this design is inherently far safer than the whole-Bible dump.

- [x] **P4 — Log noise: end-of-session disconnect logged as ERROR ✅ DONE (2026-07-09).** `ConferenceController.WireBackendLogging` now routes benign connection-lifecycle lines (SSE connection lost / exited unexpectedly / forcibly closed / connection reset|aborted|closed / operation canceled) through new **`CONF_BACKEND_DISCONNECT=5014` (Info)** instead of `CONF_BACKEND_ERROR=5003` (Error), via `IsBenignDisconnect(line)`. Genuine engine errors still land as 5003. **⚠️ NOT YET EXERCISED LIVE:** no post-change log has a disconnect, so the reclassification (5003→5014) hasn't actually run — logic-verified only.


- [x] **DECIDED (2026-07-11/12) — Catalan translation-engine bake-off: NLLB 3.3B int8 stays PRIMARY.** Four instrumented sessions (shadow opinions 4012, identical inputs) settled it: **Speechmatics-inline RETIRED as a translation engine** (broke ca/es code-switching, mangled "man shall not live by bread alone", translations lag commits — 3 sessions of evidence); **raw-commit regime**: DeepL ≥ Google > NLLB (NLLB drops sentences on unsplit multi-sentence commits — its known single-sentence design, which SaT exists to fix) ≫ inline; **production regime (SaT-stitched single sentences, 50/50/50 perfect coverage)**: three-way near-parity — each engine ~1-3 soft errors per 50 (NLLB: "approval" for estima, quoted Mark 1:12 verbatim instead of translating; DeepL: "first comes love then comes marriage" rhyme hallucination, "No dessert."; Google: stiff register). NLLB = parity + free + offline → primary on merit. DeepL (key configured, header auth) + Google stay as always-on shadow auditors; every service now accumulates comparison data automatically. Recurring lesson: the remaining error class (estima→approval, "Papa."→"Pope", rhyme completions) is CONTEXT starvation — the next quality lever is context-aware translation, not engine choice.

- [x] **Speechmatics API deprecation ✅ MIGRATED 2026-07-14 (dual-path): `operating_point` → `model`.** Speechmatics has deprecated the `operating_point` transcription-config property in favour of a new `model` property (same tiers: standard/enhanced, + melia for batch). `live-server/engines/speechmatics.py` still sends `operating_point` (works today) — migrate before they retire it: send `model`, keep `operating_point` fallback for older SDK versions, and rename the config field/descriptor if the UI wording should follow. DONE: speechmatics.py inspects the installed SDK's TranscriptionConfig — passes `model` when the SDK exposes it (tier string carries over), else `operating_point` (current SDKs, incl. python-embed + the unpinned Lite image). Both paths unit-simulated; behaviour on today's SDKs byte-identical. Image :2.7.9.

- [ ] **Considered & shelved (record only):** *Melia* multilingual model (batch-only real-time + no Catalan yet — revisit when both land); *diarization for attribution* (audience doesn't need "who"); *music detection / audio-events* (annotates only, unreliable on sung worship — a manual "pause during music" control is more dependable).

---

# EveryTongue Lite — Staged Conversion Plan (PLANNED 2026-07-13)

**Goal:** stage-by-stage convert the codebase so the online-only stack (Speechmatics STT + Google/DeepL translation) runs as a **local Docker image on any OS** (the "Mac laptop" deployment), and — as a further stage — **hosted online behind a domain**. This absorbs/supersedes the "Headless server" and "Cross-platform" bullets in Future Work below.

**Operating model (DECIDED — write-once so future-us doesn't drift):**
- **Self-hosted first, non-commercial.** Ship software, not a service (the Jellyfin model). Distribution at scale = federated: each org runs its own Docker instance; volunteers get a URL — nothing to install on phones.
- **BYO API keys** — each deployment brings its own Speechmatics/Google/DeepL keys (already the config model: `SttApiKeys`/`TranslationApiKeys`). Small churches likely fit inside vendor free tiers.
- **Explicitly NOT a public multi-tenant SaaS** — no accounts/billing/SLA. At most, privately host one instance for a known congregation.
- **Anti-fork rule:** each capability lives in exactly one place; Lite is a subset *build* of a shared `EveryTongue.Core`, never a parallel implementation. The danger zone is the Python-side components (Speechmatics client, SaT) — either both editions share the slim sidecar (v1) or both migrate to .NET together (later, optional).

**Competitive landscape (2026-07, recorded for context):** the category is now crowded — Wordly, Glossa, Aurelo (closest twin: desktop app + QR browser listeners + TTS, Mac support, free 2h/mo then €49/mo), Maestra, Sunflower AI, OneAccord, LiveVoice, Boostlingo. None do simultaneous multi-target-language translation (Aurelo: one pair per session, "coming soon"), none work offline, all are subscriptions. EveryTongue's lane: free, self-hosted, offline-capable, multi-language, Bible-integrated. Aurelo's free tier is a legitimate stopgap for a single-language congregation on a Mac. Sunflower's "upload the run sheet in advance" validates the context-priming direction (book-scoped vocab, context-aware translation).

**Verified feasibility facts (explored 2026-07-13):** audio capture is Python `sounddevice` (portable), the Speechmatics engine reads a swappable `_audio_queue` (16kHz mono int16, 3200-byte/100ms frames), browser→server binary audio already exists (conversation PTT), only 2 WinForms couplings in server-destined code (`ConferenceController.ownerForm`, `AppLogger.PromptDownloadManager` MessageBox), `CertificateService` is cross-platform, no `My.*` usage anywhere, QR endpoint is drawing-free (`PngByteQRCode`), NAudio touches exactly one server-reachable file (`TtsAudioOutput.vb`).

## Stages (each gated, each valuable standalone; full detail in the 2026-07-13 plan)

- [x] **Stage 1 — Web-Mic Broadcast. ✅ verified on the desktop app AND through the Docker container (2026-07-14).** Implementation: engine `feed()`/`web_feed_recent()` + `/audio-in` WS (speechmatics.py, server.py), `WebMicRouter` (hub→live-server forwarder, Services/Rooms/), `broadcastStart/Stop` + binary routing in SubtitleHub (host-gated, takeover, ETMC header), `AudioSource`/`WebMicRaw` on ConferenceTemplate+Room+SttSessionConfig+LiveStreamRunner (/start `audio_source`), template-manager device combo gained two web-mic sentinel entries (-2 processed / -3 raw), web client Broadcast button + level meter in host panel + `mic-worklet.js` (AudioWorklet 48k→16k int16 100ms frames), auto-resume on WS reconnect, events 5016/5017. Deviation: no distinct "waiting for microphone" banner — instead `/health` `capturing` is honest for web sessions (false until frames flow), so the EXISTING preparing/ready banners and mic-gating are correct automatically; a dedicated message is post-field-test polish. The mic becomes a browser role: host taps "Broadcast" and their device streams continuous 16kHz PCM (AudioWorklet, NOT MediaRecorder) over the existing `/ws` socket → hub routes to live-server `/audio-in` → fills the engine's `_audio_queue`. `audio_source: local|web` per template (local stays default — zero regression). Host-gated (`broadcastStart` requires `room.HostClientId`), one broadcaster per room with takeover, versioned frame header (Opus later), level meter, EC/NS/AGC toggle (off for PA feeds), "waiting for microphone" readiness state, honest `capturing` semantics (frames flowing, not session started). Standalone value: roving mic for the Windows app. **Gate:** live service with web-mic ≈ local-mic baseline on the 5015 scoreboard.
- [ ] **Stage 2 — Headless extraction, `EveryTongue.Core`. ⚙ BUILT + published 2026-07-13 — gate remaining: run a full service on the desktop app to confirm zero regression.** Done: 2a seams (PromptDownloadManager→ConfirmPrompt; NAudio behind ITtsAudioSink + ServerOptions.TtsSinkFactory/AudioDeviceProvider; ConferenceController ownerForm→marshal Action(Of Action); ServerController notify→LogSeverity + ClipboardSetter callback + netsh behind OperatingSystem.IsWindows() guard [kept in Core rather than extracted — same headless-safety, less churn]); 2b split (EveryTongue.Core net8.0 classlib: Server/, Pipeline/, Models/, Services/ minus Input/Testing/TtsAudioOutput/DesktopTtsPlayer/HardwareScanner, ConferenceController+ServerController+SentenceBuffer, wwwroot + python-sidecar Content + embedded en.json + IVT both heads; head keeps NAudio/WebView2/System.Management/Help/test-data/logos/checksums target). Extra reverse-dependency found+fixed: ConversationAudioHandler used TranslateController.SplitIntoLines → extracted to Core `Services/Translation/SentenceSplit.vb` (controller delegates). UpdateChecker → GetEntryAssembly. Publish verified: Core.dll ships, 41 checksummed files unchanged, embedded en.json in Core.dll, wwwroot/locales/python files all present. 2a in-place decoupling (each step builds): `PromptDownloadManager` → settable `ConfirmPrompt` callback; NAudio behind `ITtsAudioSink` + `ServerOptions.TtsSinkFactory`/`AudioDeviceProvider` (sole consumer `TtsAudioOutput.vb` stays in head); `ConferenceController.ownerForm` → injected `marshal As Action(Of Action)`; netsh/notify behind callbacks. 2b split: new Core vbproj (`Microsoft.NET.Sdk`, net8.0, `RootNamespace EveryTongue`, `FrameworkReference Microsoft.AspNetCore.App`, pkgs AWSSDK.Translate/Sqlite/QRCoder/USFMToolsSharp, `InternalsVisibleTo` both heads); one big `git mv` of Server/Pipeline/Models/Services (except Input/, TtsAudioOutput+DesktopTtsPlayer, HardwareScanner, Testing/) + wwwroot + Content items + **the embedded `en.json`** (`LanguagePackService` uses `GetExecutingAssembly`); fix `UpdateChecker` → `GetEntryAssembly`; verify transitive content copy in Debug+Publish; WebView2 pkg looks dead — verify, remove. **Gate:** WinForms app byte-identical behavior, publish output unchanged.
- [ ] **Stage 3 — `EveryTongue.Lite` console head + slimming. ⚙ BUILT + smoke-tested 2026-07-13 — gate remaining: a full end-to-end Lite service (web-mic room + phones + cloud translation).** Done: `EveryTongue.Lite/` console project (LiteProgram.vb — config, console+file logging, ServerController with headless callback defaults, ConferenceController with inline marshal, conversation-room wiring with cloud-only translation acquire, Ctrl+C graceful shutdown incl. GlobalShutdown+session summary); `EVERYTONGUE_CONFIG_DIR` override in ConfigManager (+`ConfigDirectory` property) AND CertificateService (cert lives beside config.json — survives container restarts); startup validation (offline STT → warning, offline/keyless translation → hard error, companion-key fallback respected via ResolveTranslationApiKey); VAD gate reordered in server.py (+vad/__init__ guards VadPipeline import) so streaming engines run **without torch**; `requirements-lite.txt`. Smoke test passed: Kestrel on alt port, cert generated into override dir, google/deepl keys configured, wwwroot+live-server+locales all flowed transitively into Lite's output. ⚠ Noted for Stage 5: something probes bare `python` at startup ("Python was not found" — cosmetic on Windows); PythonSidecarHost must fall back to system `python3` when `python-embed` is absent (required for Docker anyway). Console bootstrap (config + Kestrel + handler wiring, no dictation/tray); `EVERYTONGUE_CONFIG_DIR` env override in ConfigManager; **reorder the live-server VAD gate** (server.py:850 sits above the streaming branch at :882) so streaming engines don't require torch; `requirements-lite.txt` (no torch); online-only config validation. **Gate:** Lite runs a full service on Windows.
- [ ] **Stage 4 — Web settings page. ⚙ BUILT + API-tested 2026-07-13 — gate remaining: browser walk-through on a phone.** Done: `EndpointRegistration.Settings.vb` — PIN-gated `GET/POST /api/settings` (engine selection from both registries, write-only API keys [GET returns keySet booleans only], admin PIN change applied to LIVE ServerOptions immediately, key changes re-pushed into running cloud backends via ConfigureCloudApiKeys — no restart) + `GET /api/settings/logtail` (last 200 session-log lines, shared-read). Heads wire `SettingsConfigProvider`/`SettingsSaveHandler` (desktop: plain ConfigManager.Save — deliberately NOT SaveUiToConfig; Lite: same). Web UI: Server Settings overlay (engine selects, per-engine key fields with configured-state placeholders, PIN field, log viewer) reachable from the admin panel and from the language-picker Administrator link. Fresh-install reality: AppConfig defaults AdminPin="1234", so bootstrap = log in with 1234 → change it (console NOTE says so); Lite no longer exits on incomplete config (would brick browser-based setup) — it starts with loud guidance instead. API flow verified end-to-end on a blank config: engines switched, keys persisted to config.json, PIN rotated with old PIN 403ing immediately. Stage 6 note: default-1234 must be rejected/forced-changed in hardening. PIN-gated: API keys entry, source language, template basics, engine status, log tail. **Gate:** blank machine → full setup from a browser only.
- [x] **Stage 5 — Docker image. ✅ GATE PASSED 2026-07-14 on the translation PC: browser mic → container (ghcr 2.7.1) → Speechmatics → Google → room, incl. clause merge, grace auto-tune (1400→3000ms live), and web-mic forwarder auto-reconnect. Root-caused en route: python sidecars die on .NET pipes on Linux → sh file-redirect spawn (v2.7.1). Follow-ups: container banner prints internal IP (cosmetic), SaT not in lite image (no-op fallback), biblical-vocab warning noise. Originally: BUILT + container-tested 2026-07-13 (1.18GB, linux/amd64) — gate remaining: full service through the container (web-mic room + phones + real Speechmatics session) + arm64 build for the Mac.** Done: `Dockerfile` (multi-stage: sdk publish → aspnet:8.0 + python3 venv `/opt/etpy` on requirements-lite + libportaudio2 + ffmpeg; `EVERYTONGUE_CONFIG_DIR=/config` volume; EXPOSE 5080/5081), `.dockerignore`, `docker-compose.yml` example; `ProcessHelper.FindPython` probes `python3` then `python` (also silences the Windows Store-alias console noise); `ConversationAudioHandler` falls back to PATH `ffmpeg` on non-Windows; requirements-lite added as Core Content; stale "/start will be blocked" log message corrected. Verified IN the container: Kestrel+HTTPS with cert generated onto /config, lobby+app.js served, /api/settings answered (PIN 1234), all lite python deps import, vad package degrades exactly as designed (VadPipeline=None, engines registry loads torch-free), live-server boots and /health responds. 2026-07-14: v2.7.0 committed (d79d70e) and image PUSHED to `ghcr.io/leapfreak/everytongue-lite` (:2.7.0 + :latest, **linux/amd64 + linux/arm64** — Mac-ready). Package is PRIVATE by default — make public via github.com → profile → Packages → everytongue-lite → Package settings → Change visibility (one-time, UI-only), or `docker login ghcr.io` on each pulling machine. Remaining: setup guide page + the full-service container gate. Multi-stage Dockerfile (aspnet:8.0 + python3-slim + requirements-lite), multi-arch amd64+arm64, `/config` volume, ghcr.io publish, compose example, one-page federated setup guide. **Gate:** container serves a full service incl. web-mic on Docker Desktop (= the Linux port test). Then the Mac pilot: Docker Desktop + browser + USB mic.
- [ ] **Stage 6 — Online through a domain (~1 wk).** Caddy reverse-proxy sidecar for automatic Let's Encrypt (no ACME in-app; forwarded-headers flag in Kestrel); proportionate hardening: high-entropy hosting codes, rate limiting on code/PIN endpoints, mandatory strong admin PIN in Lite, per-room usage counters + optional caps (`TranslationUsageTracker`), room creation requires hosting code. VPS deployment guide. **Gate:** real domain, green-lock phones, zero cert prompts, abuse probes rate-limited.

**Container catch-up (2026-07-14, v2.7.1→2.7.5):** sidecar-death root cause = python dies on .NET pipes on Linux → sh file-redirect spawn (v2.7.1); honest container banner + `EVERYTONGUE_PUBLIC_HOST` (v2.7.2); phone-reachable QR/share links via `PublicHostFor` — loopback swapped for LAN IP on native hosts, env var wins in containers (v2.7.3); **SaT restored in the image** (CPU torch + wtpsplit pins, model → `HF_HOME=/config/sat-cache` on the volume) + **Bibles via volume** (`<config>/Bibles` overrides copied Windows path) (v2.7.4, image 1.18→3.59GB unpacked); Dockerfile layer order fixed so releases re-ship MBs not the torch stack (image :2.7.5).

## Lite follow-up backlog (agreed 2026-07-14, build order as listed)

- [x] **Lobby tiers / CreatorCode ✅ BUILT + container-verified 2026-07-14 (v2.7.8).** Three ranks: guests join rooms only; volunteers with the code create them; admin PIN owns settings. `AppConfig.CreatorCode` ("" = open, legacy default) → ServerOptions → enforced ON THE ENDPOINT (`POST /api/rooms` 403s without it, not just hidden UI); `GET /api/creator/verify`; `creatorCodeRequired` on /api/config; settings page field (live-applied like the PIN; "-" clears). Lobby: creation tools collapse behind a "Host tools" link + code prompt (session-remembered). Conference hosting keeps its per-template codes. Prerequisite for the dictation page; also closes the pre-existing hole where any lobby visitor could create conversation rooms on the operator's engine/keys. Verified: open→201, code set live via settings, no-code→403, code→201.

- [x] **Raw JSON config editor ✅ BUILT + API-verified 2026-07-16 (v2.7.19) — gate remaining: browser walk-through on a phone.** PIN-gated `GET/POST /api/settings/rawconfig` (EndpointRegistration.Settings.vb): GET serializes the LIVE AppConfig with ConfigManager's exact serializer (new `ConfigManager.SerializerOptions` shared property — byte-identical round-trip); POST deserializes to AppConfig (400 with the parser's precise message on bad JSON), serialize-diffs every top-level property, reflection-copies the parsed config onto the live instance (so later saves never clobber raw edits), hot-applies engines/keys/PIN/creator-code (live ServerOptions + ConfigureCloudApiKeys/ConfigureCloudTtsKeys), persists via SettingsSaveHandler, responds `{ok, changed:[names], needsRestart, pinCleared}` (pinCleared warns when an edit blanked the admin PIN → settings open). Web: "Advanced: edit raw config" toggle in the settings overlay (textarea + client-side JSON.parse pre-validation), ES5. New `web.setRaw*` keys in en/es/ca. **Verified live on Lite**: 403 wrong PIN, 400 invalid JSON with line info, valid edit → changed-list + needsRestart + live-applied + persisted to config.json on the volume.
- [x] **One-command installer ✅ 2026-07-15 — `get-lite.sh` (macOS/Linux) + `get-lite.ps1` (Windows).** `curl … | sh` / `irm … | iex` from the repo raw URL: checks Docker present+running (friendly errors), auto-detects the LAN IP for EVERYTONGUE_PUBLIC_HOST (kills the `<your-lan-ip>` substitution that broke a real deployment), creates `~/everytongue-lite`, pulls latest, replaces the container (`--restart unless-stopped`), prints the exact URLs + first-run steps. IDEMPOTENT — the same command is the updater. Tested end-to-end on Windows (fresh install → server up with ca locale; re-run → clean replace); sh path syntax-checked. README quick-start now leads with the one-liners (manual docker run collapsed into a details block). PowerShell gotcha recorded in-script: `$ErrorActionPreference=Stop` turns docker's stderr notices into aborts — use exit-code checks. Docker Desktop install itself stays a manual once-off; true 1-click (signed native app, $99 notarization) rejected.
- [x] **CONVERSATION ROOMS ON LITE ✅ REAL FIX 2026-07-14 (v2.7.12) — Speechmatics serves one-shot /transcribe.** Conversation-room PTT does one-shot POST /transcribe (ConversationAudioHandler → live-server), which previously needed an offline whisper engine — Lite ships none, so v2.7.11 only *guarded* (hid Create Conversation + 409). v2.7.12 implements option (b): `engines/speechmatics.py` `_transcribe_speechmatics()` — a short-lived RT session per utterance via the SDK's `AsyncClient.transcribe(BytesIO)` convenience (own thread + event loop; ADD_TRANSCRIPT collected; model/operating_point dual-path; billed by audio duration like any session), registered as `transcribe_fn`. Plumbing: /transcribe's silero-VAD gate relaxed to local engines only (`engines.requires_model`); `soundfile` added to requirements-lite; `/load-model` accepts `stt_api_key` and ConversationAudioHandler's cloud branch POSTs it there (the conversation path never calls /start, so the key had no delivery route). Capability: `SttBackendRegistry.Entry.SupportsOneShotTranscribe` (true for speechmatics) → `TranscribeCapable()` = offline+model OR online one-shot engine + key, so the lobby guard now passes on Lite with a Speechmatics key and still blocks truly engineless servers. **Container-verified end-to-end**: fresh Lite container, /api/config conversationRooms:true, room created, simulated PTT over /ws → readiness preparing→ready, "API key delivered (OK)", commit with correct transcript returned. (c) CPU whisper in the image remains on-demand.
- [x] **Localization sweep ✅ 2026-07-14 (v2.7.13–v2.7.15).** Lobby fully localized (data-i18n + /api/locale fetch, ~40 keys); app.js/index.html stragglers; server "Guest" sentinel mapped client-side (guestLabel); desktop dialogs (shortcuts table, FormLogConfig/FormLanguageChooser/FormQrCode, FilterEditor captions). ~80 new web.* + 28 desktop keys — **English-only so far: translate to other locale files when ready.** Deliberately NOT localized: FormTranslationBenchmark (~50 strings, operator-only) and FormLanguagePicker (first-run, pre-locale).
- [x] **Engine language-capability structure ✅ 2026-07-14 (v2.7.15).** `SttBackendRegistry.Entry.SupportedLanguages` (engine-native codes; Nothing = whisper column of language-codes.json) + `GET /api/stt-languages` (active engine, live-config aware, native names). Speechmatics carries its authoritative 55-language RT list. Host-panel Speaker Language + lobby dictation dropdowns populate from it (hardcoded lists = offline fallback only) — fixes the 13-vs-8 language drift. Translation-engine support already lives in language-codes.json vendor columns (not yet wired to UI filtering). TODO on demand: declared lists for deepgram/gladia/azure-speech; kill google.py's 49-entry whisper→BCP-47 duplicate (or add a consistency test).
- [x] **Bible downloads from web settings ✅ BUILT + verified end-to-end 2026-07-16 (v2.7.19) — gate remaining: browser walk-through on a phone.** Catalog-fetch + download/convert orchestration MOVED to Core `Services/Bible/BibleDownloadService.vb` (module: `GetCatalogAsync` with the same 24h translations.csv cache, `LoadCachedCatalog`, `GetInstalledIds`, `DownloadAndConvertAsync` zip→UsfmConverter→VerifyDatabase with stage callback); FormDownloadManager now delegates (CSV parser/HttpClient/download loop deleted from the head — anti-fork rule; bonus: one failed Bible no longer aborts the batch). PIN-gated `GET /api/settings/bibles` (catalog 1,291 redistributable entries + installed flags + per-id download states; falls back to stale cache when eBible is unreachable) + `POST /api/settings/bibles/download` (background task, idempotent re-POST, client polls the GET). `IBibleService.BiblesDirectory` exposes the scanned dir so download target == rescan target; `bible.RescanTranslations()` on completion means the phone Bible panel sees it with NO restart. LiteProgram now ALWAYS creates + uses `<config>/Bibles` (downloads land on the volume, never in the ephemeral container layer). Web: "Bibles (download)" settings section — search (min 2 chars, top-50 + "+N more"), installed ✓, per-row stage while polling; `web.setBibles*` keys en/es/ca. Copyrighted translations (BCI…) stay manual-copy — catalog lists redistributable only. **Verified live on Lite**: spablm (6MB, full Bible) downloaded+converted+verified in <5s, landed in `<config>/Bibles/spa/`, `/bible/translations` + FTS search served it immediately.
- [x] **Web template editor ✅ BUILT + API-verified 2026-07-16 (v2.7.19) — gate remaining: browser walk-through on a phone + a real hosted service from a web-created template.** Conference-template CRUD from the browser: PIN-gated `GET/POST /api/settings/templates` + `POST /api/settings/templates/delete` (EndpointRegistration.Settings.vb). Fields: name, hosting code (both required), source language, STT engine, translation engine ("" = server default — resolver already falls back; SttBackendKey deliberately always stored CONCRETE because the room-start path does NOT treat "" as default), audio source local/web + WebMicRaw, visibility, offered languages. Offered languages reuse the desktop's 1:1 pattern: a DisplayTemplate with Id = template Id, upserted/cleared/deleted alongside (colours preserved when clearing). Saves via SettingsSaveHandler + `TemplateStore.SyncFromConfig` so the lobby list updates instantly. Web UI: "Conference templates" settings section — list with edit/delete, form with engine selects from the settings payload + source-language select from `/api/stt-languages`; `web.setTpls*` keys en/es/ca. **Fixed en route: `TemplateLibraryStore` hardcoded %APPDATA% and ignored `EVERYTONGUE_CONFIG_DIR`** — template-library JSON (display templates, 1:1 STT templates…) was silently EPHEMERAL in the container; now routed through `ConfigManager.ConfigDirectory` → `<config>/templates/` on the volume (desktop path unchanged). **Verified live on Lite**: create→list→update→delete all persist to config.json across restart; lobby `GET /api/templates` shows the rename immediately; `POST /api/rooms/from-template` 403s the wrong hosting code and starts a real conference room (201 + hostToken) from the web-created template. Engine tuning templates / speakers / filter sets stay desktop-only (deliberate scope).
- [x] **Web dictation page ⚙ BUILT + container-smoke-tested 2026-07-14 (v2.7.10) — gate remaining: live end-to-end (talk → text in the editor).** Browser mic → web-mic pipeline → transcribed (optionally translated) text accumulates in an editable textarea + Copy button (a private "dictation room" rendering commits into an editor). A browser CANNOT type into other apps (sandbox) — desktop dictation (hotkeys/SendInput) stays Windows-head-only and is untouched by the split; this page is the portable 90% substitute. **Engine decision (2026-07-14): uses the CONFIGURED engine (Speechmatics) like rooms — no whisper dependency.** Whisper-in-container assessed and rejected: Vulkan doesn't work in Docker Desktop VMs (WSL2's Vulkan is an experimental Mesa layer; macOS has zero GPU passthrough); CUDA-in-container only serves NVIDIA hosts that could run the full desktop app anyway; CPU whisper.cpp IS feasible (torch already in image for SaT — add silero-vad + compile whisper.cpp CPU, model on /config volume, adequate for dictation cadence) but is a deliberate scope decision (first crack in "Lite is online-only") — revisit only if API-free dictation becomes a real need. IMPLEMENTATION: RoomType.Dictation (private, creator-code-gated via POST /api/rooms type=dictation + sourceLang); ConferenceController.HandleDictationRoomCreated synthesizes a template (AudioSource=web, streaming-capable engine — global engine when SidecarMode=online else speechmatics fallback [verified firing in container]) into the shared StartRoomBackend (extracted from HandleConferenceRoomCreated); DictationRoomCreatedHandler wired in WireEndpointHandlers (both heads). View: room type=dictation renders an editor (textarea) — commits append via addCommitted branch; Broadcast Mic reuses the whole web-mic engine (hcBroadcast/hcBcMeter ids); Copy/Clear/Done (Done stops broadcast + closes room). Translation = normal per-client pipeline (pick a language on the picker, or No translation for raw transcript). Lobby: Dictation section (speaking-language select + Start) inside creator-tools, so the CreatorCode gate covers it.

- [x] **v2.7.19 review + validation pass ✅ 2026-07-16.** 8-angle code review of the whole v2.7.19 diff, fixes applied: **(1) rawconfig POST now syncs `TemplateStore`** (raw edits to conferenceTemplates were invisible to the lobby until restart — real bug); **(2) LiteProgram no longer clobbers a custom ABSOLUTE `BiblesDirectory`** that exists (settable via the raw editor; relative defaults + dead Windows paths still get the volume override); **(3) unknown engine keys in template POST → 400** (was silently substituting the server default) and engine validation moved BEFORE template creation (a 400 used to leave a half-built entry in the list); **(4) deep fix: `ConferenceController.StartRoomBackend` treats empty `SttBackendKey` as the server default**, mirroring `TranslationBackendKey` (the old `If()` only caught Nothing, "" slipped through as an engine key); **(5) new lightweight `GET /api/settings/bibles/status`** — the 2.5s progress poll no longer re-parses the 1MB catalog CSV + rescans the Bibles tree + ships 171KB JSON per tick (client does one full refresh when downloads finish); **(6) catalog cache read/write hardened** (exists/mtime race, read-only dir no longer fails the fetch); plus dedup cleanups (shared `JsonStr` body helper ×5, one HTML-escape helper in app.js, pattern-scoped `GetInstalledIds`). **Round 2 (adversarial re-review of the fixes themselves + concurrency/lifecycle lens), 5 more fixed:** (7) partial template updates no longer WIPE previously-set engine keys — field missing from the body = keep current, present-but-empty = explicit (raw-API PATCH semantics; the web UI always sends both); (8) `_templatesLock` around `cfg.ConferenceTemplates` mutations/enumeration/`SyncFromConfig` — two concurrent admin requests could throw "collection was modified"; (9) `ConfigManager.Save` now lock + write-temp-then-atomic-move — web request threads and the desktop UI thread could tear config.json (pre-existing, made reachable by the new endpoints; crash mid-write now leaves the previous good file); (10) `SqliteConnection.ClearAllPools()` before a retry overwrites an existing Bible db (pooled connections hold the file open on Windows); (11) orphaned Bible-progress interval when the overlay closed via the save path; serialize-compare failures now conservatively mark the property changed so the save still fires. Cleared as safe on inspection: WinForms stage-callback thread context (continuations resume on the UI SynchronizationContext), TemplateLibraryStore locking, rawconfig list-swap vs live rooms (rooms keep old instances by design), usage-tracker path lifecycle, AppLogger init order. **Validation harness: `tools/validate-lite-settings.js`** — 30 checks against a fresh Lite instance (PIN gates ×5, raw config incl. TemplateStore-sync + pinCleared bootstrap flow + PIN rotation, template CRUD incl. 400/404 paths + partial-update key survival + 12-way concurrent ops + hosting-code gate + real room start, Bible catalog/download/status-poll/live-serve). **30/30 PASS 2026-07-16.** Re-run any time: fresh `EVERYTONGUE_CONFIG_DIR` + Lite on :5080 + `node tools/validate-lite-settings.js`.
- [x] **%APPDATA% hardcode sweep ✅ 2026-07-16 (v2.7.19, follow-on from the TemplateLibraryStore find).** Everything stateful now routes through `ConfigManager.ConfigDirectory` (honours `EVERYTONGUE_CONFIG_DIR` → the /config volume; identical %APPDATA% path on desktop): `TranslationUsageTracker` (monthly billable-character counters were silently RESET on every container update), `DependencyManager.ReleaseCachePath` (GitHub rate-limit cache exists to survive restarts), `FormFilterSets.SetFolder` + `FormTranslationBenchmark` bench CSVs (consistency — filter files must move with filter-sets.json). Deliberately NOT moved: `TtsCache` (wiped on every server start by design — volume churn for nothing) and Program.vb's single-instance lock file (desktop-only, LocalApplicationData is correct for a lock).

## IA restructure — three-page navigation (BUILT 2026-07-17, v2.8.0 — 44/44 harness checks; gate remaining: phone walk-through of all three pages + a real service via the permanent QR)

**Built as agreed below.** Implementation notes: **admin.html + js/admin.js** (ES6 like lobby.js, browser-locale auto-detect, PIN login with bootstrap-open, cards: Live remote [desktop head only, hidden on Lite], server settings, template editor incl. permanent-QR button, Bibles, raw config, log tail); **app.js shed ~700 lines** (settings overlay + template/bible/raw sections + picker PIN login + Live remote panel all OUT — picker's Administrator link now navigates to /admin.html, host panel gained an Admin button [new tab]); **lobby fully gated** (whole page incl. room list behind the creator code; `GET /api/rooms` + `GET /api/templates` gated server-side via `?code=` [admin PIN also passes]; creatorCode now localStorage with sessionStorage migration; 403 self-heals to the lock screen); **permanent template QR** (`/api/templates/{id}/qr` encodes `?join={tpl}`, `/api/templates/{id}/active-room` resolver newest-wins, app.js waiting overlay polls 5s and auto-joins, `joinTpl` remembered so roomEnded re-resolves — mid-service restart heals; lobby post-create overlay shows the PERMANENT QR for conference rooms with a "print this" hint; admin template editor has a QR button per template); **guest home** = re-open the language picker (volunteers with the device code go to the lobby; roomEnded routes per tier); **auth rate limiter** (per-IP, 10 wrong non-empty credentials / 5 min, shared across admin PIN + creator code + hosting codes + claim-host; empty probes never count); **pre-existing hole closed: `/api/control` mutations (start/stop/restart/clear/tune) now require the admin PIN server-side** — the UI had always hidden them but the endpoint was open (anyone on the LAN could stop a live service); status stays open. Banners: LiteProgram + get-lite.sh/.ps1 print the /admin.html URL. New `web.adm*`/`web.join*`/`web.hostAdmin`/`web.lbQrPermanent` keys in en/es/ca. Known follow-up: in-room admin-PIN host-claim lost its PIN source (picker login removed) — hostToken recovery unaffected; add a PIN prompt to claim-host if field use ever needs it.

### Original agreement (2026-07-17):

**Problem:** the client is a rabbit warren — admin enters through the language-picker page, volunteers and guests share the lobby, and tier boundaries are implicit. **Agreed model — one page per tier:**

- **`/admin.html` (Admin).** Dedicated URL typed into the address bar; UI language auto-detected from the browser like the rest of the client (`/api/locale?lang=` — no language step); PIN prompt; then ALL config: engines/keys, templates, Bibles, raw config, log tail, creator/host codes. The settings overlay MOVES out of app.js into this page; the Administrator link on the language picker and the in-room admin overlay are REMOVED. Mid-service access: an **Admin button on the host menu** opens /admin.html (PIN required as usual).
- **`/lobby.html` (Volunteer).** Gated UP-FRONT by the volunteer (creator) code — nothing renders without it, and `GET /api/rooms` (the list) becomes code-gated server-side too. Creating a conference room grants the existing per-room **host** designation (hostToken/host panel — already built, this names it). Volunteer token: move creatorCode from sessionStorage to localStorage ("volunteer on this device" persists); home button for token-holders → lobby.
- **Guests.** No landing page — they arrive ONLY via room-scoped QR/links (`index.html?room=…`). Room entry keeps the language picker. No home button; in its place a "change language" action (re-opens the picker). `GET /api/rooms/{id}` and `/qr` stay open (guests hold the id). The BARE URL (no ?room) keeps serving the legacy non-room live caption view — that's still the desktop Live-workspace flow and the fallback guest experience.
- **"Public" room visibility semantics change:** listed = listed to VOLUNTEERS in the gated lobby (guests can't browse rooms at all).

**QR audit (2026-07-17): already room-scoped.** `/api/rooms/{id}/qr` encodes `index.html?room={id}` (with the PublicHostFor LAN-IP fix), used by both the lobby's post-create overlay and the in-room host panel — guests scanning any room QR land directly in the room. Desktop FormQrCode's bare-URL QR remains the legacy-live-mode handout.

**Amendments (2026-07-17, user):**
- **The ENTRY language picker is a RIGHT, never a privilege.** This means the room-entry "Choose your language" page (each guest's own OUTPUT language) — every tier gets it on entering any room, and can RE-OPEN it any time from inside the room (the guest home-button replacement). No future tier work may gate it. NOT to be confused with the host menu's "Speaker Language" (the room's STT INPUT language) — that one retunes the engine for everyone and correctly stays host-only.
- **Permanent guest QR = TEMPLATE pointer, not room instance.** A `?room={id}` QR dies with each week's room — silly to rescan weekly for the same service. New stable join link `index.html?join={templateId}` (+ `/api/templates/{id}/qr` and a resolver endpoint): server resolves template → currently-active room born from it (newest wins) and drops the guest straight in. **No room live yet → friendly waiting page that polls and auto-joins the moment the host starts the service** (the 9:45-scan-for-the-10:00-service case). The church prints/laminates ONE QR forever; per-instance QRs remain for ad-hoc conversation rooms. Surface the permanent QR in the web template editor + the lobby post-create overlay, labelled as the one to print. This is also the guest recovery path that replaces lobby browsing ("the laminated QR always works").

**Security pulled forward from Stage 6 (do in the SAME change):** rate limiting on PIN/code endpoints + mandatory strong PIN (reject default 1234 once internet-facing hardening lands) — a well-known /admin URL + 4-digit PIN invites brute force. **Bootstrap preserved:** fresh install with default/no PIN → /admin opens and forces setting one; console banner + get-lite installers print the /admin URL. **Redirects:** old entry points (picker Administrator link) point at /admin.html rather than 404ing; existing printed room QRs keep working unchanged.

**Scope:** days — new admin.html (extract the settings overlay from ES5 app.js), lobby gate, home-button/tier logic, endpoint gating, rate limiter. Client-side tokens remain UX only; server gates stay authoritative.

**Deliberately out of scope:** .NET migration of Speechmatics/SaT engines (later, both-editions-or-neither), public SaaS/accounts/billing, offline engines in Lite, native macOS binary (Gatekeeper/notarization — Docker covers the Mac), Opus web-mic compression (header reserves room), ONNX-slimmed SaT (would return the image to ~1.2GB if size ever matters — the layering fix already makes UPDATES tiny).

---

## Future Work (not scheduled)
- **Headless server / Windows Service mode** — ➡ **superseded by EveryTongue Lite Stages 2-4 above.** Original: run EveryTongue as a Windows service (no GUI, auto-start with OS). The desktop app becomes optional — the server hosts rooms, engines, and the web client independently. Remove/deprecate the WebView2 viewer panel (redundant now that rooms + phone web client handle everything). Operator controls (start/stop engines, view logs) move to a web-based admin dashboard served by Kestrel. Install/uninstall service via CLI or installer option.
- **Cross-platform (Linux / macOS)** — ➡ **superseded by EveryTongue Lite Stages 2-5 above** (Docker covers Linux + macOS without a native port). Original analysis kept for reference. Once the WinForms dependency is removed:
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
