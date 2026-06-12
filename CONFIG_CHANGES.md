# EveryTongue — Configuration Architecture Refactor

> **Status:** ALL phases (1–9) implemented (2026-06-12). The configuration architecture AND its runtime consumption are complete: engine-independent config blocks, template libraries, sessions (ConferenceTemplate), speakers with online/offline slots, the connectivity gate, per-room display templates, per-session filter sets, and per-group template managers. Parked: cloud-STT-in-Transcribe batch engines (PLAN.md → Future Work); deferred to v2.0: removal of the GoogleCloudSttApiKey read-bridge and ConferenceTemplate's legacy embedded knobs (kept for config.json back-compat); operational: regenerate CDN locale packs for the v1.9.x keys.
> **Origin:** the config sprawl exposed while solving the "sharing Catalan speaker" (Andreu) problem — settings scattered across Hardware/Translation/Server/Rooms, dead batch-era settings, and conference templates carrying knobs the chosen engine ignores.

---

## ⭐ Guiding principle — ENGINE INDEPENDENCE & MODULARIZATION (non-negotiable)

**Every engine is a fully self-contained module. Engines must never affect one another.**

There is a lot of fragile, engine-specific "hacking" involved in getting each engine working (dependencies, model/binary paths, API keys, endpointing quirks, version pins). That work must stay **contained inside the engine it belongs to**. Touching, adding, breaking, or removing one engine must have **zero effect** on any other engine.

Concrete rules (these override convenience everywhere in this plan):

1. **No shared/global engine settings.** There is **no** global `BeamSize`, `VadSilenceMs`, `Temperature`, etc. that "some engines use and others ignore." Each engine owns **only** the fields it actually consumes.
2. **Config schema lives *with* the engine.** A `SpeechmaticsConfig` belongs to the Speechmatics module; a `WhisperCppConfig` to the whisper module. **No god-object** holding every engine's fields co-mingled (today's `SttModels.SttConfig` is exactly the anti-pattern to kill).
3. **One template = one engine.** An STT/Translate/TTS template is **bound to exactly one engine** and contains **only that engine's config block**. A template can never hold two engines' fields.
4. **Engines self-describe.** Each engine registers a **config descriptor** (fields, types, defaults, ranges, UI hints). The Options/Template UI **renders from the descriptor** — so adding an engine adds **zero** UI code and touches **no other engine**.
5. **No cross-reads.** No engine reads another engine's config, state, or files. No `If backend = "speechmatics" Then …` branches scattered through shared code — engine-specific behaviour lives behind the engine's interface.
6. **Setup is encapsulated.** Each engine owns its dependency check, model/binary path resolution, API-key handling, and "installed & ready?" validation. The "hacking" to set one engine up never leaks into shared code or another engine.
7. **Independent diagnostics.** Each engine logs under its own event range/category (see `LogEvents`), so one engine's logs never tangle with another's.
8. **The registry is the only shared surface.** Engines register `{ key, displayName, requiresInternet, requiresApiKey, requiresGpu, configDescriptor, factory }`. All orchestration code is **engine-agnostic**, driving everything through the interface — exactly the pattern already used by `live-server/engines/` + `ISttBackend`/`SttBackendRegistry`. Extend that pattern to the **config layer**, where it is currently violated.

**Existing good models to extend:** `live-server/engines/` (engine-agnostic `server.py`, each engine self-registers), `ISttBackend`/`ITranslationBackend`/`ITtsBackend` + their registries.
**Anti-patterns to eliminate:** `SttModels.SttConfig` (whisper + cloud fields in one class); global whisper decoding params in `AppConfig` consumed by some backends; `ConferenceTemplate` carrying whisper-only fields (`BeamSize`/`VadSilenceMs`/`MaxSegmentSec`/`InitialPrompt`/`ModelPath`) for **all** engines incl. Speechmatics/Google that ignore them.

---

## The model — 3 tiers, templates everywhere

```
TIER 1 · APP / GLOBAL baseline      set once per machine; not per-session
TIER 2 · PRESET LIBRARIES           each config group = a library of named templates
TIER 3 · SESSION TEMPLATE           references one template per slot (+ overrides), saveable
          ↓ consumed by
         WORKSPACES                  Conference · Conversation · Transcribe · Translate · Bible
```

- **Reference, not embed** — a session points to named templates by id; edit a template once and every session using it updates.
- **Every group is a library** of named templates (e.g. STT: *"Speechmatics — sharing (pause-heavy)"*, *"Whisper-cpp Vulkan — reading"*, *"Whisper-cpp Vulkan — sharing"*).

---

## The 8 config groups

| Group | Templated? | Engine-bound? | Referenced by | Notes |
|---|---|---|---|---|
| **STT** | ✓ library | ✓ one engine/template | Session, Speaker | only the engine's own knobs |
| **Translate** | ✓ library | ✓ | Session, Speaker | |
| **TTS** | ✓ library | ✓ | Session, Speaker | |
| **Speaker** *(conference only)* | ✓ library | — | Session | thin bundle of **references** to preferred STT/Translate/TTS templates (online + offline variants) + glossary set |
| **Room Type** *(conference / conversation — separate schemas)* | ✓ | — | Session | conference holds **audio device** + a filter-set ref |
| **Display / Output** *(operator-level only)* | ✓ library | — | Session, desktop view | projected appearance, offered languages, layout |
| **Filters** (glossary / profanity / hallucination) | ✓ collection | — | Session / Room / Speaker | one of each for now; modelled as a collection for later |
| **Online / Offline** | — (a gate) | — | Session | filters which engine templates are eligible |

**Out of the structure:** viewer-level display (each phone's own font size) stays a **per-device preference**, not session config.

---

## Engine-aware templates (how independence is realized)

- Each engine module ships an **`IEngineConfigDescriptor`** (proposed): declares its fields, types, defaults, ranges, validation, and UI hints. The engine's template type is generated/driven from this — **the only place an engine's knobs are defined.**
- **STT template** = `{ engineKey, <that engine's config block> }`. Picking the engine determines the entire field set; no other engine's fields exist on it.
- **Speaker** = references to preferred templates (Andreu → online STT `"Speechmatics-sharing"`, offline STT `"Vulkan-sharing"`, Translate `"NLLB-elies-glossary"`, TTS `"Piper-ca"`). Swapping Sunday's "sharing" for a "reading" session = change one slot's reference.
- **Online/Offline gate** simply filters the eligible engine list (`requiresInternet`) before any template is offered/resolved.

---

## Resolution order (live session)

```
engine defaults  →  session's referenced templates  →  speaker references
                 →  per-slot session overrides       →  filters (global for now)
```

Each step is logged (see Logging).

---

## Workspace × capability matrix (flexibility = yes to all)

| Workspace | STT | cloud STT | Translate | TTS | Speaker | Display | Filters |
|---|---|---|---|---|---|---|---|
| **Conference** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Conversation** | ✓ | ✓ | ✓ | ✓ | ✗ (dynamic) | ✓ | ✓ |
| **Transcribe** | ✓ | ✓ | ✓ optional | ✗ | ✗ | ✗ | ✓ |
| **Translate** | ✗ | — | ✓ | ✓ | ✗ | ✗ | ✓ |
| **Bible** | ✗ | — | ✓ verses | ✓ | ✗ | ✗ | ✗ |

A workspace declares which groups it consumes; one that uses nothing (Bible STT) shows nothing.

---

## Persistence

- A **library of named templates per group** + **session templates** referencing by id. Move beyond the single flat `config.json` (per-group store / files). App-global stays the baseline.
- Each engine's config block serializes/deserializes via **its own descriptor** — never a shared serializer that knows all engines.

---

## Logging — baked in from the start

Per standing directive, instrument up front via `AppLogger.Log(LogEvents.X, …)`:
- template resolution (which template id resolved for each slot)
- engine selection + online/offline gating decision
- speaker-reference application (which refs adopted, which overridden)
- filter set loading
- per-engine setup/validation (installed? ready? which paths/keys)

Each engine logs under its **own event range/category** so diagnostics stay isolated. Goal: a reported bug is readable from the existing log with **no reactive logging code**.

---

## Cleanup folded in

- **Purge dead settings** (verify each first): chunk-pipeline cluster (`ParallelJobs`, `ChunkSizeSec`, `PollIntervalMs`, `ChunkTimeoutMin`, `KeepChunkFiles`, `KeepPreview`, `SkipDownloadIfExists`), `Hotwords`, `FreqThreshold`, `PrintRealtime`, `PathModelAudio`, `PathOutputRoot`, `TranslationDevice`, `TranslationUnloadMinutes`.
- Retire legacy single `GoogleCloudSttApiKey` (migrated into `SttApiKeys`).
- De-dup output-format flags (Job tab vs Options) — one owner.

---

## UI reorganization

- Options reorganizes around **App/Global · per-group Template Manager · Session wizard**.
- STT config **leaves Hardware**; EOU + clause dials **reunite under the STT (Speechmatics) template**; subtitle appearance **leaves Server → Display**.
- Template/Options forms render **from engine descriptors** — adding an engine never edits a form.

---

## Implementation status

**Phase 1 — DONE (2026-06-12):**
- `Services/Config/` — `IEngineConfigDescriptor` + `IEngineConfigBlock` + `EngineConfigField` (types/ranges/UI hints) + generic `EngineConfigDescriptorBase(Of T)` (reflection-driven JSON apply, overrides, range validation).
- Per-engine STT blocks in `Services/Stt/Configs/`: `WhisperCppConfig` (shared by vulkan/cuda/cpu), `FasterWhisperConfig`, `GoogleSttConfig`, `SpeechmaticsConfig` — each owns only its fields and its machine-baseline fill (`ApplyMachineBaseline`). Cloud blocks implement `ICloudSttEngineConfig.ConfigureRunner`, so `CloudStreamingSttBackend` is now fully engine-agnostic (no Speechmatics fields in shared code).
- **`SttConfig` god-object deleted.** `ISttBackend.Start` now takes `SttSessionConfig` (engine key, device, language, port, resolved API key + typed `EngineConfig` block).
- `SttBackendRegistry.Entry` gained `ConfigDescriptor`; all 6 entries registered.
- Template scaffolding: `Models/Templates/EngineTemplate` (one template = one engine + opaque config JSON, deserialized only by that engine's descriptor), `SessionTemplate` (slot references + per-slot overrides), `TemplateLibraryStore` (per-group JSON files under `%AppData%\EveryTongue\templates\`).
- `EngineConfigResolver` — resolution order `engine defaults → machine baseline → named template → per-session overrides`, every step logged (new Config events 1106–1112 in `LogEvents`).
- `ConferenceController` rewired through the resolver; engine-key branches removed from it (model-path selection moved into each engine's baseline; Speechmatics inline-translation gate is now a typed-block check). Wire format to the Python live-server unchanged.

**Phase 2 — DONE (2026-06-12):**
- **ConferenceTemplate → referenced STT templates.** `ConferenceTemplate.SttTemplateId` references the STT library; legacy embedded knobs (`BeamSize`/`VadSilenceMs`/`MaxSegmentSec`/`InitialPrompt`/`ModelPath`) are kept only for config.json back-compat and as a resolution fallback. Idempotent migration runs in `ConfigManager.ApplyDefaults` (`ConferenceTemplateMigration`) — 1:1, the library template reuses the conference template's Id. Empty `ModelPath` is omitted from the stored block so machine defaults still apply, and the block is filtered to the bound engine's declared fields (a Speechmatics-bound template stores no whisper knobs).
- `ConferenceController` resolves from the referenced template (`web override → STT template → machine baseline → engine defaults`); shared `BuildSttOverrides` keeps create/restart precedence identical to pre-refactor behavior.
- `FormTemplateManager` writes through to the library template on save (1:1 era — sharing one STT template across conference templates comes with the Phase 5 UI) and deletes it with the conference template.
- `TranslationBackendRegistry` + `TtsBackendRegistry` entries gained `ConfigDescriptor` (`BasicEngineConfigDescriptor` — empty block, engines currently expose no per-session knobs; an engine that grows knobs gets its own block class beside the engine).
- Speaker-as-references: `SpeakerProfile` (online/offline STT + translate + TTS template refs + glossary set ref) persisted by `TemplateLibraryStore` (`speaker-profiles.json`). Nothing consumes it yet — wired in with the session wizard.
- `EngineConfigResolver.BuildTemplateConfig` — builds partial template blocks (only provided keys, only the engine's declared fields).

**Phase 3 — DONE (2026-06-12):**
- **Online/Offline gate**: `ConnectivityMode` enum on `SessionTemplate` (explicit switch, default Online). `ConnectivityGate` filters eligible engines per group from the registries' `RequiresInternet` flag, gate-checks engine templates (`GateTemplate` → logs `CONFIG_GATE_DECISION` 1113 and blocks, **no auto-fallback**), and picks a speaker's online vs offline STT slot (`SelectSpeakerSttTemplateId`, no cross-mode fallback).
- **Display group**: `DisplayTemplate` (bg/fg colour, font family/size/bold, offered languages, layout hint) in the library (`display-templates.json`). Viewer-level per-device prefs deliberately excluded. `SessionResolver.ResolveDisplay` falls back to the app-global `Subtitle*` settings when unreferenced.
- **Filters as collection**: `FilterSet` (glossary/profanity/hallucinations paths; empty path = global default file) in the library (`filter-sets.json`). `SessionResolver.ResolveFilterSet` always returns concrete paths (global files when unreferenced). Per-type precedence still deferred (Phase 6).
- `SessionTemplate` extended: `Mode`, `DisplayTemplateId`, `FilterSetId`, `SpeakerProfileIds`. `SessionResolver.Resolve` produces a `ResolvedSession` (gate-validated engine slots + display + filters + speakers), fully logged.
- Nothing consumes `ResolvedSession` yet — Phase 4 wires the workspaces onto it.

**Phase 4 — DONE (2026-06-12), with one deliberate deferral:**
- **Workspace capability declarations**: `WorkspaceCapabilities` (`Services/Config/`) encodes the workspace × capability matrix; UI/session wiring reads it instead of hardcoding per-workspace assumptions.
- **Translate-in-Transcribe**: already existed — `PipelineRunner.TranslateSubtitlesAsync` runs in both the YouTube and local-audio-file modes whenever Output Language ≠ Input Language. Verified, no change needed.
- **TTS in Translate (desktop)**: new Speak/Stop button on the Translate tab. Synthesises the translated output through the server's `ITtsService` (sentence-chunked, ordered) and plays via the new shared `DesktopTtsPlayer` (`Services/Audio/`, NAudio). Requires the server running; localized status messages otherwise.
- **Bible verse translation + TTS (desktop)**: "Translate to" combo + "Read aloud" button on the Bible tab. `BibleController` translates the displayed chapter verse-by-verse via `ITranslationService` (FLORES source derived from the Bible translation's ISO-639-3 language via new `TranslationService.Iso3ToFloresLang`) and re-renders with the translation under each verse; Read aloud speaks the translated chapter when translation is active, else the original.
- **Bible verse translation (web)**: Translate/Original toggle in the Bible read-all bar (shown only when the viewer's language differs from the Bible's). Sequentially POSTs `/api/translate` per verse and renders the translation under each verse (ES5, run-id guarded). Web Bible TTS already existed (verse ▶ buttons + Read All, browser or server TTS).
- **DEFERRED — cloud STT in Transcribe**: the cloud engines (Google, Speechmatics) are streaming-realtime only; batch file transcription would need new batch-API engine modules (Speechmatics batch jobs API, Google long-running recognize) in `live-server/engines/` plus an upload pipeline. That is engine work, not workspace wiring — schedule alongside future engine additions. The capability is declared in `WorkspaceCapabilities` so the UI surface is ready.

**Phase 5 — structural parts DONE (2026-06-12):**
- **Dead-settings purge**: removed `SkipDownloadIfExists`, `Hotwords`, `FreqThreshold`, `PrintRealtime`, `TranslationUnloadMinutes` from `AppConfig`, FormOptions (controls + load/save) and en.json. The others on the original purge list were verified live consumers and kept (see Phase 1 notes).
- **Descriptor-driven STT Template Manager** (`FormEngineTemplates`): CRUD over the STT library. Knob editors are rendered from the bound engine's `IEngineConfigDescriptor` at runtime (the one sanctioned use of runtime-generated controls — the field set depends on the engine), so **adding an engine never edits this form**. Only edited values are stored (empty paths omitted → machine baseline still applies). Deleting a template referenced by conference templates warns with the reference count.
- **Shared STT templates in conference templates**: `FormTemplateManager` gained an "STT Template" picker — "(this template's own settings)" (the 1:1 write-through, as before) or a named library template. Choosing a shared template disables the embedded knob editors, references it by id (never overwrites it), and syncs the engine key. **This delivers the original Andreu scenario**: one "Speechmatics — sharing" template referenced by many conference templates, edited once.
- **`EngineCfg_*` locale strings** added to en.json (all descriptor field labels) + `EngTpl_*`/`Tmpl_SttTemplate*` form strings.

**Phase 5 — completion round (2026-06-12, user-approved decisions):**
- **HYBRID clause dials**: the Speechmatics clause hold-and-lock dials are now template-pinnable — `SpeechmaticsConfig` carries them as Advanced descriptor fields. A room whose STT template explicitly stores any clause field uses those values, FIXED for the session (`_pinnedClauseDials` in ConferenceController, logged at room start); rooms without pinned dials keep today's live Options tuning (`CurrentThresholds(roomId)` reads AppConfig fresh). The template editor only stores advanced fields when "Include advanced settings" is checked (auto-checked when a template already pins some), so templates don't accidentally pin dials. Clause timer (poll resolution) stays machine-global in Options.
- **Options reorg**: new **Speech-to-Text** nav page (engine, API key, Speechmatics region/operating-point/EOU — moved out of Hardware, which now holds only readiness scoring) with a "Manage STT Templates…" button; new **Display** nav page (subtitle colors/font — moved out of Server, renamed from "Server & Subtitles" to "Server"). Nav order: General · Tool Paths · Speech-to-Text · Translation · Text-to-Speech · Display · Server · Hardware · Advanced.
- **Session wizard → session templates**: the wizard's final step gained "Save as session template" + name. It creates a Display template from the appearance step, engine-choice STT/translate templates (no knobs — machine baseline applies), and a SessionTemplate referencing them by id.

**Remaining (parked):**
- cloud-STT-in-Transcribe batch engines (PLAN.md → Future Work; see Phase 4 notes), clause-dial relocation into the Speechmatics template + descriptor-driven Options UI + per-group Template Manager + session wizard + locale strings for `EngineCfg_*` label keys (Phase 5), dead-settings purge (Phase 5 — note: exploration found `ParallelJobs`/`ChunkSizeSec`/`PollIntervalMs`/`ChunkTimeoutMin`/`KeepChunkFiles`/`KeepPreview`/`PathModelAudio`/`PathOutputRoot`/`TranslationDevice` are still live consumers; only `SkipDownloadIfExists`, `Hotwords`, `FreqThreshold`, `PrintRealtime`, `TranslationUnloadMinutes` are actually dead).

---

## Suggested phasing

1. **Model + persistence scaffolding** — template libraries, session template, reference resolution + logging. Introduce `IEngineConfigDescriptor` and per-engine config classes; **split the `SttConfig` god-object** into per-engine blocks. ✅ **DONE**
2. **Engine-aware STT / Translate / TTS templates** — migrate `ConferenceTemplate`; Speaker-as-references. ✅ **DONE**
3. **Online/Offline gate · Display group · Filters as collection.** ✅ **DONE**
4. **Workspace wiring** + enable the cross-engine capabilities (cloud STT in Transcribe, translate-in-Transcribe, TTS in Translate/Bible, Bible verse translation). ✅ **DONE** *(cloud-STT-in-Transcribe deferred to engine work)*
5. **UI reorg** + dead-settings purge + Session wizard. ✅ **Structural parts DONE** — purge, descriptor-driven STT Template Manager, shared-template picker, EngineCfg_* strings. *Open: cosmetic Options reorg (STT out of Hardware, subtitle appearance → Display), clause-dial relocation (kept live-tunable in Options on purpose), session wizard — layout decisions to be directed by the user in the WinForms Designer.*
6. **Speakers + Online/Offline gate at runtime** — see "Runtime consumption plan" below. 
7. **Display consumption** — see below.
8. **Filter consumption** — see below.
9. **Session convergence + per-group managers + legacy retirement** — see below.
10. *(still deferred)* per-type filter precedence; offline-detection prompt (PLAN.md); session recording/export slot.

---

# Runtime consumption plan (Phases 6–9)

> Everything below wires ALREADY-BUILT scaffolding into live behavior. No new
> config-model concepts are introduced. The production path is **conference
> rooms** (phone web client + desktop server), so consumption centers there.
> Each phase is independently shippable and testable; order is by user value.

## Guiding decision — ConferenceTemplate IS the conference session (recommended)

Two session concepts exist: `ConferenceTemplate` (proven hosting unit: name,
hosting code, audio device, visibility, STT template ref) and `SessionTemplate`
(generic slot references, currently created only by the wizard). Running both
as parallel "session" notions invites drift. **Recommendation: evolve
ConferenceTemplate into the conference-flavored session** by adding the missing
slot references (`Mode`, `DisplayTemplateId`, `FilterSetId`,
`SpeakerProfileIds`) — it already follows the reference-not-embed model since
Phase 2. `SessionTemplate` remains the wizard/desktop-session shape and a
future shape for non-conference workspaces; Phase 9 decides whether to merge or
retire it. *(If you'd rather make SessionTemplate the one true session and have
ConferenceTemplate reference it, say so before Phase 6 starts — it changes the
field placement below but not the work shape.)*

## Phase 6 — Speakers + Online/Offline gate (the Andreu core) ✅ **DONE (2026-06-12)**

> Implemented as planned with the ConferenceTemplate-as-session decision:
> `ConferenceTemplate.Mode` + `SpeakerProfileIds`; `Room.ActiveSpeakerId` +
> `Room.Mode` (initialized from the template at creation); `FormSpeakerProfiles`
> (reference-only CRUD, opened from the conference template editor); speaker
> checklist + Mode combo in FormTemplateManager; host panel (app.js) speaker
> dropdown + Online/Offline selector posting `speakerId`/`mode` to
> `/api/control/pipeline` (sent only when changed); `TrySwitchSpeaker` /
> `TrySwitchMode` pre-validate through the gate and REJECT (logged
> `CONFIG_GATE_DECISION`) rather than fall back; `ResolveRoomSttTemplate` makes
> the active speaker's gated slot win over the template reference in both the
> create and restart paths; room start fails closed when the mode makes the
> engine ineligible. New event `CONF_SPEAKER_SWITCHED` (5011). Speaker glossary
> combo present but disabled until Phase 8.

**Goal:** mid-service speaker switching: the host picks "Andreu" on the phone
panel and the room restarts onto his preferred STT template; flipping the room
to Offline re-resolves through his offline slot. No auto-fallback ever.

1. **Speaker manager UI** (`FormSpeakerProfiles`, opened from FormTemplateManager
   and the Options STT page): CRUD over `SpeakerProfile` — name + four reference
   combos (online STT template, offline STT template, translate template, TTS
   template; "(none)" allowed) + glossary-set ref (combo populated from filter
   sets; disabled until Phase 8). Pure reference picking — no descriptor
   rendering needed.
2. **ConferenceTemplate gains** `Mode As ConnectivityMode` (default Online) and
   `SpeakerProfileIds As List(Of String)`; FormTemplateManager gets a Mode combo
   and a speaker checklist (order = display order).
3. **Room runtime state**: `Room` gains `ActiveSpeakerId` + `Mode`;
   `/api/rooms/{id}` responses include the speaker list (id+name) and mode so
   the host panel can render them.
4. **Host panel (app.js, ES5)**: speaker dropdown + Online/Offline toggle in the
   existing pipeline panel. Both post to `/api/control/pipeline`
   (`{speakerId}` / `{mode}`), which routes to ConferenceController.
5. **ConferenceController**: `SwitchSpeaker(roomId, speakerId)` →
   `ConnectivityGate.SelectSpeakerSttTemplateId(speaker, roomMode)` → resolve
   that engine template (existing resolver path) → restart backend (reuse
   `RestartConferenceBackend` machinery; flushes buffers, locks clauses — all
   already there). Empty slot for the current mode → log `CONFIG_GATE_DECISION`
   + host-panel toast, **no fallback**. `SetMode(roomId, mode)` re-runs the same
   resolution for the active speaker.
6. **Gate enforcement on create**: room creation resolves through
   `ConnectivityGate.GateTemplate` using the template's Mode (today it's
   ungated); blocked → clear host-panel error.
7. **Logging**: speaker switch + gate decisions land under the existing
   Config/Conference event ranges; add `CONF_SPEAKER_SWITCHED` event.

*Touches:* new FormSpeakerProfiles (+Designer), FormTemplateManager,
ConferenceTemplate, RoomModels, EndpointRegistration, ConferenceController,
app.js, en.json. *Risk:* restart-path regressions — mitigated because speaker
switch reuses the existing restart machinery. *Test:* two speakers with
different engines (Speechmatics vs Vulkan); switch mid-session; flip offline
and confirm the Speechmatics speaker blocks with a logged gate decision.

## Phase 7 — Display consumption ✅ **DONE (2026-06-12)**

> Implemented: `FormDisplayTemplates` (colors, font, layout, offered-languages
> checklist) opened from the Options Display page and from the conference
> template editor's new Display picker (`ConferenceTemplate.DisplayTemplateId`).
> Rooms resolve their Display template at creation (`Room.Display`); the room
> payload exposes it; app.js applies per-room colors/font to the subtitle view
> and filters the language picker + translation dropdown by offered languages
> (empty = all). Per-device font size deliberately untouched. **Deviation from
> the original sketch:** the desktop projected view keeps the app-global
> appearance — with multiple simultaneous rooms there is no single "active"
> room to bind it to; revisit if a dedicated projected-room selector is added.

**Goal:** a room's projected/viewer appearance and offered languages come from
its Display template; the app-global `Subtitle*` settings remain the fallback.

1. **Display template manager** (`FormDisplayTemplates`: name, colors, font,
   offered-languages checklist, layout combo) opened from the Options Display
   page; ConferenceTemplate gains `DisplayTemplateId` + picker.
2. **Server**: room creation resolves `SessionResolver.ResolveDisplay` →
   stored per room; a `/api/rooms/{id}/display` (or fields on the existing room
   payload) exposes it.
3. **Web client (app.js, ES5)**: room view applies per-room colors/font via a
   style block; **offered languages** filter the language picker for that room
   (empty list = all — current behavior).
4. **Desktop projected view**: `SubtitleService.BgColor/FgColor` switch to the
   active room's resolved display when one is set.

*Touches:* new FormDisplayTemplates, FormTemplateManager, ConferenceTemplate,
SubtitleService/ServerOptions, EndpointRegistration, app.js, en.json.
*Test:* two rooms with different display templates side-by-side on two phones;
per-device font-size preference must still override (viewer-level stays local).

## Phase 8 — Filter consumption (per-session sets) ✅ **DONE (2026-06-12)**

> Implemented: translate-server `/translate` and `/glossary/apply` accept
> optional `glossary_path`/`profanity_path` (per-path cache, mtime-invalidated,
> fall back to global files on error). SAFE with the translation cache —
> verified that glossary/profanity are applied AFTER the cached raw NLLB
> output, so the cache key needs no filter component. live-server `/start`
> accepts `hallucinations_path` (per-room sidecar instance = per-room filter).
> .NET: `TranslationFilterPaths` threaded through ITranslationService /
> ITranslationBackend / orchestrator / sidecar backend (cloud backends ignore
> it); `SttSessionConfig.HallucinationsPath` → LiveStreamRunner → /start.
> `ConferenceTemplate.FilterSetId` + picker; `ConferenceController` resolves
> per-room effective filters at create/restart (speaker `GlossarySetId`
> overrides the room's glossary — speaker > room > global, delivering the
> per-speaker glossary) and passes them on every room-scoped translate +
> glossary-apply call. `FormFilterSets` manager (paths + "copy global files
> into this set" seeding `%AppData%\EveryTongue\filters\{id}\` + open-folder);
> speaker glossary combo now active. **Note:** the Filter Editor still edits
> the GLOBAL files — named sets are edited via their folder for now.

**Goal:** a room can use a named FilterSet instead of the global files.

1. **FilterSet manager**: extend FormFilterEditor with a set selector (sets
   from `TemplateLibraryStore.GetFilterSets()` + "(global)"); editing a named
   set edits its own files (stored under `%AppData%\EveryTongue\filters\{id}\`).
2. **live-server** (per-room sidecar — easy): `/start` gains
   `hallucinations_path`; ConferenceController passes the room's resolved
   `FilterSet.HallucinationsPath`.
3. **translate-server** (shared sidecar — the real work): `/translate` gains
   optional `glossary_path`/`profanity_path`; server caches loaded sets keyed
   by path (mtime-invalidated). .NET passes the room's resolved paths on every
   room-scoped translate call.
4. **References**: ConferenceTemplate gains `FilterSetId` + picker; speaker
   `GlossarySetId` activates (speaker override > room set > global), which
   delivers the deferred "per-speaker glossary" in its simplest form.

*Touches:* FormFilterEditor, both Python servers, TranslationService /
orchestrator call sites, ConferenceController, ConferenceTemplate, en.json.
*Risk:* highest of the four (Python + shared-sidecar caching); ship behind
"(global)" default so nothing changes until a set is selected.
*Test:* room A with a custom glossary set, room B global, simultaneously; check
filter-hit logs attribute the right set.

## Phase 9 — Convergence + cleanup ✅ **DONE (2026-06-12)**

> Implemented: the wizard's "Save as session template" now emits a hostable
> **ConferenceTemplate** (wizard device/language + referenced Display template
> + 1:1 STT write-through + auto-generated 6-digit hosting code, synced to the
> lobby) — `SessionTemplate` is no longer created anywhere; the model/store
> remain as scaffolding for future non-conference workspaces.
> `FormEngineTemplates` generalized to a group parameter (stt/translate/tts)
> with group-aware delete reference counts (conference templates + speaker
> slots); "Manage Translation/TTS Templates…" buttons added to the Options
> Translation and TTS pages so speaker references are creatable. FormMain's
> direct `GoogleCloudSttApiKey` reads replaced with `GetSttApiKey` (the
> read-bridge in AppConfig stays until v2.0 so pre-1.8.x configs don't lose
> their key). Remaining operational item: regenerate the CDN locale packs.

1. **Wizard/SessionTemplate convergence** per the guiding decision: either the
   wizard emits a ConferenceTemplate (+ Display ref) and `SessionTemplate` is
   retired, or SessionTemplate stays for non-conference workspaces — decide
   with Phase 6 experience in hand.
2. **Per-group template managers**: generalize FormEngineTemplates to take a
   group + registry adapter so Translate/TTS get managers the day their engines
   grow per-session knobs (zero value before then).
3. **Legacy retirement**: `GoogleCloudSttApiKey` bridge removal (one release
   after v1.9.x has migrated keys), ConferenceTemplate legacy embedded knobs
   (`BeamSize` etc.) dropped from JSON once all configs are migrated.
4. **CDN locale pack regeneration** for all v1.9.x keys (operational, not code).

## Suggested sequencing & sizing

| Phase | Value | Size | Depends on |
|---|---|---|---|
| 6 Speakers+Gate | the original problem | L (1 full session) | nothing |
| 7 Display | high visual payoff | M | nothing |
| 8 Filters | medium; per-speaker glossary | L (Python) | 6 (speaker refs) for the speaker-override part |
| 9 Convergence | hygiene | S–M | 6 (decision input) |

Phases 6 and 7 are independent — either can go first. Recommended: 6 → 7 → 8 → 9.

---

## Deferred / assumptions

- **Filter precedence** (per-type: glossary merges, profanity/hallucination override) deferred until multi-set selection exists. For now filters are a single global set.
- **Target language:** dynamic for rooms, config for batch workspaces.
- **Speaker with multiple offline-engine refs:** first-listed is the default.
- **Offline-detection prompt** ("Looks like you're offline — switch?") already parked in `PLAN.md` → Future Work. Online/Offline is an explicit switch with **no auto-fallback**.

---

## The one rule to remember
**If a change to one engine forces a change to another engine — or to shared code that another engine depends on — the design is wrong.** Engines are islands; the registry + interface are the only bridges.
