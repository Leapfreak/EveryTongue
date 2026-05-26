# Kestrel Migration Plan

Enterprise-grade architecture redesign for Transcription Tools. Replaces the current HttpListener + TcpListener/SslStream server with ASP.NET Core Kestrel hosted in-process, and establishes the foundation for Bible integration, TTS, audio streaming, pluggable translation backends, and future extensibility.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Phase 1: Foundation](#phase-1-foundation--kestrel-host--dependency-injection)
- [Phase 2: WebSocket Migration](#phase-2-websocket-hub--real-time-infrastructure)
- [Phase 3: Web Client Extraction](#phase-3-web-client-extraction--static-files)
- [Phase 4: Service Abstractions](#phase-4-service-abstractions--interfaces)
- [Phase 5: Translation Backends](#phase-5-translation-backends--pluggable-architecture)
- [Phase 6: TTS Engine](#phase-6-tts-engine--tiered-speech-synthesis)
- [Phase 7: Bible Integration](#phase-7-bible-integration--sqlite)
- [Phase 8: Audio Streaming](#phase-8-audio-streaming--files--ndi)
- [Phase 9: Observability](#phase-9-observability--metrics--health)
- [Phase 10: Cutover](#phase-10-cutover--remove-legacy-server)
- [Cross-Cutting Concerns](#cross-cutting-concerns)
- [Migration Safety](#migration-safety)

---

## Architecture Overview

### Current Architecture

```
FormMain (WinForms)
  |-- SubtitleServer.vb (HttpListener + TcpListener/SslStream)
  |     |-- Manual HTTP routing (duplicated across HTTP/HTTPS)
  |     |-- Manual WebSocket upgrade + frame handling
  |     |-- ~800 lines of embedded HTML/JS/CSS in VB string literal
  |     |-- Self-signed cert via SslStream.AuthenticateAsServer
  |     |-- ConcurrentDictionary of WebSocket clients
  |     +-- History queue (ConcurrentQueue<CommittedEntry>)
  |
  |-- LiveStreamRunner.vb (manages live-server Python sidecar)
  |-- TranslationService.vb (manages nllb-server Python sidecar)
  +-- FormFilterEditor.vb (glossary editor)
```

### Target Architecture

```
FormMain (WinForms)
  |
  +-- WebApplication (Kestrel, hosted in-process on background thread)
        |
        |-- Middleware Pipeline
        |     |-- ResponseCompression (gzip/brotli)
        |     |-- StaticFiles (wwwroot/, ETags, cache headers)
        |     |-- WebSockets (permessage-deflate)
        |     |-- RequestLogging
        |     |-- ErrorHandling
        |     +-- HealthChecks
        |
        |-- Endpoints (Minimal API, defined once, served HTTP + HTTPS)
        |     |-- GET  /                           -> Static HTML client
        |     |-- WS   /ws                         -> Subtitle WebSocket hub
        |     |-- GET  /api/control                -> Admin control/status
        |     |-- GET  /api/health                 -> Health check
        |     |-- GET  /api/metrics                -> Server metrics
        |     |-- GET  /cert                       -> Self-signed cert download
        |     |-- GET  /nosleep.wav                -> Silent keepalive audio
        |     |-- POST /api/feedback               -> Listener feedback
        |     |-- POST /api/glossary-suggestion    -> Glossary suggestions
        |     |-- GET  /bible/{translation}/{book}/{chapter}[/{verse}]
        |     |-- GET  /bible/search?q=&lang=      -> Full-text search
        |     |-- GET  /tts/cache/{file}           -> Cached TTS audio clips
        |     |-- GET  /audio/stream/{id}          -> Live audio stream
        |     +-- GET  /audio/file/{path}          -> File-based audio (range requests)
        |
        |-- Services (Dependency Injection)
        |     |-- ISubtitleService          -> Client state, broadcast, history
        |     |-- ITranslationService       -> Orchestrates translation backends
        |     |     |-- NllbBackend         -> Local NLLB-200 (offline)
        |     |     |-- DeepLBackend        -> DeepL API (cloud)
        |     |     |-- GoogleBackend       -> Google Translate API (cloud)
        |     |     |-- AzureBackend        -> Azure Translator (cloud)
        |     |     +-- CustomApiBackend    -> User-defined endpoint
        |     |-- ITtsService              -> Orchestrates TTS backends
        |     |     |-- PiperBackend        -> Piper TTS (~30 European langs)
        |     |     |-- MmsTtsBackend       -> Meta MMS-TTS (1100+ langs)
        |     |     +-- EdgeTtsBackend      -> MS Edge TTS (cloud fallback)
        |     |-- IBibleService            -> SQLite Bible access + search
        |     |-- IAudioStreamService      -> File + NDI audio streaming
        |     |-- IMetricsService          -> Performance counters, thresholds
        |     |-- ICertificateService      -> Self-signed cert generation
        |     +-- ILanguageMapService      -> Cross-system language code mapping
        |
        +-- Hubs
              +-- SubtitleHub             -> WebSocket connection manager
```

### Design Principles

1. **Interface-first** — Every service has an interface. Implementations are swappable via DI.
2. **Single responsibility** — Each service does one thing. SubtitleServer.vb's 1871 lines become 10+ focused files.
3. **Define once, serve everywhere** — Routes defined once, Kestrel handles HTTP + HTTPS.
4. **Offline-first, cloud-optional** — Local backends always available. Cloud backends are opt-in.
5. **Pre-encode, broadcast many** — Messages encoded once, sent as `ReadOnlyMemory<byte>` to all clients.
6. **Backpressure over buffering** — Slow clients skip updates rather than accumulating unbounded queues.
7. **Cache at every layer** — Static files (ETag), TTS audio (per-language-per-commit), Bible chapters (client-side localStorage), compressed responses (server-side).
8. **Observable** — Health checks, structured logging, metrics endpoints. Problems visible before users report them.

---

## Project Structure

```
TranscriptionTools/
  |-- TranscriptionTools.vbproj          (+ Microsoft.AspNetCore.App framework ref)
  |-- Program.vb                         (unchanged — mutex, Application.Run)
  |
  |-- Forms/
  |     |-- FormMain.vb                  (starts Kestrel host, wires events)
  |     |-- FormMain.Designer.vb
  |     +-- FormFilterEditor.vb
  |
  |-- Server/                            (NEW — all Kestrel infrastructure)
  |     |-- KestrelHost.vb               (WebApplication builder, host lifecycle)
  |     |-- EndpointRegistration.vb      (all Minimal API route definitions)
  |     |-- Middleware/
  |     |     |-- RequestLoggingMiddleware.vb
  |     |     +-- ErrorHandlingMiddleware.vb
  |     +-- Hubs/
  |           +-- SubtitleHub.vb         (WebSocket connection manager)
  |
  |-- Services/                          (NEW — business logic, DI-registered)
  |     |-- Interfaces/
  |     |     |-- ISubtitleService.vb
  |     |     |-- ITranslationService.vb
  |     |     |-- ITranslationBackend.vb
  |     |     |-- ITtsService.vb
  |     |     |-- ITtsBackend.vb
  |     |     |-- IBibleService.vb
  |     |     |-- IAudioStreamService.vb
  |     |     |-- IMetricsService.vb
  |     |     |-- ICertificateService.vb
  |     |     +-- ILanguageMapService.vb
  |     |
  |     |-- Subtitle/
  |     |     |-- SubtitleService.vb     (client state, broadcast, history — extracted from SubtitleServer.vb)
  |     |     +-- CommittedEntry.vb      (data model — extracted from SubtitleServer.vb)
  |     |
  |     |-- Translation/
  |     |     |-- TranslationOrchestrator.vb   (routes to active backend, fallback chain)
  |     |     |-- Backends/
  |     |     |     |-- NllbBackend.vb         (wraps existing Python sidecar)
  |     |     |     |-- DeepLBackend.vb        (REST client)
  |     |     |     |-- GoogleTranslateBackend.vb
  |     |     |     |-- AzureTranslatorBackend.vb
  |     |     |     +-- CustomApiBackend.vb
  |     |     +-- LanguageMapService.vb        (whisper <-> NLLB <-> DeepL <-> Google code mapping)
  |     |
  |     |-- Tts/
  |     |     |-- TtsOrchestrator.vb           (tiered: Piper -> MMS -> Edge -> browser)
  |     |     |-- TtsCache.vb                  (ring-buffer file cache per language)
  |     |     |-- Backends/
  |     |     |     |-- PiperBackend.vb        (Python sidecar or direct ONNX)
  |     |     |     |-- MmsTtsBackend.vb       (Python sidecar, Meta MMS)
  |     |     |     +-- EdgeTtsBackend.vb      (cloud, edge-tts Python package)
  |     |     +-- Models/
  |     |           +-- TtsVoiceInfo.vb
  |     |
  |     |-- Bible/
  |     |     |-- BibleService.vb              (SQLite queries, FTS5 search)
  |     |     |-- BibleReferenceParser.vb      (multilingual ref parsing)
  |     |     +-- BibleReferenceDetector.vb    (detect refs in transcript text)
  |     |
  |     |-- Audio/
  |     |     |-- AudioStreamService.vb        (file playback + NDI capture)
  |     |     +-- NdiSource.vb                 (NDI SDK integration)
  |     |
  |     |-- Infrastructure/
  |     |     |-- CertificateService.vb        (self-signed cert — extracted from SubtitleServer.vb)
  |     |     |-- MetricsService.vb            (counters, gauges, thresholds)
  |     |     +-- PythonProcessManager.vb      (shared sidecar lifecycle management)
  |     |
  |     +-- Configuration/
  |           |-- ServerOptions.vb             (ports, cert path, compression settings)
  |           |-- TranslationOptions.vb        (backend selection, API keys, fallback chain)
  |           |-- TtsOptions.vb                (backend priority, model paths, cache size)
  |           +-- BibleOptions.vb              (database paths, available translations)
  |
  |-- Pipeline/                          (EXISTING — modified)
  |     |-- SubtitleServer.vb            (DEPRECATED — kept during migration, removed Phase 10)
  |     |-- LiveStreamRunner.vb          (modified to use PythonProcessManager)
  |     |-- TranslationService.vb        (replaced by TranslationOrchestrator, kept as NllbBackend wrapper)
  |     |-- PipelineRunner.vb            (unchanged — file-based transcription)
  |     +-- ...
  |
  |-- Models/                            (EXISTING — extended)
  |     |-- AppConfig.vb                 (+ new config sections)
  |     |-- ConfigManager.vb
  |     +-- ...
  |
  |-- wwwroot/                           (NEW — extracted web client)
  |     |-- index.html
  |     |-- css/
  |     |     +-- app.css
  |     |-- js/
  |     |     |-- app.js                 (subtitle client core)
  |     |     |-- websocket.js           (connection management)
  |     |     |-- tts-client.js          (TTS playback + voice audit)
  |     |     |-- bible-client.js        (browse, search, reference links)
  |     |     |-- admin.js               (admin panel)
  |     |     |-- i18n.js                (internationalisation strings)
  |     |     +-- compat.js              (device fingerprint, suitability score)
  |     +-- audio/
  |           +-- nosleep.wav
  |
  |-- Data/                              (NEW — data files)
  |     |-- language-codes.json          (cross-system language mapping)
  |     |-- compatibility.json           (device compatibility database)
  |     +-- specs.json                   (hardware tier definitions)
  |
  +-- bibles/                            (NEW — SQLite Bible databases)
        |-- en/
        |     +-- kjv.db
        |-- es/
        |     +-- rva.db
        +-- ...
```

---

## Phase 1: Foundation — Kestrel Host + Dependency Injection

**Goal:** Stand up Kestrel alongside the existing SubtitleServer on different ports. Prove the host works inside WinForms. Establish the DI container and middleware pipeline.

### 1a. Project File Changes

```xml
<!-- TranscriptionTools.vbproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <!-- ... existing properties ... -->
  </PropertyGroup>

  <!-- ADD: ASP.NET Core framework reference (already in .NET 8 runtime) -->
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <!-- ADD: SQLite for Bible integration -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.*" />
  </ItemGroup>

  <!-- ... existing items ... -->
</Project>
```

No new runtime download. `Microsoft.AspNetCore.App` ships with every .NET 8 installation.

### 1b. KestrelHost.vb — Host Lifecycle

```
Responsibilities:
  - Build WebApplication with Kestrel options
  - Configure ports (HTTP + HTTPS) with self-signed cert
  - Register all services in DI container
  - Configure middleware pipeline
  - Start/stop host on background thread
  - Bridge Kestrel's ILogger to FormMain's log output
  - Expose IServiceProvider for FormMain to resolve services
```

Key design decisions:
- Kestrel runs on a **background thread** via `Task.Run()`. WinForms stays on the UI thread.
- `WebApplication.CreateBuilder()` with `builder.WebHost.UseKestrel()`
- Host lifetime tied to `CancellationToken` from FormMain
- Services registered as **singletons** (shared state across requests)
- `ISubtitleService` is the primary bridge — FormMain calls it to broadcast, Kestrel endpoints read from it

### 1c. Middleware Pipeline Order

```
Request
  |-> ErrorHandlingMiddleware        (catch-all, structured error responses)
  |-> RequestLoggingMiddleware       (request timing, client info)
  |-> ResponseCompression            (gzip/brotli — biggest single bandwidth win)
  |-> StaticFiles                    (wwwroot/, ETags, cache-control headers)
  |-> WebSockets                     (upgrade negotiation, permessage-deflate)
  |-> Routing                        (Minimal API endpoint matching)
  |-> Endpoints                      (handler execution)
Response
```

### 1d. Compression Configuration

```
Compression targets:
  - HTML responses:     ~150KB -> ~25KB (83% reduction)
  - JSON API responses: ~70% reduction typical
  - WebSocket frames:   permessage-deflate (~70-80% for repetitive JSON)
  - Static JS/CSS:      pre-compressed at build time (brotli)

Excluded from compression:
  - Audio streams (already compressed: Opus/MP3)
  - Binary WebSocket frames (audio data)
  - Small responses < 1KB (overhead exceeds savings)
  - /cert endpoint (DER binary)
  - /nosleep.wav (tiny, already minimal)
```

### 1e. DI Registration

```
Services registered (singleton unless noted):

Core:
  ISubtitleService        -> SubtitleService         (client state, broadcast)
  ICertificateService     -> CertificateService      (cert generation/loading)
  ILanguageMapService     -> LanguageMapService       (cross-system code mapping)
  IMetricsService         -> MetricsService           (counters, health)

Translation:
  ITranslationService     -> TranslationOrchestrator  (backend routing)
  ITranslationBackend     -> NllbBackend              (registered as keyed service)
  ITranslationBackend     -> DeepLBackend             (registered as keyed service)
  ... etc

TTS:
  ITtsService             -> TtsOrchestrator           (tiered fallback)
  ITtsBackend             -> PiperBackend              (keyed)
  ITtsBackend             -> MmsTtsBackend             (keyed)
  ITtsBackend             -> EdgeTtsBackend            (keyed)

Data:
  IBibleService           -> BibleService              (SQLite access)
  IAudioStreamService     -> AudioStreamService        (file + NDI)

Infrastructure:
  PythonProcessManager    -> PythonProcessManager      (shared sidecar lifecycle)
  IOptions<ServerOptions>
  IOptions<TranslationOptions>
  IOptions<TtsOptions>
  IOptions<BibleOptions>
```

### 1f. Verification

- Kestrel starts on ports 5082 (HTTP) / 5083 (HTTPS) — temporary, alongside existing server
- `GET /api/health` returns `{"status":"healthy","version":"1.4.0"}`
- Existing SubtitleServer continues on 5080/5081 undisturbed
- FormMain starts both servers, routes traffic to old server
- Compression measurable via response headers (`Content-Encoding: br`)

---

## Phase 2: WebSocket Hub + Real-Time Infrastructure

**Goal:** Migrate WebSocket handling from SubtitleServer to Kestrel. This is the core of the app — subtitle delivery to phones.

### 2a. SubtitleHub.vb — Connection Manager

```
Responsibilities:
  - Accept WebSocket upgrades on /ws
  - Track clients in ConcurrentDictionary<string, ClientConnection>
  - Parse incoming messages (setLanguage, setInputLanguage, ping)
  - Broadcast to clients (update, commit, clear)
  - History replay on reconnect (lastId-based)
  - Backpressure: skip-on-busy (Interlocked pattern, carried from existing code)
  - Client cleanup on disconnect
  - Raise events for FormMain integration
```

### 2b. ClientConnection Model

```
ClientConnection:
  - Id: string (GUID)
  - WebSocket: WebSocket
  - Language: string (translation target, empty = original)
  - UserAgent: string
  - RemoteEndpoint: string
  - DeviceFingerprint: DeviceInfo (NEW — for compatibility tracking)
  - SendBusy: int (Interlocked flag, 0 = ready, 1 = in-flight)
  - ConnectedAt: DateTime
  - LastMessageAt: DateTime
  - MessagesSent: long (counter for metrics)
  - MessagesDropped: long (counter for metrics)
```

### 2c. Broadcast Optimisation — Pre-Encode Once

Current: each broadcast iterates clients and serialises JSON per-send.
Target: serialise once, send `ReadOnlyMemory<byte>` to all.

```
Broadcast flow:
  1. Create message object (CommitMessage, UpdateMessage, etc.)
  2. Serialise to JSON bytes ONCE -> ReadOnlyMemory<byte>
  3. For translated commits: pre-encode per-language variants
  4. Fan out to clients:
     - For each client, select the correct pre-encoded variant
     - TrySend via Interlocked gate (same backpressure as current)
     - Fire-and-forget Task for non-blocking sends
  5. Metrics: increment sent/dropped counters
```

### 2d. History Management

```
Changes from current:
  - Cap _committedLines at 500 entries (configurable via ServerOptions)
  - When cap reached, dequeue oldest
  - Replay sends only entries with id > client's lastId
  - Cap replay to most recent 200 even if lastId is very old
  - Pre-encode history entries on commit (not on replay)
```

### 2e. Event Bridge — Kestrel <-> FormMain

The SubtitleHub needs to raise events that FormMain handles on the UI thread.

```
Pattern:
  SubtitleHub runs on Kestrel's thread pool
  FormMain subscribes to ISubtitleService events
  Events use SynchronizationContext.Post() to marshal to UI thread
  Same event signatures as current SubtitleServer:
    - StatusChanged(sender, message)
    - RemoteCommand(sender, command)
    - ActiveLanguagesChanged(sender, EventArgs)
    - InputLanguageChanged(sender, language)
    - LogMessage(sender, message)
```

### 2f. Verification

- Phone connects to new Kestrel WebSocket, receives subtitles
- History replay works on reconnect
- Translation language selection works per-client
- Admin panel commands flow through to FormMain
- Client count updates in UI
- Backpressure: busy clients skip interim updates without blocking

---

## Phase 3: Web Client Extraction + Static Files

**Goal:** Extract the ~800 lines of HTML/JS/CSS from `GetHtmlPage()` into separate files in `wwwroot/`. Enable browser caching and proper development workflow.

### 3a. File Extraction

```
SubtitleServer.vb GetHtmlPage() lines 1106-1769
  -> wwwroot/index.html          (HTML structure, meta tags)
  -> wwwroot/css/app.css         (all inline styles)
  -> wwwroot/js/app.js           (subtitle rendering, scroll, fonts)
  -> wwwroot/js/websocket.js     (connect, reconnect, message parsing)
  -> wwwroot/js/admin.js         (admin panel, polling, commands)
  -> wwwroot/js/i18n.js          (8 languages, string lookup)
  -> wwwroot/audio/nosleep.wav   (silent keepalive)
```

### 3b. Dynamic Configuration Injection

Currently `GetHtmlPage()` injects `{{BG_COLOR}}` and `{{FG_COLOR}}` into the HTML string.

With static files, inject config via a lightweight API endpoint:

```
GET /api/config -> {
  "bgColor": "#000000",
  "fgColor": "#FFFFFF",
  "wsUrl": "wss://192.168.1.5:5081/ws",
  "httpsEnabled": true,
  "version": "1.4.0"
}
```

Client fetches on load, applies settings. Keeps HTML fully static and cacheable.

### 3c. Cache Strategy

```
Static files (JS/CSS):
  - Filename includes version hash: app.v1.4.0.js
  - Cache-Control: public, max-age=31536000 (1 year)
  - Browser caches indefinitely, re-downloads only on version change

index.html:
  - Cache-Control: no-cache (always revalidated)
  - ETag for conditional GET (304 Not Modified if unchanged)
  - References versioned JS/CSS files

API responses:
  - Cache-Control: no-store (real-time data)
  - Compressed via middleware
```

### 3d. JS Modernisation Decision

Current constraint: ES5 for old phone compatibility.

With extracted files, options open up:
- **Keep ES5** for maximum compatibility (safest for field deployment)
- **Or** add a simple build step (esbuild, 1 command) to transpile modern JS -> ES5
- Decision deferred — extract first, modernise later if needed

### 3e. Verification

- Phone loads page from Kestrel static files
- Browser DevTools shows cached JS/CSS (304 on refresh)
- Response sizes ~80% smaller with compression
- BgColor/FgColor still configurable by operator
- All i18n strings still work
- Admin panel functional

---

## Phase 4: Service Abstractions + Interfaces

**Goal:** Define the interface contracts for all services. Build the abstractions before the implementations.

### 4a. ITranslationBackend

```vb
Public Interface ITranslationBackend
    ReadOnly Property Name As String
    ReadOnly Property RequiresInternet As Boolean
    ReadOnly Property IsAvailable As Boolean

    Function TranslateAsync(text As String,
                            sourceLang As String,
                            targetLangs As IReadOnlyList(Of String),
                            ct As CancellationToken
    ) As Task(Of Dictionary(Of String, String))

    Function GetSupportedLanguagesAsync(ct As CancellationToken
    ) As Task(Of IReadOnlyList(Of LanguageInfo))

    Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
End Interface
```

### 4b. ITranslationService (Orchestrator)

```vb
Public Interface ITranslationService
    Function TranslateAsync(text As String,
                            sourceLang As String,
                            targetLangs As IReadOnlyList(Of String),
                            ct As CancellationToken
    ) As Task(Of Dictionary(Of String, String))

    ReadOnly Property ActiveBackend As String
    ReadOnly Property FallbackBackend As String
    Function GetAllBackends() As IReadOnlyList(Of BackendInfo)
    Sub SetActiveBackend(name As String)
End Interface
```

### 4c. ITtsBackend

```vb
Public Interface ITtsBackend
    ReadOnly Property Name As String
    ReadOnly Property RequiresInternet As Boolean
    ReadOnly Property Priority As Integer  ' Lower = preferred

    Function SynthesiseAsync(text As String,
                             language As String,
                             ct As CancellationToken
    ) As Task(Of TtsResult)
    ' TtsResult: { AudioData As Byte(), Codec As String, DurationMs As Integer }

    Function GetSupportedLanguagesAsync(ct As CancellationToken
    ) As Task(Of IReadOnlyList(Of String))

    Function IsLanguageSupportedAsync(language As String,
                                      ct As CancellationToken
    ) As Task(Of Boolean)

    Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
End Interface
```

### 4d. ITtsService (Orchestrator)

```vb
Public Interface ITtsService
    Function SynthesiseAsync(text As String,
                             language As String,
                             commitId As Integer,
                             ct As CancellationToken
    ) As Task(Of String)
    ' Returns URL path to cached audio: /tts/cache/fra_commit_42.opus

    Function GetAvailableVoicesAsync(language As String,
                                     ct As CancellationToken
    ) As Task(Of IReadOnlyList(Of TtsVoiceInfo))

    ReadOnly Property CacheStats As TtsCacheStats
End Interface
```

### 4e. IBibleService

```vb
Public Interface IBibleService
    Function GetTranslationsAsync(language As String,
                                  ct As CancellationToken
    ) As Task(Of IReadOnlyList(Of BibleTranslation))

    Function GetChapterAsync(translationId As String,
                             book As String,
                             chapter As Integer,
                             ct As CancellationToken
    ) As Task(Of BibleChapter)

    Function GetVersesAsync(translationId As String,
                            book As String,
                            chapter As Integer,
                            verseStart As Integer,
                            Optional verseEnd As Integer = -1,
                            Optional ct As CancellationToken = Nothing
    ) As Task(Of IReadOnlyList(Of BibleVerse))

    Function SearchAsync(query As String,
                         translationId As String,
                         Optional maxResults As Integer = 50,
                         Optional ct As CancellationToken = Nothing
    ) As Task(Of IReadOnlyList(Of BibleSearchResult))

    Function ParseReferenceAsync(reference As String,
                                 Optional language As String = "en",
                                 Optional ct As CancellationToken = Nothing
    ) As Task(Of BibleReference)

    Function DetectReferencesInText(text As String
    ) As IReadOnlyList(Of DetectedReference)
End Interface
```

### 4f. IAudioStreamService

```vb
Public Interface IAudioStreamService
    Function GetFileStreamAsync(filePath As String,
                                rangeStart As Long,
                                rangeEnd As Long,
                                ct As CancellationToken
    ) As Task(Of AudioStreamResult)
    ' Supports range requests for seeking in browser audio player

    Function GetNdiSourcesAsync(ct As CancellationToken
    ) As Task(Of IReadOnlyList(Of NdiSourceInfo))

    Function StartNdiCaptureAsync(sourceName As String,
                                  ct As CancellationToken
    ) As Task(Of String)
    ' Returns stream ID for /audio/stream/{id}

    Sub StopNdiCapture(streamId As String)
End Interface
```

### 4g. ILanguageMapService

```vb
Public Interface ILanguageMapService
    Function WhisperToNllb(whisperCode As String) As String
    Function NllbToWhisper(nllbCode As String) As String
    Function ToDeepL(code As String) As String
    Function ToGoogle(code As String) As String
    Function ToAzure(code As String) As String
    Function ToDisplayName(code As String) As String
    Function ToNativeName(code As String) As String
    Function GetAllLanguages() As IReadOnlyList(Of LanguageInfo)
End Interface
```

Loaded from `Data/language-codes.json`. Single source of truth for all language code conversions across every subsystem.

---

## Phase 5: Translation Backends — Pluggable Architecture

**Goal:** Refactor translation from a single NLLB sidecar into a pluggable backend system with fallback chain.

### 5a. NllbBackend

Wraps the existing `TranslationService.vb` / Python sidecar. No functional change — just implements `ITranslationBackend`.

```
NllbBackend:
  - Manages nllb-server/server.py Python process (via PythonProcessManager)
  - Translates via POST http://127.0.0.1:5090/translate
  - Glossary applied server-side (existing behaviour)
  - RequiresInternet = False
  - Always available as offline fallback
```

### 5b. Cloud Backends (DeepL, Google, Azure)

```
Each cloud backend:
  - Simple HttpClient REST calls
  - API key stored encrypted in AppConfig
  - Language code translation via ILanguageMapService
  - Configurable timeout (default 5s for real-time use)
  - Usage tracking: characters translated per session / per month
  - Rate limit handling: backoff + switch to fallback
```

### 5c. TranslationOrchestrator — Fallback Chain

```
Default chain:
  1. Active backend (user-selected, e.g. DeepL)
  2. If fails/unavailable -> NllbBackend (always available offline)

Per-language override:
  - User can set: "Use DeepL for French, NLLB for everything else"
  - Stored in TranslationOptions.LanguageOverrides dictionary

Hybrid mode:
  - Cloud for languages it supports well
  - NLLB for languages cloud doesn't cover (e.g. Catalan on some APIs)
  - Automatic based on GetSupportedLanguages() intersection
```

### 5d. Glossary Integration

All backends (including cloud) pass through the existing glossary post-processing:
- Cloud API returns translation
- Glossary wrong->right replacements applied
- Same corrections regardless of backend
- Future: DeepL's built-in glossary API as optimisation (bypasses post-processing for DeepL)

---

## Phase 6: TTS Engine — Tiered Speech Synthesis

**Goal:** Server-side TTS with tiered fallback. Generate audio once per language per commit, cache and serve via Kestrel static files.

### 6a. TTS Flow

```
Committed text arrives ("God loves you")
  |
  TtsOrchestrator.SynthesiseAsync("God loves you", "fra_Latn", commitId=42)
  |
  |-> Is Piper voice available for fra?
  |     YES -> PiperBackend.SynthesiseAsync() -> opus bytes
  |     NO  -> Is MMS-TTS model available for fra?
  |         YES -> MmsTtsBackend.SynthesiseAsync() -> opus bytes
  |         NO  -> Is Edge TTS enabled + internet available?
  |             YES -> EdgeTtsBackend.SynthesiseAsync() -> opus bytes
  |             NO  -> Return Nothing (client falls back to browser speechSynthesis)
  |
  TtsCache.Store(language="fra", commitId=42, audioBytes, codec="opus")
  |
  Returns "/tts/cache/fra_commit_42.opus"
```

### 6b. TTS Cache (Ring Buffer)

```
TtsCache:
  - Directory: {AppData}/tts-cache/ (or relative in portable mode)
  - File naming: {lang}_commit_{id}.{codec}
  - Max entries per language: 200 (configurable)
  - Eviction: delete oldest when limit reached
  - Served via Kestrel static file middleware (automatic ETag, range requests)
  - Cleanup on session end or app shutdown
```

### 6c. PiperBackend

```
Options:
  A) Python sidecar (piper-tts package) — consistent with existing pattern
  B) Direct ONNX Runtime from .NET — eliminates a Python process

Recommendation: Option A initially (faster to implement, consistent pattern).
Migrate to Option B later for efficiency if needed.

Model management:
  - Models stored in ./tts-models/piper/{lang}/
  - ~15-50MB per language per voice
  - Download on demand or pre-bundle for USB deployment
  - Voice selection: prefer "medium" quality, fall back to "low"
```

### 6d. MmsTtsBackend

```
Python sidecar (same pattern as NLLB):
  - FastAPI server wrapping facebook/mms-tts-{lang}
  - One model loaded at a time (swap on language change) OR
  - Pool of recently-used models (LRU cache, configurable max)
  - GPU inference via PyTorch
  - Endpoint: POST /synthesise {text, language} -> WAV bytes
  - Server-side Opus encoding (via ffmpeg or opusenc)
```

### 6e. EdgeTtsBackend

```
Python wrapper (edge-tts package):
  - No API key needed (free, uses MS Edge's read-aloud API)
  - 400+ voices, 100+ languages
  - Async streaming — audio available before full synthesis
  - RequiresInternet = True
  - Best quality of the three for most languages
  - Fallback when local models aren't available
```

### 6f. Audio Codec Strategy

```
Primary: Opus (best quality per byte)
  - Encode at 24kbps mono — excellent for speech
  - ~15KB per 5-second sentence
  - Supported: Chrome, Firefox, Edge, Safari 15+

Fallback: MP3 (universal)
  - Encode at 32kbps mono
  - ~20KB per 5-second sentence
  - For older Safari/iOS devices

Client sends codec support in WebSocket handshake:
  {type: "setLanguage", language: "fra", codecs: ["opus", "mp3"]}

Commit message includes TTS URL:
  {type: "commit", text: "...", id: 42, tts: "/tts/cache/fra_commit_42.opus"}
```

### 6g. Bandwidth Impact (100 clients, 6 languages)

```
Without TTS dedup:   100 clients x 15KB/sentence = 1.5MB per sentence burst
With per-lang dedup: 6 languages x 15KB/sentence = 90KB per sentence burst

Per-language dedup is critical. TtsCache serves the same file to all clients
requesting the same language — Kestrel handles this natively via static files.
```

---

## Phase 7: Bible Integration — SQLite

**Goal:** Multilingual Bible access with full-text search, reference parsing, and auto-detection in transcripts.

### 7a. Database Schema

```sql
-- Per-translation SQLite database: bibles/{lang}/{translation_id}.db

CREATE TABLE metadata (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
-- Keys: translation_id, language, name, license, copyright, source

CREATE TABLE books (
    abbrev TEXT PRIMARY KEY,       -- OSIS: "GEN", "MAT", "REV"
    name TEXT NOT NULL,            -- Localised: "Genesis", "Genese", etc.
    testament TEXT NOT NULL,       -- "OT" / "NT"
    sort_order INTEGER NOT NULL,
    chapters INTEGER NOT NULL      -- Total chapters in this book
);

CREATE TABLE verses (
    book TEXT NOT NULL,
    chapter INTEGER NOT NULL,
    verse INTEGER NOT NULL,
    text TEXT NOT NULL,
    PRIMARY KEY (book, chapter, verse)
);

-- Full-text search (FTS5)
CREATE VIRTUAL TABLE verses_fts USING fts5(
    text,
    content=verses,
    content_rowid=rowid,
    tokenize='unicode61 remove_diacritics 2'
);
```

### 7b. BibleService Implementation

```
BibleService:
  - Scans bibles/ directory on startup, registers available translations
  - Opens SQLite connections per-translation (connection pooling via Microsoft.Data.Sqlite)
  - Read-only mode (no writes needed)
  - FTS5 search with snippet highlighting
  - Reference parser handles multilingual book names + abbreviations
  - Reference detector uses regex patterns for chapter:verse in transcribed text
```

### 7c. API Endpoints

```
GET /bible/translations                         -> list all available
GET /bible/translations/{lang}                  -> available for a language
GET /bible/{id}/books                           -> book list
GET /bible/{id}/{book}/{chapter}                -> full chapter
GET /bible/{id}/{book}/{chapter}/{verse}        -> single verse
GET /bible/{id}/{book}/{chapter}/{v1}-{v2}      -> verse range
GET /bible/search?q={query}&translation={id}    -> FTS5 search
GET /bible/parse?ref={ref}&lang={lang}          -> parse human reference

All responses: JSON, compressed, Cache-Control varies:
  - Translation/book lists: cache 1 hour (rarely change)
  - Chapter/verse content: cache indefinitely (Bible text never changes)
  - Search results: no-cache (query-dependent)
```

### 7d. Transcript Integration

```
When a commit message is broadcast:
  1. BibleReferenceDetector scans text for patterns
  2. If references found, enrich the WebSocket message:
     {
       type: "commit",
       text: "As Paul says in Romans 8:28...",
       id: 42,
       bibleRefs: [
         {ref: "ROM.8.28", display: "Romans 8:28", start: 22, end: 34}
       ]
     }
  3. Phone client renders detected refs as tappable links
  4. Tapping fetches /bible/parse?ref=ROM.8.28&lang=fra -> French Bible text
```

---

## Phase 8: Audio Streaming — Files + NDI

**Goal:** Stream audio from local files (recorded sessions, music) and NDI network sources through Kestrel with proper HTTP range request support.

### 8a. File-Based Audio Streaming

```
Endpoint: GET /audio/file/{path}

Features:
  - HTTP Range requests (Accept-Ranges: bytes) for seeking in browser player
  - Content-Type detection (audio/mpeg, audio/ogg, audio/wav, etc.)
  - Chunked transfer for large files
  - Cache headers (ETag based on file modified date + size)
  - Path validation (prevent directory traversal — whitelist allowed directories)

Use cases:
  - Replay recorded sermons with subtitle overlay
  - Play pre-recorded TTS audio files
  - Serve music/worship audio tracks
```

### 8b. NDI Source Integration

```
NDI (Network Device Interface) — industry standard for AV over IP.

Endpoint: GET /audio/stream/{streamId} (chunked/SSE)

Flow:
  1. GET /audio/ndi/sources -> list available NDI sources on network
  2. POST /audio/ndi/capture {sourceName} -> starts capture, returns streamId
  3. GET /audio/stream/{streamId} -> chunked audio stream (Opus-encoded)
  4. DELETE /audio/ndi/capture/{streamId} -> stops capture

Implementation:
  - NDI SDK (NewTek/Vizrt) via P/Invoke or managed wrapper
  - Audio frames captured, encoded to Opus, streamed via chunked response
  - Multiple clients can consume same stream (single capture, fan-out)
  - Automatic source discovery via NDI's mDNS
```

### 8c. Security

```
Audio endpoints restricted:
  - File paths validated against configurable whitelist directories
  - NDI capture requires admin panel authentication (future)
  - Rate limiting on stream creation
  - Stream auto-timeout after configurable idle period
```

---

## Phase 9: Observability — Metrics + Health

**Goal:** Enterprise-grade monitoring. Know when the system is struggling before users notice.

### 9a. Health Check Endpoint

```
GET /api/health -> {
  "status": "healthy" | "degraded" | "unhealthy",
  "checks": {
    "kestrel": "healthy",
    "liveServer": "healthy",
    "translationServer": "healthy",
    "ttsServer": "healthy",
    "bibleDatabase": "healthy"
  },
  "uptime": "02:34:15",
  "version": "1.4.0"
}

Uses ASP.NET Core's built-in IHealthCheck framework.
Each service implements IHealthCheck for its own component.
```

### 9b. Metrics Endpoint

```
GET /api/metrics -> {
  "clients": {
    "connected": 47,
    "byLanguage": {"fra_Latn": 12, "deu_Latn": 8, ...}
  },
  "broadcast": {
    "latencyMs": 12,
    "messagesSent": 14502,
    "messagesDropped": 3
  },
  "translation": {
    "activeBackend": "DeepL",
    "latencyMs": 180,
    "charactersThisSession": 52400
  },
  "tts": {
    "cacheHitRate": 0.92,
    "generationsThisSession": 340,
    "cacheSize": "48MB"
  },
  "system": {
    "cpuPercent": 32,
    "memoryMB": 820,
    "gpuPercent": 68,
    "bandwidthMbps": 1.2
  }
}
```

### 9c. Threshold Alerts

```
Configurable thresholds (via ServerOptions):

| Metric               | Warning  | Critical |
|----------------------|----------|----------|
| Connected clients    | 75       | 95       |
| CPU usage            | 70%      | 90%      |
| GPU usage            | 80%      | 95%      |
| Broadcast latency    | 100ms    | 500ms    |
| Messages dropped/min | 10       | 50       |
| Transcription lag    | 3s       | 8s       |
| Translation lag      | 2s       | 5s       |
| Memory usage         | 2GB      | 3.5GB    |

Alerts surface via:
  - IMetricsService events -> FormMain status bar
  - /api/health degrades from "healthy" to "degraded"/"unhealthy"
  - Structured log entries
```

### 9d. Structured Logging

```
Replace ad-hoc string logging with ILogger<T>:
  - Request logging (path, method, status, duration)
  - WebSocket events (connect, disconnect, language change)
  - Translation events (backend used, latency, fallback triggered)
  - TTS events (cache hit/miss, generation time, codec)
  - Health state changes
  - Threshold alerts

Output: existing dated log files (YYYYMMDD_pipeline-debug.log)
Format: structured but human-readable (not JSON — operator needs to read these)
```

---

## Phase 10: Cutover — Remove Legacy Server

**Goal:** Remove SubtitleServer.vb's HttpListener/TcpListener code entirely. Kestrel is the sole server.

### 10a. Cutover Checklist

```
Before removing legacy code, verify ALL of these:

Endpoints:
  [ ] GET /              serves static HTML (from wwwroot/)
  [ ] WS /ws             WebSocket subtitle delivery
  [ ] GET /api/control   admin panel actions + status polling
  [ ] GET /cert          self-signed cert download
  [ ] GET /nosleep.wav   silent keepalive audio
  [ ] GET /api/health    health check
  [ ] GET /api/metrics   server metrics
  [ ] GET /api/config    client configuration (bg/fg color, ws URL)

WebSocket messages (server -> client):
  [ ] {type:"update"}    interim text
  [ ] {type:"commit"}    committed text + translations
  [ ] {type:"clear"}     clear display
  [ ] {type:"pong"}      keepalive response

WebSocket messages (client -> server):
  [ ] {type:"setLanguage"}       translation language
  [ ] {type:"setInputLanguage"}  input language (admin)
  [ ] {type:"ping"}              keepalive

Behaviour:
  [ ] History replay on reconnect (lastId-based)
  [ ] Per-client language routing
  [ ] Backpressure (skip-on-busy)
  [ ] Client connect/disconnect events to FormMain
  [ ] Remote command forwarding to FormMain
  [ ] Active languages tracking
  [ ] Compression active on all HTTP responses
  [ ] Static files cached with ETags
  [ ] HTTPS with self-signed cert (same cert, Kestrel options)
  [ ] Ports configurable (default 5080/5081)
  [ ] localhost-only fallback if remote binding fails

Integration:
  [ ] FormMain.StartSubtitleServer() uses KestrelHost
  [ ] All event handlers migrated
  [ ] BroadcastUpdate/Commit/Clear called from FormMain work correctly
  [ ] GetActiveTranslationLanguages() returns correct data
  [ ] Admin panel fully functional from phone
  [ ] Wake Lock works over HTTPS
  [ ] Cert acceptance overlay still shown for self-signed cert
```

### 10b. Removal

```
Delete:
  - SubtitleServer.vb HttpListener code
  - SubtitleServer.vb TcpListener/SslStream code
  - SubtitleServer.vb GetHtmlPage() method (HTML now in wwwroot/)
  - SubtitleServer.vb manual HTTP routing
  - SubtitleServer.vb manual WebSocket upgrade code

Keep (refactored into new locations):
  - Broadcast logic          -> SubtitleService.vb
  - Client tracking          -> SubtitleHub.vb
  - CommittedEntry model     -> CommittedEntry.vb
  - Cert generation          -> CertificateService.vb
  - Language mapping         -> LanguageMapService.vb
  - Event definitions        -> ISubtitleService.vb
```

### 10c. Port Takeover

Once legacy server is removed:
- Kestrel moves from temporary ports (5082/5083) to production ports (5080/5081)
- No change for phone clients — same URLs
- Update ServerOptions defaults

---

## Cross-Cutting Concerns

### Configuration Management

```
IOptions<T> pattern for all configuration:

ServerOptions:
  - HttpPort (5080)
  - HttpsPort (5081)
  - CertificatePath
  - MaxClients (100)
  - HistoryCap (500)
  - AllowRemote (true)

TranslationOptions:
  - ActiveBackend ("nllb")
  - FallbackBackend ("nllb")
  - LanguageOverrides (Dictionary)
  - DeepLApiKey (encrypted)
  - GoogleApiKey (encrypted)
  - AzureKey (encrypted)
  - AzureRegion
  - CustomEndpoint
  - MonthlyCharacterBudget
  - TimeoutMs (5000)

TtsOptions:
  - Enabled (true)
  - BackendPriority (["piper", "mms", "edge"])
  - PiperModelPath ("./tts-models/piper/")
  - MmsModelPath ("./tts-models/mms/")
  - EdgeTtsEnabled (true)
  - PreferredCodec ("opus")
  - CacheMaxPerLanguage (200)
  - CachePath

BibleOptions:
  - DatabasePath ("./bibles/")
  - EnableReferenceDetection (true)
  - EnableSearch (true)

All stored in existing AppConfig.json via ConfigManager.
Loaded on startup, reloadable without restart where possible.
```

### Error Handling Strategy

```
Layers:
  1. ErrorHandlingMiddleware — catch-all for unhandled exceptions
     -> Returns JSON error response (no stack traces in production)
     -> Logs full exception to pipeline log

  2. Per-endpoint try/catch — business logic errors
     -> Returns appropriate HTTP status (400, 404, 500)
     -> Structured error response: {error: "...", code: "..."}

  3. Service-level resilience
     -> Translation: fallback to next backend on failure
     -> TTS: fallback to next backend, then to "no TTS" gracefully
     -> Bible: return empty results, not errors
     -> WebSocket: drop message on send failure, don't crash broadcast loop

  4. Python sidecar management (PythonProcessManager)
     -> Auto-restart on crash with exponential backoff
     -> Max 5 restarts, then surface error to operator
     -> Health check polling (GET /health every 10s)
```

### Security

```
Principles:
  - No secrets in source code (API keys encrypted in AppConfig)
  - Path traversal prevention on all file-serving endpoints
  - Input validation on all API parameters
  - WebSocket message size limits
  - Rate limiting on expensive endpoints (search, TTS generation)
  - Self-signed cert for HTTPS (same approach as current)
  - No authentication needed (local network, trusted environment)
  - Admin panel access implicit (same as current — no auth, local use)
```

### Testability

```
Interface-first design enables:
  - Unit testing services with mock backends
  - Integration testing endpoints with WebApplicationFactory
  - Backend swapping in tests (mock translation, mock TTS)
  - Health check testing

Not in scope for initial migration:
  - Full test suite (solo developer context — ship working code, test live)
  - CI test pipeline
  - But the architecture SUPPORTS adding tests later without refactoring
```

---

## Migration Safety

### Parallel Running

During phases 1-9, BOTH servers run simultaneously:
- Legacy SubtitleServer on ports 5080/5081 (production traffic)
- New Kestrel server on ports 5082/5083 (testing)
- FormMain routes to legacy by default
- Toggle switch (debug/settings) to route to Kestrel for testing
- Phone can manually connect to either by changing port

### Rollback

At any point before Phase 10:
- Stop Kestrel host
- Legacy server continues working unchanged
- No code deleted until Phase 10 cutover
- Branch can be abandoned without affecting main

### Data Compatibility

- Same WebSocket JSON protocol — phones don't need updates
- Same commit history format
- Same config file (AppConfig.json extended, not replaced)
- Same cert file (reused by Kestrel)
- Same log file format

### Incremental Delivery

Each phase is independently testable and deployable:
- Phase 1: Kestrel starts, health endpoint works
- Phase 2: WebSocket works on new server
- Phase 3: Static files served, compressed
- Phase 4: Interfaces defined (no runtime change)
- Phase 5: Translation backends pluggable
- Phase 6: TTS generates audio
- Phase 7: Bible data served
- Phase 8: Audio streaming works
- Phase 9: Metrics visible
- Phase 10: Legacy removed

The congregation can be switched to Kestrel at any point after Phase 3 (once WebSocket + static files are proven). Phases 4-9 add features on top.

---

## Estimated Scope

| Phase | New Files | Modified Files | Complexity |
|-------|-----------|----------------|------------|
| 1. Foundation | 4-5 | 2 | Medium |
| 2. WebSocket Hub | 3-4 | 2 | High (core logic) |
| 3. Web Client Extraction | 8-10 | 1 | Medium (tedious) |
| 4. Service Interfaces | 10-12 | 0 | Low (contracts only) |
| 5. Translation Backends | 6-8 | 2 | Medium |
| 6. TTS Engine | 6-8 | 2 | High |
| 7. Bible Integration | 4-5 | 1 | Medium |
| 8. Audio Streaming | 3-4 | 1 | Medium-High |
| 9. Observability | 3-4 | 2 | Medium |
| 10. Cutover | 0 | 3 (deletions) | Low (verification) |

Phases 1-3 are the critical path. Once those are solid, phases 4-9 can be built in any order.
