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

**Live Transcription**
- Real-time speech-to-text using whisper.cpp with Silero VAD for natural speech boundary detection
- GPU-accelerated inference via Vulkan (all GPUs) or CUDA (NVIDIA)
- Intelligent commit system: VAD-commit on pauses, sentence-commit at boundaries, force-commit safety valve
- Hallucination filtering with consecutive-skip buffer management
- Adjustable VAD silence threshold and max segment duration (live tuning via sliders)
- Session statistics with commit type breakdown, speaking rate, and silence gap analysis

**Live Translation (NLLB-200)**
- Real-time translation of transcribed text using NLLB-200 1.3B (CTranslate2)
- Per-client language selection on phone subtitle display
- Glossary system for domain-specific corrections (biblical/church terminology)
- Profanity filter with per-language word lists

**Phone Subtitle Display**
- Built-in HTTP + HTTPS subtitle server with WebSocket streaming
- Self-signed certificate for secure context (enables Wake Lock API on phones)
- Admin panel: remote start/stop/restart, input language selector, tune button
- Client-side i18n in 8 languages
- Wake Lock keeps phone screen on during display (HTTPS only)

**Batch Processing**
- **YouTube -> Subtitles** -- Download a YouTube video (or use a local file), optionally trim to a time range, and generate subtitles
- **Audio File -> Subtitles** -- Transcribe audio files (OGG, MP3, WAV, FLAC, M4A, etc.) directly into subtitles
- **YouTube -> Full Video** -- Download and trim a video without transcription
- **YouTube -> Audio Only** -- Download, trim, and extract audio to MP3
- Parallel chunk-based processing with resume support for interrupted jobs
- Multiple output formats: SRT, VTT, TXT, JSON, CSV, LRC

**Other**
- Configurable whisper parameters (beam size, temperature, VAD, threading, etc.)
- Multi-language UI: English, Spanish, French, German, Catalan, Portuguese, Chinese (Simplified), Japanese
- Light/Dark/System theme support
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

To publish a release build:

```bash
dotnet publish EveryTongue/EveryTongue.vbproj -c Release -o EveryTongue/bin/Publish
```

To build the installer (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php)):

```bash
iscc setup.iss
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
