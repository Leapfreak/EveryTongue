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
- "More languages..." expands to show all NLLB-supported languages (auto-translated UI)
- Selecting a language immediately applies it and proceeds to the main window
- The choice is saved in config; can be changed later via View > Language or Options > General
- If the selected language has no `.resx` yet, the editor auto-generates one via NLLB on the spot

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
  Check Dependencies          (Python venvs, NLLB, faster-whisper)
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
│  Engine: [NLLB (local) ▼]  Status: Model loaded                 │
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
│  Backend: NLLB (local) | DeepL | Google | Azure | Custom        │
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
                 YouTube model, Audio model, faster-whisper model,
                 NLLB model, output root, SubtitleEdit,
                 glossary, Bibles directory
               — yt-dlp format string
               — [Verify All Paths]
               — [Download Models...]

  Translation
    Engines    — Translation enabled toggle
               — Backend selector: NLLB / DeepL / Google / Azure / Custom
                 (Feature #14 — pluggable backends)
               — API key fields (shown per backend)
               — [Test] button
               — Fallback to NLLB when offline toggle
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
│  Auto-configured: medium whisper model, NLLB enabled            │
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
│  Translation engine: [NLLB (local, private) ▼]                  │
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
│  Translation: Enabled (NLLB, CUDA) — 4 languages               │
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
translators, backed by NLLB for auto-translation, plus a **CLI script** for developer
batch operations. Both read/write the same `.resx` files and use the same
`auto`/`human` tracking mechanism.

### Tracking: auto vs human

Each `.resx` `<data>` entry uses the `<comment>` element to track its source:

```xml
<!-- Auto-translated by NLLB — will be overwritten on next auto-generate -->
<data name="Btn_Start"><value>Iniciar</value><comment>auto</comment></data>

<!-- Human-submitted — protected from auto-overwrite -->
<data name="Btn_Stop"><value>Detener</value><comment>human</comment></data>
```

- `auto` — machine-translated via NLLB, regenerated on demand
- `human` — submitted by a native speaker, permanently locked from auto-overwrite
- (missing comment) — treated as `auto` for legacy entries on first run

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
│  [Auto-Fill Missing (NLLB)]  [Auto-Fill All (NLLB)]               │
│  [Export Template CSV...]    [Import Template CSV...]              │
│  [Reset Selected to Auto]                                         │
│                                                                   │
│                              [ Save ]  [ Close ]                  │
└───────────────────────────────────────────────────────────────────┘
```

**Editor features:**

- **Language selector** — dropdown of existing locales + "New Language" to create one from scratch
- **New Language** — prompts for locale code and display name, creates a new `.resx` with all keys
  auto-translated via NLLB in one pass
- **DataGridView** — key, English source, current translation, status indicator
- **Inline editing** — click a translation cell to edit; saving marks it `human`
- **Filter dropdown** — All / Missing only / Auto only / Human only
- **Search** — filter keys by name or English text
- **Auto-Fill Missing (NLLB)** — translates only missing/empty keys using the app's
  built-in NLLB translation service, marks them `auto`
- **Auto-Fill All (NLLB)** — retranslates all `auto` keys (leaves `human` untouched)
- **Export Template CSV** — for offline editing or sending to a volunteer
- **Import Template CSV** — loads a completed CSV, marks imported entries `human`
- **Reset Selected to Auto** — demotes selected rows back to `auto` (bad human translation)
- **Save** — writes the `.resx` file and regenerates the `app.js` i18n block

**NLLB integration:**

The editor calls the same `ITranslationService` already used for live translation.
If NLLB is loaded, translations happen instantly on-device. If not loaded, the editor
offers to load it (respecting the unload timer from Options). This means the app can
generate its own UI translations in any of the 200+ languages NLLB supports, without
any external API or internet connection.

### CLI script (developer tool)

`scripts/translate.py` provides batch operations for CI/CD and development:

| Command | What it does |
|---|---|
| `--auto` | Fill missing + refresh `auto` keys across all locales via NLLB |
| `--auto --lang ko,ar` | Same but only for specific locales |
| `--export-template es` | Generate CSV template for a volunteer |
| `--import file.csv --lang es` | Import volunteer translations, mark as `human` |
| `--reset-keys "X,Y" --lang es` | Demote specific keys back to `auto` |
| `--status` | Summary table: per-language counts of human vs auto vs missing |
| `--generate-js` | Regenerate `app.js` i18n block from all `.resx` files |

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

When a developer adds a new key to `Strings.resx` (English master):
- Opening the Localization Editor shows the key as 🔴 missing for all languages
- Clicking **Auto-Fill Missing** translates it via NLLB with `comment=auto`
- Or the CLI `--auto` run does the same in batch
- Existing `human` entries are untouched
- A translator can later edit the auto value to upgrade it to `human`

### app.js i18n generation

Both the in-app editor (on Save) and the CLI (`--generate-js`) regenerate the i18n
object in `app.js` from the `.resx` files, using a key mapping. This eliminates
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
| **Translate workspace** | STUB | Placeholder — future text-to-text (Feature #14) |
| **Bible workspace** | Implemented | BibleService + web client exist; native desktop = WebView2 |
| **Options dialog** | Implemented | VS-style tree dialog, consolidate from current tabs |
| **Log panel** | Implemented | Unify current job log + server log |
| **Session Wizard** | STUB | Shell with steps, wired to existing settings where possible |
| **QR Code window** | STUB | Menu item + toolbar button disabled until QRCoder added |
| **Audio Level Monitor** | STUB | Meter UI placeholder in Live workspace, no backend yet |
| **Hardware Score** | STUB | Options > Hardware section shows "Run scan" button, no HardwareScanner yet |
| **Diagnostics Export** | STUB | File menu item disabled |
| **Glossary Simple Mode** | STUB | FormFilterEditor toggle exists but simple mode not built |
| **Event Profiles** | STUB | Wizard step 5 "Save as profile" disabled |
| **Spec Sheet Generator** | STUB | Help menu item disabled |
| **File Integrity Check** | STUB | Tools menu item disabled |
| **Portable Mode** | STUB | Detection logic placeholder |
| **Feedback prompt** | STUB | Post-session dialog placeholder |
| **Localization Editor** | Implemented | Tools menu, DataGridView editor, NLLB auto-fill, CSV import/export |
| **First-Run Language Picker** | Implemented | Flag grid, search, auto-generates .resx via NLLB for new languages |

---

## Implementation Approach

### Phase 1: Shell + Navigation
- `FormLanguagePicker.vb` — first-run language selector with flag tiles, search, NLLB auto-generation
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
- `FormLocalizationEditor.vb` — DataGridView editor for `.resx` files, NLLB auto-fill, CSV import/export, auto/human tracking
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
- Translate workspace — wire to NLLB backend, then pluggable backends (Feature #14)
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
