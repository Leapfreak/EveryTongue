# EveryTongue — Project Rules for Claude

## Build & Publish
- **Publish command**: `dotnet publish EveryTongue/EveryTongue.vbproj -c Release -r win-x64 --self-contained false`
- **Publish output**: `EveryTongue\bin\Publish\` (set in vbproj via `<PublishDir>`)
- **NEVER use `-o` flag** — the user runs the EXE from `bin\Publish\`
- Must close EveryTongue.exe before publishing (DLL lock)
- Clean if stale: `dotnet clean EveryTongue/EveryTongue.vbproj -c Release`
- **Always publish after code changes** — after making changes, automatically run the publish command so the user can test immediately. Do not wait to be asked.

## Architecture — Read Before Changing

### Two UI systems exist (DO NOT MIX)
- **FormOptions.vb** — the ACTIVE options dialog (Tools → Options / Tool Paths / Server Settings). This is what the user sees and interacts with. Changes to settings/paths/server config go here.
- **Hidden tab pages** (tabPagePaths, tabPageSettings, tabPageServer in FormMain.Designer.vb) — LEGACY controls that are never navigated to. They still exist in the form and are read by `SaveUiToConfig()`.
- **CRITICAL**: `SaveUiToConfig()` reads values from hidden tab controls and writes them to `_config`. If FormOptions has updated `_config` but the hidden controls are stale, `SaveUiToConfig` will OVERWRITE the good values with stale ones.
- **Rule**: When adding a setting to FormOptions, either also sync the hidden tab control via `LoadConfigToUi()`, or (better) remove it from `SaveUiToConfig()` so the hidden control is never read.
- `_isSyncingUi` flag suppresses `SaveUiToConfig` during `LoadConfigToUi` to prevent mid-update overwrites.

### Nav rail and workspaces
- The 80px nav rail reduces content width. Use `cw = 760` (not 880) for layout calculations in Designer.vb.
- `SwitchWorkspace()` shows/hides tab pages. Only Live, Transcribe, Translate, and Bible are reachable from nav rail.
- Hidden tabs (Paths, Settings, Server, Help) are NEVER shown via nav rail — only via FormOptions or menu items that call helpers directly.

### WinForms Designer preference
- **Always use Designer files (*.Designer.vb) for UI layout** — controls, sizing, positioning, anchoring, docking, and property setup belong in `InitializeComponent()`.
- **Do NOT create or configure controls dynamically in code-behind (.vb files)** unless absolutely necessary (e.g. runtime-generated content).
- When adding new controls to a form, add them to the corresponding `.Designer.vb` file and declare them as `Friend WithEvents` fields.

### WinForms layout pitfalls
- `ClientSize.Width` returns wrong values during `SuspendLayout()`. Use known constants (like `cw`) instead.
- When a panel has `Dock = DockStyle.Fill`, its default size is ~200px. Controls added BEFORE docking use that small size for anchor math. Set the panel size explicitly BEFORE adding anchored controls, THEN set `Dock = Fill`.
- `PerformClick()` on buttons inside hidden/non-visible tab pages may silently fail. Call the underlying method directly instead.
- `&&` in TreeView node text shows literally as "&&" — TreeView doesn't use `&` as mnemonic. Only ToolStripMenuItem uses `&&` to escape `&`.
- `Keys.Oemcomma` displays as raw text in menus. Set `ShortcutKeyDisplayString` explicitly.

### Bible — Two separate viewers (BOTH must be updated)
- **Desktop Bible tab** — native WinForms `RichTextBox` in `Controllers/BibleController.vb`. Uses `BibleService` directly to fetch and render verses via `DisplayVerses()`.
- **Phone/WebView2 Bible panel** — JavaScript in `wwwroot/js/app.js`. Fetches verses via `/bible/` REST endpoints and renders in HTML.
- These are completely independent renderers. Any Bible feature (copyright, formatting, new UI elements) must be implemented in BOTH places or it will only work in one.

### Translation
- The translation server uses FLORES-200 language codes (e.g. `eng_Latn`, `spa_Latn`), NOT ISO 639-1 codes (`en`, `es`).
- Always convert whisper codes to FLORES codes via `TranslationService.WhisperToFloresLang()` before calling the translation API.
- NLLB works best on single sentences. Multi-sentence input should be split client-side and translated sentence-by-sentence.
- The translation pipeline (live/job) already does sentence splitting. The Translate workspace does its own splitting in `RunTranslateAsync` via `SplitIntoSentences()`.

### Web client (wwwroot/js/app.js)
- Must be ES5-compatible (no arrow functions, no `let`/`const`, no template literals).
- WebView2 caches aggressively. Use `_cb={timestamp}` cache-busting URL params when navigating.
- The `?bibleLang=` param triggers auto-opening the Bible panel and skipping the language picker.

### Process pipe buffer (CRITICAL)
- **Windows pipe buffer is 4KB**. When you set `RedirectStandardOutput = True` or `RedirectStandardError = True` on a `ProcessStartInfo`, you MUST drain BOTH pipes before calling `WaitForExit()` or the child process WILL deadlock once the buffer fills.
- For synchronous code: read one pipe async, the other sync, then wait:
  ```vb
  Dim stderrTask = proc.StandardError.ReadToEndAsync()
  Dim stdout = proc.StandardOutput.ReadToEnd()
  stderrTask.Wait()
  proc.WaitForExit(timeout)
  ```
- For long-running processes: use background `Task.Run` drain loops (see `SttConcurrencyRunner.vb` for the pattern).
- Even if you think the process doesn't write to stdout — it might. Always drain both.

### Python servers
- `translate-server/server.py` — translation sidecar: NLLB (FastAPI on TranslationPort)
- `live-server/server.py` — live transcription relay
- `mms-tts-server/server.py` — MMS-TTS speech synthesis
- All use embedded Python from `python-embed/`

## Code Quality Rules
- **No `Debug.WriteLine`** — always use `AppLogger.Log(LogEvents.EVENT_ID, msg)`. `Debug.WriteLine` is stripped from Release builds, so production errors go invisible.
- **No `List(Of Object)` for known types** — use the actual type (e.g. `List(Of DetectedReference)` not `List(Of Object)` with `DirectCast`).
- **Each workspace gets its own controller** — FormMain delegates to controllers in `Controllers/`. A controller owns the event wiring, state, and logic for one workspace tab. FormMain only handles cross-cutting concerns (config, theme, navigation, shell). New features should get a new controller, not more code in FormMain.
- **Extract when duplicating across 2+ classes** — if two services copy-paste the same logic (process management, file I/O patterns, etc.), extract a shared class immediately. See `PythonSidecarHost` as the model.
- **Verify "avoids circular DI" claims** — this project uses Kestrel DI. Before accepting post-build property wiring, check whether the dependency is actually circular. Prefer constructor injection.
- **Use enums for fixed value sets, not strings** — if a config value can only be one of N known values, make it an enum with `JsonStringEnumConverter` for backward-compatible JSON serialization.
- **All user-facing strings must be localised** — never hardcode English in code. Use `GetString("Key")` or `_getString("Key")` at runtime via `LanguagePackService`. Locale files are JSON in `locales/*.json`. The number of supported languages is not fixed — any language pack can be downloaded or added. When adding new string keys, add to `locales/en.json` then translate to all other existing locale files in the folder.
- **Missing dependencies → prompt Download Manager** — when a feature can't run because tools are missing, call `AppLogger.PromptDownloadManager(message, title)` which shows a Yes/No dialog offering to open the Download Manager. Don't silently fail or show a dead-end error.
- **Never hardcode engine names, model paths, or backend-specific values** — the app supports pluggable STT, translation, and TTS backends via registries. Code must read the configured backend from `AppConfig` and behave accordingly. Don't assume a specific backend (e.g. "faster-whisper", "whisper-cpp-vulkan") — always use the config value. When scanning for models, use file patterns (e.g. `*.bin` for GGML files) not name prefixes. Model paths differ by backend (files vs directories); always resolve the correct path based on the active backend key.

### Adding a new TTS engine
1. Create a class in `Services/Tts/` implementing `ITtsBackend` (see `PiperBackend.vb`, `MmsTtsBackend.vb`, `EdgeTtsBackend.vb` for examples)
2. Register it in DI in `Server/KestrelHost.vb` (e.g. `services.AddSingleton(Of ITtsBackend, MyNewBackend)()`)
3. Add one line to `Services/Tts/TtsBackendRegistry.vb` in the `_backends` list:
   ```vb
   New Entry With {.Key = "mykey", .DisplayName = "My Engine (offline)", .RequiresInternet = False}
   ```
4. The Options dialog TTS preference combos auto-populate from the registry — no UI changes needed.

### Adding a new translation engine
1. Create a class in `Services/Translation/` implementing `ITranslationBackend` (see `SidecarTranslationBackend.vb`, `CloudTranslationBackend.vb`)
2. Register it in DI in `Server/KestrelHost.vb`
3. Add one line to `Services/Translation/TranslationBackendRegistry.vb` in the `_backends` list:
   ```vb
   New Entry With {.Key = "mykey", .DisplayName = "My Engine (online)", .RequiresInternet = True, .RequiresApiKey = True}
   ```
4. The Options dialog Translation engine combo auto-populates from the registry — no UI changes needed.
5. `AppConfig.TranslationBackend` stores the user's selected engine key (default: `"nllb"`).

## Before Making Changes
1. **Read the full call chain** — trace what calls what before editing. Many bugs came from not understanding that `SaveUiToConfig` was called from 15+ places.
2. **Check for duplicate code paths** — settings can be changed from hidden tabs, FormOptions, menu handlers, and keyboard shortcuts. Make sure all paths stay consistent.
3. **Build and verify** — `dotnet build` after every change, not just at the end.
4. **Don't leave vestigial code active** — if a UI path is replaced, disable the old one so it can't overwrite the new one.
5. **When fixing a bug, check for the same bug elsewhere** — if a pattern is wrong in one place, search the codebase for the same pattern in other files/methods and fix all occurrences.
6. **When unsure about a bug's cause, add debug logging first** — use `AppLogger.Log(LogEvents.EVENT_ID, msg)` to log state/values to the log file, publish, and inspect the output. Do NOT assume root causes without evidence. For web client debugging, use the `SLOG()` function (routes to server log via WebSocket) so output appears in the server console the user is already watching — never ask the user to open browser dev tools.
7. **Trace the full code path before changing anything** — when a feature isn't working, read every function in the chain from trigger to effect before proposing a fix. Do NOT guess-and-iterate with multiple publish cycles. Understand the root cause first, fix once.

## Project Structure
- VB.NET WinForms app targeting net8.0-windows
- Uses WebView2, Kestrel (ASP.NET Core), SQLite, NAudio, QRCoder
- Web client served from `wwwroot/`
- Key files:
  - `Forms/FormMain.vb` — main form logic, config save/load (slim — delegates to controllers)
  - `Forms/FormMain.Shell.vb` — UI shell (nav rail, menu, status bar)
  - `Forms/FormMain.Designer.vb` — control layout for hidden tab pages
  - `Forms/FormOptions.vb` — the active settings dialog
  - `Forms/FormDownloadManager.vb` — centralized dependency installer
  - `Controllers/` — LiveController, TranscribeController, ServerController, BibleController, TranslateController
  - `Server/KestrelHost.vb` — Kestrel web server setup
  - `Server/EndpointRegistration.vb` — API endpoints
  - `Pipeline/TranslationService.vb` — translation service wrapper, language code maps
  - `Pipeline/PythonSidecarHost.vb` — shared Python sidecar lifecycle (used by TranslationService, LiveStreamRunner, MmsTtsBackend)
  - `Services/Infrastructure/AppLogger.vb` — centralized structured logging with event IDs
  - `Services/Infrastructure/LogEvents.vb` — all event ID constants and registry
  - `Forms/FormLogConfig.vb` — log routing configuration dialog
  - `Models/AppConfig.vb` — configuration model
  - `Models/DependencyManager.vb` — tool/dependency checking and downloading

## Assets
- **NEVER clean the Assets folder** — keep all images whether currently used or not

## Logging — Structured Event System (v1.8.5)
- **All logging uses structured events**: `AppLogger.Log(LogEvents.EVENT_ID, message)` — never use a plain string overload (it was removed).
- **Event IDs**: Defined in `Services/Infrastructure/LogEvents.vb` as integer constants (120+ events). Each event has a category (`LogCategory`) and default severity (`LogSeverity`).
- **Categories**: 20 categories in `LogCategory` enum — Startup, Server, Stt, Pipeline, Translation, Tts, Conference, Rooms, Bible, UI, Config, Download, Locale, Audio, PythonLog, Benchmark, Hardware, Update, Legacy, Uncategorised.
- **Routing**: `LogRoutingConfig` controls which categories/levels go to file vs UI. Three presets: Minimal, Normal, Verbose. Configurable via Tools → Log Configuration (`FormLogConfig`).
- **Log viewer**: DataGridView in FormMain.Shell.vb with category/level filter combos, text search, pause/resume scroll, color-coded rows, event description tooltips on hover.
- **Rate limiter**: Repeated events (>5 in 10s window) are collapsed with a summary line.
- **Session summary**: `AppLogger.EmitSessionSummary()` called on shutdown — logs duration, total events, errors, top 5 categories.
- **Python log routing**: `PythonSidecarHost.BaseEventId` property + level offsets (base+0=Info, +1=Debug, +2=Warning, +3=Error). Each Python server has its own base event ID range.
- **`WriteDebugLog`**: Thin forwarder on FormMain routing to `AppLogger.Log(LogEvents.LEGACY, msg)` — kept only because 4 controllers accept it as `Action(Of String)` delegate.
- **Adding a new event**: Add a constant to `LogEvents.vb`, register it in the shared `Sub New()` block with `LogEventRegistry.Register(id, category, severity, description)`.
- **Key files**: `LogEvents.vb`, `LogEventRegistry.vb`, `LogRoutingConfig.vb`, `LogCategory.vb`, `LogSeverity.vb`, `AppLogger.vb`, `FormLogConfig.vb` — all in `Services/Infrastructure/`.
- Global exception handlers in Program.vb
