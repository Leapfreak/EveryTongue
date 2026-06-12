# EveryTongue — Configuration Architecture Refactor

> **Status:** Phases 1–3 implemented (2026-06-12) — descriptor infrastructure, per-engine STT config blocks, SttConfig god-object split, template libraries + resolver, ConferenceTemplate migrated to referenced STT templates, translate/TTS descriptors, Speaker-as-references, Online/Offline gate, Display group, Filters-as-collection. See "Implementation status" below. Phases 4–5 pending.
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

**Not yet (deliberately):** workspace wiring/cross-engine capabilities onto `ResolvedSession` (Phase 4), clause-dial relocation into the Speechmatics template + descriptor-driven Options UI + per-group Template Manager + session wizard + locale strings for `EngineCfg_*` label keys (Phase 5), dead-settings purge (Phase 5 — note: exploration found `ParallelJobs`/`ChunkSizeSec`/`PollIntervalMs`/`ChunkTimeoutMin`/`KeepChunkFiles`/`KeepPreview`/`PathModelAudio`/`PathOutputRoot`/`TranslationDevice` are still live consumers; only `SkipDownloadIfExists`, `Hotwords`, `FreqThreshold`, `PrintRealtime`, `TranslationUnloadMinutes` are actually dead).

---

## Suggested phasing

1. **Model + persistence scaffolding** — template libraries, session template, reference resolution + logging. Introduce `IEngineConfigDescriptor` and per-engine config classes; **split the `SttConfig` god-object** into per-engine blocks. ✅ **DONE**
2. **Engine-aware STT / Translate / TTS templates** — migrate `ConferenceTemplate`; Speaker-as-references. ✅ **DONE**
3. **Online/Offline gate · Display group · Filters as collection.** ✅ **DONE**
4. **Workspace wiring** + enable the cross-engine capabilities (cloud STT in Transcribe, translate-in-Transcribe, TTS in Translate/Bible, Bible verse translation).
5. **UI reorg** + dead-settings purge + Session wizard.
6. *(deferred)* per-room/speaker filter selection + per-type precedence; offline-detection prompt; session recording/export slot.

---

## Deferred / assumptions

- **Filter precedence** (per-type: glossary merges, profanity/hallucination override) deferred until multi-set selection exists. For now filters are a single global set.
- **Target language:** dynamic for rooms, config for batch workspaces.
- **Speaker with multiple offline-engine refs:** first-listed is the default.
- **Offline-detection prompt** ("Looks like you're offline — switch?") already parked in `PLAN.md` → Future Work. Online/Offline is an explicit switch with **no auto-fallback**.

---

## The one rule to remember
**If a change to one engine forces a change to another engine — or to shared code that another engine depends on — the design is wrong.** Engines are islands; the registry + interface are the only bridges.
