# Architecture Audit — 2026-06-13 (post v1.9.4)

> Three-lens audit against the project's own laws (CONFIG_CHANGES.md engine
> independence, CLAUDE.md code-quality rules): engine independence, layering/
> duplication, correctness practices. Items checked off as they are fixed.

## P0 — Stability (crash / corruption risk)

- [ ] **A1. Async Sub fire-and-forget without Try/Catch** — `ConferenceController`:
  `TranslateAndBroadcastForRoomAsync`, `HandleTranslatedCommitAsync`,
  `BroadcastLockedClauseAsync`, `TranslateAndBroadcastBufferedAsync`. Backend
  commit events fire on the sidecar tail thread (no sync context) — an
  unhandled exception in these terminates the process mid-service. Fix: outer
  Try/Catch + CONF_BACKEND_ERROR logging in each Async Sub.
- [ ] **A2. Unsynchronized per-room dictionaries** — `ConferenceController._sttBackends/
  _sentenceBuffers/_clauseAccumulators/_pinnedClauseDials/_roomFilters/_roomTemplateIds`
  are plain Dictionaries touched by THREE thread classes: UI thread (web handlers via
  BeginInvoke), timer thread (BufferTimerTick → flushes), and sidecar tail threads
  (commit events). Fix: ConcurrentDictionary (mechanical swap; TryGetValue/indexer
  patterns already compatible) or marshal commit events to the UI thread.
- [ ] **A3. `New Random()` in `RoomManager.GenerateRoomId`** — same-millisecond room
  creations can collide. Fix: `Random.Shared` (or a Shared instance + lock).
  Same pattern (low risk) in FormSessionWizard hosting code.

## P1 — Responsiveness + boundaries

- [x] **B1. BibleController blocks the UI thread** — 8× `GetAwaiter().GetResult()`
  on SQLite/service calls in navigation paths (Initialize, LoadBibleTranslations,
  ShowBookButtons, LoadChapterAt, GoToReference, CopyVerse/Chapter). Bible
  verse translation (Phase 4) made these paths hotter. Fix: make the handlers
  Async Subs (with Try/Catch per A1) awaiting the service.
- [x] **B2. Controllers showing MessageBox** — ServerController (2×),
  TranscribeController (4×) call MessageBox directly; ConferenceController is
  clean (callback-injected). Fix: inject a notify callback or return results.
- [x] **B3. SubtitleService hand-rolled JSON** — 10+ string-concat payload sites
  with `EscapeJson` (one typo breaks every phone). EndpointRegistration already
  uses the safe `WriteAsJsonAsync(New With {...})` pattern. Fix: build payloads
  with `JsonSerializer.Serialize(New With {...})`. Same pattern (smaller) in
  LiveStreamRunner /start body + TranslationService request bodies (those two
  NOT yet converted — SubtitleService only).

## P2 — Engine independence (remaining violations, by severity)

The seams exist (descriptors, ISttBackend, ICloudSttEngineConfig, registries);
these are the places still bypassing them:

- [ ] **C1. Speechmatics clause hold-and-lock lives in shared code** —
  `ConferenceController` clause accumulators + `IsHoldEnabled` engine-key gate +
  the AppConfig `Speechmatics*` dial cluster + FormOptions dials. Largest
  violation, but DELIBERATE for now: the dials' live-tunability from Options is
  a valued workflow (hybrid pinning shipped in v1.9.2). Revisit only with a
  design that keeps live tuning (e.g. engine-owned accumulator service behind
  an ISttBackend capability interface).
- [x] **C2. Speechmatics inline-translation wiring in ConferenceController** —
  DONE: session wiring moved to `SpeechmaticsTranslation.ConfigureSession`
  (gate + block cast + target computation, incl. `SourceFlores`); retargeting
  goes through new `IRetargetableSttBackend` capability interface implemented
  by CloudStreamingSttBackend. No engine-key literals left in the controller's
  inline-translation path (clause-hold path C1 untouched, still gated).
- [x] **C3. LiveStreamRunner engine branches** — DONE:
  `ICloudSttEngineConfig.BuildStartJsonExtras()` lets each engine block emit
  its own /start JSON fragment (byte-identical output for Speechmatics);
  runner's 4 Speechmatics-only `Stt*` properties deleted (generic `SttApiKey`
  kept), replaced by `CloudEngineStartExtras`; sidecar-arg + model-path choice
  now driven by `SttBackendRegistry.Entry.SidecarMode` metadata ("whisper-cpp"
  default for unknown keys). Note: a speechmatics session without a config
  block now emits no speechmatics fields (previously emitted empty defaults) —
  unreachable in practice since EngineConfigResolver always supplies the block.
- [ ] **C4. Model scanning hardcoded in FormTemplateManager** — `*.bin` for
  whisper-cpp vs `config.json`-dir for others. Fix: descriptor-declared scan
  pattern (e.g. `ModelScan` metadata on the registry entry/descriptor).
- [ ] **C5. ServerController model-path branch** (`faster-whisper` →
  PathFasterWhisperModel) + **KestrelHost GoogleApiKey block** +
  **FormMain google-cloud-stt → google-translate affinity rule**. Fix: registry
  metadata (default model path per engine already exists on translation
  registry entries; mirror for STT) and a declared "companion translation
  backend" hint instead of hardcoded affinity.
- [ ] **C6. live-server faster-whisper/whisper-cpp branches** — offline engines
  bypass the `engines/` registry that online engines use. Fix: move them into
  `engines/` modules (design debt, not regression).
- [x] **C7. ServerOptions engine fields** — DONE: `SttRegion`/`SttOperatingPoint`
  deleted (nothing read them after C3) along with their ServerController
  assignments; KestrelHost's GoogleApiKey wiring block deleted — GoogleBackend
  now self-configures via an `IOptions(Of ServerOptions)` DI constructor.
  Note: the `GoogleApiKey` field itself stays on ServerOptions as the transport
  for the key (read only by GoogleBackend); FormMain.Shell still re-Configures
  GoogleBackend on Options save (live key updates).

## P3 — Hygiene

- [ ] **D1. `List(Of Object)` in EndpointRegistration** (SpeakerListFor, room
  members) — banned by project rules; use typed/anonymous lists.
- [ ] **D2. String value-sets → enums**: `Room.Mode` ("online"/"offline" — parse
  at the edge, enum inside), `DefaultVisibility`, `DisplayTemplate.Layout`.
- [ ] **D3. List+detail CRUD boilerplate** across the 5 template/manager forms
  (~650 duplicated lines). Extract a base form when next touched — not before
  (forms are stable and the user adjusts layouts in the Designer).
- [ ] **D4. EndpointRegistration size** (1,300 lines, 40 inline endpoints) —
  split into per-area files when next majorly edited.
- [ ] **D5. DateTime.Now in logs/persisted timestamps** — consistent but
  timezone-local; fine for a single-site app, note only.

## Verified clean
- SubtitleService client map (ConcurrentDictionary), TemplateLibraryStore
  (SyncLock), HttpClient lifetimes (shared/static), dialog Using blocks,
  no Debug.WriteLine anywhere, ConferenceController UI-boundary discipline
  (callback-injected, no MessageBox).
- The legacy hidden tabs (tabPagePaths/Settings/Server) referenced in CLAUDE.md
  appear to have been removed already — CLAUDE.md's warning is stale.

## Suggested order
1. P0 (A1–A3) — small, mechanical, removes real mid-service crash risk.
2. B1 + B2 — UI responsiveness + boundary, moderate.
3. B3 — JSON safety in the broadcast hot path.
4. C2 → C3 → C4/C5 — engine-independence debt, one seam at a time.
5. P3 opportunistically.
