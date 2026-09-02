# Engine Residency & Concurrency Plan

**Status:** IMPLEMENTED 2026-09-03 (all phases), verified same day — awaiting field validation on Jezer/laptop. Verification: 17/17 behavioural checks (tools/ReadinessTest: idle-based give-up ~5s, progress extends past the idle window with no hard cap, dead-process fail-fast, cancel, share/deny/re-route/evict/spare/idle-event/category independence), full solution build clean, audit suite clean (publish 5/5; heuristic findings in new code fixed), py_compile clean, live smoke on the dev box (warm queue → live-server spawned with per-port log live-server-5101.log → SidecarReadiness ready in 2.6s → /warm HTTP 200 → SaT resident → graceful spare shutdown at app exit, Errors=0).

**Implementation deviations (documented, not silent):**
1. llama-server is arbitrated via `EngineResidencyArbiter` as a coordinator consulted by ServerController/pool/FormMain — not literally folded into the pool class. Same behaviour (limit, warm spare, evict, re-route, idle re-warm), far less refactor risk to the field-critical paths.
2. Warm-spare PARKING covers ONLINE streaming engines (the Speechmatics case). Whisper-family rooms still stop fully at room close — their models are guarded by the STT slot limit + refuse rule instead; a whisper warm spare is future work.
3. RoomReadinessNotifier's TRANSLATION probe keeps its 60s fail-open (it polls an in-process availability flag — no process/log to read progress from). The STT side is fully progress-aware.
4. The room RESTART path (speaker/mode switch) re-wires the backend but does not re-arbitrate STT residency; the room's original lease stands. Residual gap, low risk (same room, usually same engine).
5. Web host panel warming display = the existing roomStatus preparing/ready notes (rsPreparing/rsTransWarming); no new web UI was added.
6. Bonus fix found during Phase 2: the translate-server pool tailed `translate-server-<port>.log` but Python wrote a FIXED filename — pooled sidecars' python lines never reached session.log. Both servers now accept `--log-name`.

Jeremy has further items to add (see the section at the bottom).

## Motivating incident (2026-09-02, laptop RTX 2070 Super 8GB)

Conference rooms silently produced no captions. Diagnosis chain:

1. Global translation engine = Salamandra (since v2.14), so llama-server loads at app start and holds **4,201 MiB** VRAM for the whole app session, with no refcounting.
2. The room's template engine (nllb-3.3b-int8, ~4GB CUDA) loads on top at room start → GPU over-commit/paging on an 8GB card.
3. A live-server `/health` poll hung, and a pre-existing `WaitForReady` defect (no per-poll timeout) let that one hung request eat the whole 30s retry window and exit through the silent cancellation branch. Zero error lines across 4 failed rooms.
4. Verified by `git diff 445d48f..a9c8e05` (v2.12 → v2.14): the defect is unchanged since ≤v2.12; the only room-start-path file that changed was ConferenceController (translation-context + Salamandra routing). v2.14 didn't add the bug — it added the VRAM squatter that triggers it on 8GB hardware.

**Already shipped (2026-09-02/03, unreleased):** per-poll timeouts (2s WaitForReady, 2s Start reuse-probe, 3s CheckCapturingAsync) so a hung poll fails into a retry; capture-start lifecycle instrumentation (STT_CAPTURE_LIFECYCLE 3011); honest fail-open logging (ROOM_READINESS_TIMEOUT 5110 instead of a false "STT ready"); live-server /start entry + silent-rejection logging. Awaiting field confirmation on Jezer + laptop.

## Design principle

Room engine replaces global engine (keep this semantic), but local GPU models must never stack beyond what the config allows. One arbiter sits in front of every local-model engine load.

## Config (new AppConfig keys, FormOptions advanced section, localized labels)

- `MaxConcurrentSttEngines` = 1 (default)
- `MaxConcurrentTranslationEngines` = 1 (default)
- `MaxConcurrentTtsEngines` = 1 (default)
- `EngineLoadIdleTimeoutSeconds` = 15 (default) — the ONLY give-up rule for engine loads: seconds of NO progress (process alive but log silent and probe failing) before a load is declared failed. **No absolute hard cap exists** (decided 2026-09-03: "check that things are progressing, rather than hard set boundaries" — nobody waits 5 minutes for anything).

Counts **resident local model processes** per category (registry ModelType/offline flag decides "local" — never engine names). Cloud/inline engines bypass. Two rooms sharing one model = 1 toward the limit. Floor 1. Forward-thinking: bigger GPUs raise the number and eviction simply stops happening.

## Arbiter rules (per category)

| Situation | Action |
|---|---|
| Requested engine already resident | Share it; lease++ |
| Resident count < limit | Load alongside; lease++ |
| At limit, some resident engine has zero leases (warm spare) | Evict one spare, load requested — one log line names both models + swap cost (~20–35s) |
| At limit, all resident engines leased | **Never load.** Category-specific (decided 2026-09-03): **Translation re-routes** the new room to a resident engine (any engine translates the pair; warning log + host-panel note — inline-hold-conflict philosophy). **STT refuses** — the room fails to start with a clear operator message naming both models (wrong-language fine-tune transcribing a service is worse than no room; fail-closed like the connectivity gate). |
| Cloud / inline engine | Bypass |

**Warm-spare lifetime (unifies today's two inconsistent lifetimes):** lease count 0 = model STAYS resident, evictable on demand. Fixes both current behaviours: NLLB today unloads at room close (next room repays ~30s load); llama-server today never unloads (squats 4.2GB). Eager app-start warm-up of the global engine = "pre-fill the spare" — kept, background, evictable.

**Idle state (decided 2026-09-03): after the LAST room closes, re-warm the GLOBAL engine.** If a room engine displaced the global one (e.g. NLLB evicted Salamandra), the arbiter background-swaps back to the Options engine once no room holds a lease — the app always returns to a predictable idle state, and the Translate workspace / desktop pipeline always finds its engine warm. (Accepted cost: back-to-back rooms with a non-global engine repay the swap; alternative "keep last-used warm" was considered and declined.)

## Fast room start — warm the whole chain, stop repaying per room (agreed 2026-09-03)

**Requirement (Jeremy):** room startups must be fast, and per-room repay of load costs is unacceptable. Measured on the laptop (2026-09-02, working run): ~20s to captions, ~28s to translations — repaid for EVERY room, because each room spawns a fresh live-server (SaT reload 15–35s) and NLLB unloads at room close (~27s reload).

1. **Live-server warm spare (kills the SaT repay + python boot):** keep ONE live-server process resident with SaT loaded, instead of spawn-per-room + kill-at-close.
   - App start (background, AFTER the translation warm — see warm queue below): if the effective default room configuration uses a streaming engine, pre-spawn the live-server; pre-load SaT **when the effective STT configuration enables it** (Speechmatics "Hold & merge"/"Split with SaT" dials, or whisper `sat_hold`). NOTE: SaT is an STT-side feature — the gate is the STT template, NOT the translation engine.
   - Needs a capture-less warm path in server.py (SaT `sat_segmenter.load` without /start; /start already reconnects sessions in-place, so add a clean stop-capture→restart cycle if one is missing).
   - Room start = attach to the resident process + /start (Speechmatics connect ~0.5s). Room close = stop capture, KEEP the process as the lease-0 spare. Port allocation changes from always-increment to reuse-the-spare.
   - A speechmatics live-server holds no GPU model — the spare is a process-warm mechanism aligned with the arbiter, not counted against the GPU limits. A whisper live-server DOES hold a model → counts against the STT slot as usual.
2. **Translation repay:** already fixed by the warm-spare rule above (NLLB stays resident at lease 0, evictable on demand).
3. **Background warm queue — one heavy load at a time:** app-start warm-ups run SEQUENTIALLY at low priority (translation engine first, then live-server/SaT). Parallel heavy loads are what caused both the original startup hang (pre-v2.0.4 eager loading) and the 2026-09-02 room-start stall — never trade one for the other. A room started mid-warm attaches to the in-progress load (lease on it) rather than spawning a duplicate.
   - **UI-thread rule:** engines load in child processes watched from background Tasks — the UI thread must NEVER wait on them (`.Result`/`.Wait()` banned on warm/arbiter paths; completions marshalled back via the controllers' `_marshal` pattern). Threading keeps the UI thread free; the sequential one-at-a-time queue keeps the MACHINE responsive — both are required, neither substitutes for the other.
   - **Process priority (decided 2026-09-03): normal, always.** No below-normal loading, no priority juggling — the sequential queue is the only throttle, and a live room's engine is never deprioritized.
   - **Warm churn rules (approved 2026-09-03):** changing the engine in Options only re-points the NEXT warm target — never an immediate swap (no churn from exploring settings). App shutdown aborts any in-progress warm instantly rather than waiting for it.
4. **Acceptance targets (laptop-class hardware, warm spares populated):** captions ≤3s from room create; translations ≤2s; second room after closing the first pays NO model reloads. Cold start (spares not yet warm) may still be slow but every step is now visible in the log (STT_CAPTURE_LIFECYCLE).
5. **Room-start timing summary event (approved 2026-09-03):** one log line per room start: "room ready in Xs (STT Ys, translation Zs, spare hit/miss per engine)" — same discipline as the clause-stats line at room close. Makes the acceptance targets verifiable from any field log, and gives log-readers the explanation in one place instead of reconstructing it from scattered lifecycle lines.
6. **Warming state visible in the UI (approved 2026-09-03):** status-bar element per category — cold / warming (elapsed seconds) / ready (model name) — plus the same state on the web host panel. Silence is what makes loading feel like a hang; a visible "Warming NLLB… 18s" reads as intentional. Localized strings as always.

## Static timeout review (2026-09-03) — replace wall-clock give-up deadlines with progress-aware waits

**Requirement (Jeremy):** statically set waiting periods ("after 30s give up", "wait 3s") assume a machine speed — fast PCs waste nothing but slow PCs get abandoned mid-load. Reviewed all ~144 VB + ~40 Python static time constructs. Verdicts:

- **Class A — wall-clock give-up deadlines on resource-dependent startups (FIX ALL):** `LiveStreamRunner.WaitForReady` 30s (Tuesday's failure site), `TranslationService.WaitForReady` 30s (NLLB — same disease, unfixed), `ConversationAudioHandler` 15s, `LlamaServerHost` 120s, `RoomReadinessNotifier` 60s fail-open, `ConferenceController` vocab-push ~60s, benchmark runners 60s–3min. Seven hand-rolled copies of the same loop.
- **Class B — per-operation caps (REVIEW SIZES):** keep the pattern (stops hangs), but the room-translate 10s cap can drop a translation during a cold first inference — must log + not silently drop; magic HTTP timeouts become named constants with rationale.
- **Class C (poll pacing 250ms–1s), E (shutdown WaitForExit grace + kill), F (infra constants):** correct, machine-independent — keep.
- **Class D — blind "wait and hope" sleeps:** each hides a missing event; convert opportunistically.

**Policy — idle-based waits:** one shared readiness helper (e.g. `SidecarReadiness.WaitAsync(probe, progressSignal, idleTimeoutSeconds)`) replacing all Class-A loops. Semantics: keep waiting while the sidecar shows PROGRESS (process alive + its tailed log advancing — PythonSidecarHost already tails every sidecar log, expose "seconds since last output"); give up only after `EngineLoadIdleTimeoutSeconds` of NO progress or on process exit. **No hard cap** (decided 2026-09-03) — progress alone decides. Self-adapting: slow PCs get as long as the engine visibly needs; dead engines fail FASTER than today (process-exit is immediate). Per-poll request caps (the 2026-09-02 fix) stay — they protect the poll, the idle rule replaces the deadline.

**Auditor (problem class has now recurred — make it mechanical):** heuristic-tier `tools/audit-static-timeouts.js` — flag new `AddSeconds(`/deadline-loop patterns outside the shared helper, and bare magic-number timeouts without a named constant.

## Phases

1. **Phase 1 — Translation slot + shared readiness helper:** the idle-based `SidecarReadiness` helper first (it underpins every warm/load path), retrofitted into all Class-A sites; then fold llama-server hosting into the translation pool as a pool entry (the pool already has acquire/release refcounting — extract-shared rule, not a third lifecycle); implement limit + warm-spare policy; the three config keys; guard-rail log at every model load (list resident models, warn on likely GPU over-commit); `audit-static-timeouts.js`.
2. **Phase 2 — STT slot + live-server warm spare:** the resident live-server with SaT pre-loaded (fast-room-start section above); whisper rooms currently spawn one whisper-server EACH (duplicate GGML loads for concurrent rooms) — share when model path matches, arbiter rules otherwise. Hygiene folded in (approved 2026-09-03): per-port live-server log files — today two concurrent live-servers write the SAME live-server.log and every room's sidecar host tails it, so [9000] lines duplicate and can belong to the OTHER room's server (confused the 2026-09-02 diagnosis); and verify Lite/headless gets identical warm behaviour through Core (no UI-thread assumptions in the warm queue).
3. **Phase 3 — TTS wiring:** knob exists from Phase 1; binds when a heavy local TTS engine lands.

## Known behaviour change at default 1

The v2.2.0 concurrent multi-NLLB pool serializes/shares under the limit. Shadow auditors (DeepL/Google) are cloud — unaffected. The translation benchmark comparing two LOCAL models must respect the limit (or prompt to raise the setting for the run) — decided: respect it; an over-committed benchmark reproduces the very stall this plan fixes.

## Considered and NOT adopted (2026-09-03, Jeremy's call — do not re-propose without new evidence)

Template-targeted warm (warm stays aimed at the GLOBAL engine); intent-triggered warming (hosting-page open as a warm signal); lease-based process-priority promotion; spare idle-TTL / memory-pressure eviction; learned per-model VRAM footprints; periodic spare heartbeat.

## Further items (Jeremy — to add)

- (placeholder — additional requirements to be captured here)
