# Every Tongue

Real-time speech transcription and multilingual translation for live events — listeners follow along on their own phones in their own language. Ships in two editions:

- **Windows desktop** (full) — offline-capable: whisper.cpp / Speechmatics STT, NLLB or cloud translation, GPU acceleration, operator workspaces. Built on [whisper.cpp](https://github.com/ggerganov/whisper.cpp) with Silero VAD.
- **EveryTongue Lite** (cross-platform container) — the online-only stack (cloud STT + cloud translation) as a Docker image for Windows, macOS, or Linux. The microphone is any browser, configuration is a web page, and **you bring your own API keys** (they stay on your machine — never in the image).

## Quick start — Lite (any OS with Docker)

```
docker run -d --name everytongue-lite --restart unless-stopped \
  -p 5080:5080 -p 5081:5081 \
  -e EVERYTONGUE_PUBLIC_HOST=<your-lan-ip>:5081 \
  -v ./et-config:/config \
  ghcr.io/leapfreak/everytongue-lite:latest
```

Open `https://<your-lan-ip>:5081` (accept the one-time certificate warning) → **Administrator** (default PIN `1234`) → **Settings**: pick engines (e.g. Speechmatics + Google Translate), paste your API keys, change the PIN. Host a room, tap **🎙 Broadcast Mic**, and phones that scan the room's QR receive live translations. Config, HTTPS certificate, and logs persist in `./et-config` across updates (`docker pull` + re-run).

## Download — Windows desktop

Download the latest version from the [Releases](https://github.com/Leapfreak/EveryTongue/releases) page:

- **EveryTongue_Setup_x.x.x.exe** (recommended) -- Installer with Start Menu shortcuts and uninstaller
- **EveryTongue_App_vx.x.x.zip** -- Portable version, extract and run

On first launch, the app will prompt you to download the required tools (whisper.cpp, yt-dlp, FFmpeg, Whisper model, and SubtitleEdit). This is a one-time setup that downloads everything automatically.

> **Note:** Windows SmartScreen may show a "Windows protected your PC" warning because the installer is not code-signed. Click "More info" then "Run anyway" to proceed. The source code is fully open for inspection.

## Features

**Speech-to-Text — 9 pluggable engines**
- Offline: whisper.cpp (Vulkan — any GPU, CUDA, or CPU), faster-whisper (CUDA)
- Online streaming: Speechmatics, Google Cloud STT, Deepgram, Gladia, Azure AI Speech
- Silero VAD boundary detection for offline engines; self-endpointing for streaming engines
- Speechmatics quality stack: per-speaker end-of-utterance auto-tune (adapts to speaking pace via diarization), buffer-to-pause clause merging, SaT (wtpsplit) sentence re-segmentation before translation, auto-reconnect with no audio loss
- Hallucination filtering, per-session filter sets, session statistics

**Translation — 12 pluggable engines**
- Offline: NLLB-200 1.3B / 3.3B (CTranslate2, float16 or int8) — different rooms can run different models concurrently
- Online: Google Translate, DeepL, Azure Translator, Amazon Translate, OpenAI, DeepSeek, LibreTranslate (self-hostable endpoint), Speechmatics inline
- Per-room engine selection; optional "shadow" second/third-opinion translations logged per commit for engine comparison
- Glossary corrections and profanity masking applied uniformly across offline AND cloud engines
- Usage tracking with per-engine monthly character budgets

**Rooms — multi-room translation server**
- Conference rooms (one speaker → many listeners) and conversation rooms (everyone speaks, each reads in their own language)
- **Web-Mic Broadcast**: the room microphone can be any browser — a phone on the pulpit, a laptop, no cable to the server
- Room templates with hosting codes, speaker profiles, per-room engines, display templates, QR-code joining
- Host controls from any phone: pause, clear, lock, speaker/language switching, kick
- Engine-readiness indicators so nobody speaks before the models are loaded

**Text-to-Speech — 6 pluggable engines**
- Offline: Piper, MMS-TTS · Online: Edge TTS, Azure AI Speech, Google Cloud TTS, OpenAI TTS
- Per-listener voice on phones + optional local playback to a PA/NDI output device

**System-wide Dictation (Windows)**
- Speak → text is typed into whatever app has focus (global hotkey, continuous or push-to-talk)
- Optional translate-while-dictating (speak one language, type another)
- Audio cues when the engine is genuinely capturing; own microphone selection

**Bible Integration**
- Downloadable Bibles (eBible.org); desktop reader and phone panel with verse lookup and search
- Scripture reference detection; chapter translation; biblical proper-noun vocabulary for STT boosting

**Phone Web Client**
- HTTPS + WebSocket subtitle streaming, per-client language, TTS read-aloud, transcript save
- Automatic keep-screen-on, browser-language detection, font/size/color settings
- PIN-gated admin: engine start/stop, input language, server settings (engines + API keys) and log viewer from the browser

**Batch Processing**
- **YouTube -> Subtitles** -- Download a YouTube video (or use a local file), optionally trim to a time range, and generate subtitles
- **Audio File -> Subtitles** -- Transcribe audio files (OGG, MP3, WAV, FLAC, M4A, etc.) directly into subtitles
- **YouTube -> Full Video** -- Download and trim a video without transcription
- **YouTube -> Audio Only** -- Download, trim, and extract audio to MP3
- Parallel chunk-based processing with resume support for interrupted jobs
- Multiple output formats: SRT, VTT, TXT, JSON, CSV, LRC

**Other**
- Filter Editor (glossary / profanity / hallucinations), structured event logging with configurable routing, benchmark suite (STT/translation/TTS comparison + concurrency)
- Multi-language UI with downloadable language packs (any language)
- Light/Dark/System theme support
- Centralized Download Manager for all tools, models, and optional components
- Automatic app and tool update checking via GitHub Releases
- Start with Windows option

## Requirements

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (the installer checks for this)
- NVIDIA GPU recommended for live transcription (CUDA acceleration)

All other dependencies are downloaded automatically on first launch:
- [whisper.cpp](https://github.com/ggerganov/whisper.cpp) (speech-to-text engine — batch and live)
- [NLLB-200](https://huggingface.co/JustFrederik/nllb-200-1.3B-ct2-float16) (translation model, ~2.6GB)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) (YouTube downloads)
- [FFmpeg](https://ffmpeg.org/) (audio/video processing)
- [SubtitleEdit](https://github.com/SubtitleEdit/subtitleedit) (subtitle editing)
- Whisper GGML model (ggml-large-v3, ~3GB)

## Build from Source

```bash
git clone https://github.com/Leapfreak/EveryTongue.git
cd EveryTongue
dotnet build
```

The solution has three projects: `EveryTongue.Core` (the WinForms-free server — Kestrel, rooms, engines, web client), `EveryTongue` (Windows desktop head), and `EveryTongue.Lite` (cross-platform headless console host).

To publish the desktop release build (output goes to `EveryTongue/bin/Publish`):

```bash
dotnet publish EveryTongue/EveryTongue.vbproj -c Release -r win-x64 --self-contained false
```

To build the installer (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php)):

```bash
iscc setup.iss
```

To build the Lite container image:

```bash
docker build -t everytongue-lite .
```

## Dependencies

This application uses the following open-source projects:

| Component | License | Purpose |
|-----------|---------|---------|
| [whisper.cpp](https://github.com/ggml-org/whisper.cpp) | MIT | Batch speech-to-text engine |
| [NLLB-200](https://huggingface.co/JustFrederik/nllb-200-1.3B-ct2-float16) | CC-BY-NC-4.0 | Multilingual translation (user-downloaded) |
| [NAudio](https://github.com/naudio/NAudio) | MIT | Audio capture and playback |
| [edge-tts](https://github.com/rany2/edge-tts) | GPL 3.0 | Text-to-speech synthesis |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Unlicense | Video/audio downloading |
| [FFmpeg](https://ffmpeg.org/) | LGPL/GPL | Audio/video processing |
| [SubtitleEdit](https://github.com/SubtitleEdit/subtitleedit) | GPL 3.0 | Subtitle editing (optional) |

For a complete list of all dependencies and their licenses, see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## License

This project is licensed under the **GNU General Public License v3.0**. See the [LICENSE](LICENSE) file for details.

This means you are free to use, modify, and distribute this software, provided that any derivative works are also released under the GPL-3.0 license and give appropriate credit to this project.
