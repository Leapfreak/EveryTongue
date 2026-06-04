# EveryTongue — Future Architecture (updated 2026-06-04)

> This document describes the long-term architectural vision for EveryTongue: supporting every deployment scenario from offline conferences in developing countries to cloud-hosted conversation rooms, on any hardware, any platform.

---

## Competitive Landscape

### Direct Competitors (conference real-time translation)

**Microsoft Translator Live** — the closest free comparison. Presenter speaks, audience joins via a code on their phones, gets real-time translated captions. Free for up to 100 participants.
- Strengths: free, polished, good language coverage, easy setup
- Weaknesses: cloud-only (requires internet for every participant), no offline mode, no self-hosting, no PA mic integration, no data privacy (audio sent to Microsoft), no multi-room conference management
- Threat level: **high for online use cases** — if the venue has reliable internet, this is what most people reach for today

**Wordly** — AI-powered real-time translation specifically built for conferences and events. Attendees use phones/browsers. Closest product match to EveryTongue's feature set.
- Strengths: purpose-built for conferences, good accuracy, professional support
- Weaknesses: cloud-only, subscription pricing (~$2,000-5,000+ per event or annual enterprise contracts), aimed at corporate conferences, no offline mode, no self-hosting
- Threat level: **low for our users** — pricing rules it out for charities and developing-world organisations

**KUDO / Interprefy** — interpretation platforms focused on **human interpreters** with AI assist. The model is: you still hire professional interpreters, the platform manages audio routing and remote interpreter access.
- Strengths: highest quality (human interpreters), professional-grade
- Weaknesses: very expensive (interpreter fees + platform fees), enterprise-oriented, not AI-first
- Threat level: **none** — completely different market segment

### Adjacent Products

| Product | What it does | Why it's different from EveryTongue |
|---------|-------------|-------------------------------------|
| Google Translate conversation mode | Real-time speech translation on a phone | 1-to-1 only, not conference broadcast |
| Zoom/Teams/Webex translation | Built-in AI captions with translation | Requires their platform, cloud-only, not for in-person events |
| SpeakSmart, Lingmo, Timekettle | Hardware translation earbuds/devices | Expensive per-unit, not scalable to audience |
| Amazon Transcribe + Translate | AWS services (STT + translation APIs) | Raw APIs, requires custom development, cloud-only, pay-per-use |

### The Gap EveryTongue Fills

Nobody else combines **all** of these:

- **Offline / self-hosted** — no cloud dependency, no internet required
- **Free** — no subscription, no per-event cost, no per-seat licensing
- **PA microphone input** — real conference setup, not phone-to-phone
- **Broadcast to unlimited phones** via local WiFi only
- **Runs on modest/donated hardware** — not just high-end servers
- **Multi-room support** — multiple concurrent sessions at the same conference
- **Data privacy** — audio and translations never leave the local network
- **Open architecture** — pluggable engines, not locked to one vendor's STT/translation

### Target Users (underserved by existing solutions)

- International churches and mission organisations
- Charities and NGOs running multilingual events in developing countries
- Conference centres in areas with unreliable or no internet
- Organisations that can't afford $2,000+ per event (Wordly) or professional interpreters (KUDO)
- Deployments where data privacy matters (audio never leaving the building)
- Sites relying on donated/second-hand hardware

### Strategic Position

The main competitive risk is Microsoft Translator Live — it's free and covers the online conference case well. EveryTongue's defensible advantages are:

1. **Offline capability** — the single strongest differentiator. No internet needed at the venue.
2. **Self-hosted / data privacy** — audio stays on the local network, never reaches a cloud provider.
3. **PA mic integration** — designed for real conference audio setups, not phone microphones.
4. **Multi-room management** — conference templates, room governance, host controls for complex multi-session events.
5. **Cost** — free software on donated hardware vs recurring SaaS subscriptions.
6. **Flexibility** — open engine architecture means the best available STT/translation/TTS can be plugged in as technology improves, not locked to one vendor.

As the architecture expands to cover online and Lite modes, EveryTongue becomes competitive in Microsoft Translator Live's territory too — while retaining the offline capability that Microsoft can't match.

---

## The Problem

EveryTongue currently requires a Windows machine with an NVIDIA GPU to run the full pipeline (STT + Translation + TTS). That's a high cost of entry for a charity operating in the developing world. The software needs to scale down to cheap hardware and scale up to cloud deployments — without losing its core strength: **offline, real-time translation at multinational conferences.**

---

## Deployment Quadrants

|  | **Offline** | **Online** |
|--|------------|-----------|
| **Conference** | PA mic → Server Whisper → NLLB → phones. Current core use case. Needs GPU or good CPU. | PA mic → Cloud STT API → Cloud/local translation → phones. No GPU needed locally. |
| **Conversation** | Phones → server audio → Server Whisper → NLLB → phones. Current rooms. Needs GPU. | Phones → Web Speech API (text) → NLLB on CPU → phones. No GPU needed anywhere. |

All four quadrants should be supported by the same codebase. The difference is which engines are plugged in, not which app you run.

---

## Core Architectural Principle

Every pipeline is the same four steps. The only question is **where** each step executes:

```
Audio --> Text --> Translated Text --> Speech
(STT)    (Translation)                (TTS)
```

Each step can run on the **Client** (phone), **Server** (local box), or **Cloud** (remote GPU server).

### Pipeline Configurations

| Mode | STT | Translation | TTS | Hardware Needed |
|------|-----|-------------|-----|-----------------|
| **Full** (offline conference) | Server: Faster Whisper + GPU | Server: NLLB 3.3B + GPU | Server: Piper, or Client: browser voices | Windows/Linux + NVIDIA GPU |
| **Standard** (offline, modest hardware) | Server: whisper.cpp on CPU/Vulkan | Server: NLLB 1.3B on CPU | Client: browser voices | Any modern laptop |
| **Lite** (online conversation) | Client: Web Speech API | Server: NLLB on CPU | Client: browser voices | Raspberry Pi / any old laptop |
| **Cloud** (online conference) | Cloud relay to remote GPU | Cloud or Server | Client: browser voices | Local WiFi router + internet |

### Auto-Detection Strategy

At startup, the server detects what's available and assembles the best pipeline:

```
NVIDIA GPU (CUDA)?            --> Use FasterWhisperBackend (best performance)
Any GPU (Vulkan)?             --> Use WhisperCppBackend + Vulkan (AMD, Intel, older NVIDIA)
No usable GPU?                --> Use WhisperCppBackend + CPU (smaller models recommended)
Internet + cloud API key?     --> Use CloudSttBackend (future)
None of the above?            --> Enable client-side STT mode (phones transcribe via Web Speech API)
```

Same tiered fallback for Translation and TTS (already implemented for TTS: Piper → MMS-TTS → Edge TTS). The operator can override in Options, but sensible defaults mean a non-technical user just runs it and it works.

### Current State of Pluggable Engine Architecture (as of v1.7.5)

All three pipeline stages now have fully pluggable registries with factory methods:

| Stage | Registry | Factory | Backends Available | Config Property |
|-------|----------|---------|-------------------|-----------------|
| **STT** | `SttBackendRegistry` | `CreateBackend(key)` | Faster Whisper, whisper.cpp Vulkan, whisper.cpp CPU | `AppConfig.SttBackend` |
| **Translation** | `TranslationBackendRegistry` | N/A (combo-driven) | NLLB 1.3B, NLLB 3.3B, MADLAD-400, DeepL, Google, Azure | `AppConfig.TranslationBackend` |
| **TTS** | `TtsBackendRegistry` | N/A (priority list) | Piper, MMS-TTS, Edge TTS | `AppConfig.TtsBackends` |

All UI labels are generic ("STT model", "Translation engine", "TTS preference") — no engine-specific terminology in user-facing strings. Adding a new engine requires: (1) implement the interface, (2) add one line to the registry. No UI changes needed.

The Options dialog auto-populates engine dropdowns from the registries. Changing the translation backend in Options now triggers an automatic sidecar restart (previously required manual restart). Dependency checks use the configured model path, not hardcoded defaults.

---

## Cross-GPU Support — NEXT (v1.8.0)

Currently STT (faster-whisper) requires NVIDIA CUDA. This is the single biggest hardware constraint. The strategy is simple: **CUDA → Vulkan → CPU**. No other backends needed — Vulkan covers every GPU from the last 10+ years across all three vendors.

### Speed Comparison (Whisper STT, updated 2026-06)

| Backend | Relative Speed | GPU Coverage | Notes |
|---------|---------------|-------------|-------|
| CUDA (faster-whisper/CTranslate2) | **1.0x baseline** | NVIDIA only | INT8/FP16 quantization, most optimized |
| Vulkan (whisper.cpp 1.8+) | **~1.2-1.8x slower** | **All GPUs** (NVIDIA, AMD, Intel) | 10-12x faster than CPU on iGPUs. Mature as of 2026. |
| CPU-only (whisper.cpp, AVX2) | ~3-6x slower | N/A | Fallback when no GPU at all |

Previous estimates of Vulkan being 1.5-3x slower were based on early implementations. As of whisper.cpp 1.8.3, Vulkan delivers ~10-12x speedup over CPU even on integrated GPUs (AMD Radeon 780M, Intel Arc). On discrete GPUs the gap with CUDA narrows further.

DirectML (ONNX Runtime) is no longer in scope — Vulkan covers the same hardware with better performance and cross-platform portability.

### Practical Impact

- **CUDA on mid-range NVIDIA**: ~10-20% of real-time (10s audio → 1-2s). Handles 3-4 concurrent streams.
- **Vulkan on discrete AMD/Intel GPU**: ~15-35% of real-time. Comfortably real-time for 1-2 streams.
- **Vulkan on integrated GPU** (Ryzen APU, Intel Iris): ~30-60% of real-time. Viable for single-stream conference use.
- **CPU-only** (modern i5/Ryzen 5): ~30-60% of real-time with `base`/`small` models. Viable for single-stream conference with clean PA audio.

### Fallback Strategy

```
Startup hardware detection:
  1. NVIDIA GPU with CUDA?     → Use faster-whisper (existing, best performance)
  2. Any GPU with Vulkan?      → Use whisper.cpp + Vulkan (covers AMD, Intel, older NVIDIA)
  3. No usable GPU?            → Use whisper.cpp + CPU (AVX2, smaller models recommended)
```

Auto-detection at startup via `HardwareScanner`. The operator can override in Options, but sensible defaults mean a non-technical user just runs it and it works. Same tiered-fallback pattern as TTS (Piper → MMS-TTS → Edge TTS).

### Implementation Plan

whisper.cpp ships as a single native executable (`whisper-cli.exe`) with prebuilt Vulkan support. The implementation wraps it in the same sidecar HTTP server pattern used by faster-whisper's `live-server/server.py`:

1. **New Python sidecar: `whisper-cpp-server/server.py`** — FastAPI server that wraps `whisper-cli.exe`. Exposes the same HTTP + SSE API as `live-server/server.py` (`/health`, `/start`, `/stop`, `/stream`, `/config`, `/devices`, `/stats`, `/transcribe`, `/shutdown`). Uses subprocess to run whisper-cli for transcription, sounddevice for audio capture, Silero VAD for voice activity detection (same as live-server).

2. **New VB.NET backend: `WhisperCppBackend.vb`** — implements `ISttBackend`. Thin adapter (like `FasterWhisperBackend`) that wraps a new `WhisperCppRunner` using `PythonSidecarHost` to manage the sidecar lifecycle. Communicates via the same HTTP+SSE protocol.

3. **Registry entries** — two new entries in `SttBackendRegistry`:
   - `"whisper-cpp-vulkan"` → `WhisperCppBackend` (Vulkan GPU acceleration)
   - `"whisper-cpp-cpu"` → `WhisperCppBackend` (CPU-only fallback)
   Both use the same backend class; the difference is the `--gpu` flag passed to whisper-cli.

4. **Auto-detection** — `HardwareScanner` extended to detect Vulkan support (check for vulkan-1.dll or run `vulkaninfo`). At startup, if no CUDA GPU is found but Vulkan is available, default `AppConfig.SttBackend` to `"whisper-cpp-vulkan"`. If no GPU at all, default to `"whisper-cpp-cpu"`.

5. **Download Manager** — add whisper-cli.exe (Vulkan build) and GGML model files as downloadable dependencies. Models: `ggml-base.bin` (~150MB, for CPU), `ggml-large-v3-turbo.bin` (~1.5GB, for GPU).

6. **Options dialog** — already auto-populates from `SttBackendRegistry.GetAll()`. No UI changes needed.

### Other Pipeline Stages

- **Translation (NLLB)**: CTranslate2 already supports CPU-only mode. For GPU on non-NVIDIA, ONNX Runtime + DirectML conversion is the path. As of v1.7.5, three offline models are available via the Download Manager:
  - **NLLB 1.3B** (float16) — ~2.5GB download, ~6.5GB VRAM. Avg quality ~80%. Good for constrained hardware.
  - **NLLB 3.3B** (float16) — ~6.7GB download, ~9GB VRAM. Avg quality ~83%. Best option for 16GB GPUs.
  - **MADLAD-400 3B** (int8_float16) — wider language coverage (450+) but lower quality for English→X pairs (~37-60%).
  - NLLB-200 3.3B is the best dedicated multilingual translation model available in CTranslate2 format. Published benchmarks confirm it outperforms M2M-100, MADLAD-400 3B, and recent 7-8B LLMs at translation.
- **TTS**: No GPU dependency. Piper is CPU-only (ONNX), Edge TTS is cloud, MMS-TTS uses PyTorch (CUDA optional). Browser voices are free and local.

---

## Lite Mode — Zero-GPU Deployment

### Concept

When no STT backend is available on the server, EveryTongue switches to **Lite mode**: phones do their own speech-to-text using the Web Speech API, and the server is purely a text translation relay.

### How It Works

1. Speaker talks into their phone (or a phone placed near the PA speaker)
2. Phone's browser uses `webkitSpeechRecognition` / `SpeechRecognition` to transcribe (uses Google/Apple cloud — free, no API key)
3. Phone sends **text** to the server via WebSocket
4. Server translates via NLLB on CPU (~500ms-1s per sentence)
5. Server broadcasts translated text to all other phones
6. Phones speak the translation using browser `speechSynthesis` (free, local)

### Server Requirements in Lite Mode

- **No audio capture** — no NAudio, no FFmpeg, no Whisper
- **No GPU** — NLLB on CPU is the only compute
- **Runs on anything**: Raspberry Pi 5 (~$80), any old laptop, $5/month cloud VPS
- **Minimal download**: no Whisper models (1-6GB), no FFmpeg, no audio dependencies

### What Already Exists

Conversation rooms already have text chat with translation — user types text, server translates, everyone sees it. The Web Speech API path is the same flow, except the text comes from speech recognition instead of the keyboard. Minimal new code needed.

### Tradeoffs

- **Requires internet on the speaker's phone** — Web Speech API sends audio to Google/Apple for transcription. Listener phones only need local WiFi to the server.
- **Language support** — good for major languages (English, Spanish, French, Portuguese, Arabic, Swahili, Hindi), weaker for minority languages.
- **Less accurate than Whisper** in noisy environments or with accented speech. But adequate for clear speech in a conference/meeting setting.
- **Not fully offline** — this is the conversation/online quadrant, not a replacement for the offline conference use case.

### UX in Lite Mode

- If no STT backend is detected, conference mode disables audio input and shows guidance: "No speech engine available — connect a phone as speaker, or install Whisper from the Download Manager"
- Conversation rooms work seamlessly — the phone's "speak" button uses Web Speech API instead of sending audio to the server
- No dead-end errors, no broken features — just graceful degradation

---

## Cloud Relay — Shared GPU Server

### Concept

A charity runs **one** GPU-powered EveryTongue server (cloud-hosted or at HQ). Field offices connect to it instead of running local engines.

```
[Field Office A - RPi]     --\
[Field Office B - laptop]  ---+--> [Cloud EveryTongue Server - GPU]
[Field Office C - phones]  --/
```

### Two Deployment Patterns

**Pattern 1: Local relay server**
Each field site runs a local Lite server for room management and local WiFi. Heavy compute (STT, translation) is relayed to the cloud instance via API.

- Local server handles: rooms, lobby, WebSocket hub, static files, local WiFi
- Cloud server handles: Whisper STT, NLLB translation, TTS synthesis
- Advantage: works with intermittent internet (rooms stay up, translation queues when connectivity drops)
- Implementation: `CloudRelaySttBackend` and `CloudRelayTranslationBackend` that forward requests to a remote EveryTongue server's REST API

**Pattern 2: Direct cloud connection**
Phones connect directly to the cloud server over the internet. No local server at all.

- Simplest setup: just give people the URL
- Disadvantage: requires stable internet for all participants, higher latency
- Best for: remote/hybrid conferences where participants aren't co-located

### Cost Model

- A single T4 GPU cloud instance (~$150-300/month) can serve 3-4 concurrent STT streams
- Amortised across 10+ field offices, that's $15-30/month per site
- Spot/preemptible instances can reduce this further for scheduled events
- The server only needs to run during events, not 24/7

---

## Cross-Platform — Linux, macOS, Docker

### Prerequisite: Headless Server

The WinForms desktop app is the current barrier to cross-platform. Once the headless server mode is built (already planned in PLAN.md), the Windows dependency disappears for the server component.

### What's Already Cross-Platform

- Kestrel web server (ASP.NET Core)
- All web client HTML/JS/CSS
- Python sidecars (NLLB, MMS-TTS, live-server)
- Piper TTS (native binaries for all platforms)
- whisper.cpp (builds for Linux/macOS natively)
- The entire phone experience (rooms, lobby, translation, TTS, Bible)

### What Needs Replacing

| Windows Component | Cross-Platform Replacement |
|-------------------|---------------------------|
| WinForms UI | Web-based admin dashboard (served by Kestrel) |
| WebView2 | Not needed (phones are the primary UI) |
| NAudio audio capture | Platform-specific audio backends, or USB audio passed to whisper.cpp directly |
| Firewall rules (`netsh`) | Platform-aware setup scripts or manual configuration |

### Target Platforms

- **Linux** — highest-value target. Cheaper to run headless Ubuntu/Debian. Churches in developing countries could use low-cost hardware. Raspberry Pi 5 (ARM64) viable for Lite mode or single-stream CPU STT.
- **macOS** — Metal backend for whisper.cpp is fast (~1.2-1.5x vs CUDA). Apple Silicon Macs increasingly common in churches/organisations.
- **Docker** — package headless server + all sidecars as a single Docker image. One `docker run` and it's serving rooms. GPU passthrough via `--gpus all` (NVIDIA) or `--device /dev/dri` (AMD/Intel). Ideal for cloud hosting and technical deployments.

### Build Architecture

```
EveryTongue.Core        <-- Shared library: rooms, WebSocket hub, translation
                            orchestrator, all interfaces, engine registries.
                            No UI, no audio capture, no platform dependencies.
                            Targets net8.0 (cross-platform).

EveryTongue.Desktop     <-- Windows WinForms app (current product).
                            References Core + NAudio + WebView2 + full engine suite.
                            Targets net8.0-windows.

EveryTongue.Server      <-- Cross-platform console app. References Core.
                            Headless, runs on Linux/macOS/Windows/Docker/RPi.
                            Engine registration based on what's installed.
                            Targets net8.0.

EveryTongue.Lite        <-- Minimal build of Server. No audio dependencies.
                            Just rooms + NLLB translation + static web files.
                            Tiny footprint. Could be a single-file executable.
                            Targets net8.0.
```

All variants share the same web client, same rooms, same lobby, same phone experience. The difference is just what engines are available server-side.

---

## Second-Hand Hardware Strategy

For charities that can't afford new equipment but can source donated/second-hand hardware:

- **GTX 1060/1070** (~$50-80 used) — runs faster-whisper with `small` model comfortably. Single-stream real-time conference STT.
- **GTX 1080/1080 Ti** (~$100-150 used) — runs `medium` or `large-v3` model. Multi-stream capable.
- **Any modern laptop** (i5/Ryzen 5, 2020+) — runs whisper.cpp CPU with `base`/`small` model for single-stream conference use. Clean PA audio helps accuracy.
- **Raspberry Pi 5** (~$80 new) — runs Lite mode (NLLB on CPU, phones do STT+TTS). Adequate for conversation rooms.

---

## Cost Analysis — Local vs Cloud vs Hybrid

The assumption that "cloud is cheaper" doesn't hold in all scenarios. The right answer depends on usage patterns, number of sites, and whether internet is available at all.

### Option A: Local Hardware (one-time cost)

| Setup | Cost | Ongoing | Notes |
|-------|------|---------|-------|
| Used gaming laptop (GTX 1060) | $200-400 | ~$5-15/month power | Full offline conference. Single-stream STT. |
| Desktop + used GTX 1070 | $200-250 | ~$5-15/month power | Same capability, cheaper if parts available |
| Desktop + used GTX 1080 Ti | $300-400 | ~$10-20/month power | Multi-stream, `large-v3` model |
| Any modern laptop (no GPU) | $200-400 | ~$5/month power | whisper.cpp CPU, `base`/`small` model, single-stream |
| Raspberry Pi 5 + accessories | ~$100 | ~$1/month power | Lite mode only (conversations, no PA mic STT) |

**Year 1 total**: $200-500. **Year 2+**: $60-180 (just power). Works offline. No subscriptions. Hardware can be donated.

### Option B: Cloud GPU (recurring cost)

| Provider | Instance | Cost/hour | 4hr weekly conference | Daily 2hr meeting |
|----------|----------|-----------|----------------------|-------------------|
| AWS g4dn.xlarge (T4) | On-demand | ~$0.50/hr | ~$8/month | ~$30/month |
| AWS g4dn.xlarge (T4) | Spot | ~$0.15/hr | ~$2.50/month | ~$9/month |
| Azure NC4as T4 | On-demand | ~$0.50/hr | ~$8/month | ~$30/month |
| GCP g2-standard-4 | On-demand | ~$0.55/hr | ~$9/month | ~$33/month |

**Year 1 total**: $100-380. **Year 2 total**: same again. Never stops costing. Requires internet at venue.

Spot/preemptible instances are cheapest but can be interrupted — risky for live conferences. On-demand is reliable but costs more.

### Option C: Cloud CPU / Lite (cheapest recurring)

| Setup | Cost/month | Capability |
|-------|-----------|------------|
| Basic VPS (2 vCPU, 4GB RAM) | $5-20/month | NLLB translation on CPU. Phones do STT + TTS. Conversation rooms. |
| Larger VPS (4 vCPU, 8GB RAM) | $20-40/month | Faster NLLB. Could run whisper.cpp CPU for single-stream. |

**Year 1 total**: $60-480. Requires internet for all participants.

### Break-Even Analysis

| Scenario | Cloud wins until... | Then local wins |
|----------|-------------------|-----------------|
| Single site, weekly use | ~12-18 months | Used laptop pays for itself |
| Single site, daily use | ~3-6 months | Local hardware pays off fast |
| 10 field offices | ~18-24 months | 10 laptops ($3,000-5,000) vs shared cloud ($100-300/month) |
| Occasional use (monthly events) | ~3+ years | Cloud stays cheaper for rare use |

### The Hidden Costs

**Cloud hidden costs:**
- Internet at the venue (not always available, not always reliable)
- Latency — audio round-trip to cloud adds 200-500ms on top of processing time
- Data transfer costs (audio streaming to cloud adds ~$1-5/month)
- Vendor lock-in and price changes
- Downtime risk — cloud outage during a live conference is catastrophic

**Local hidden costs:**
- Hardware failure or theft (real risk in field conditions)
- Maintenance and setup at each site (needs someone technical)
- Shipping/transporting hardware to remote locations
- Power reliability (some field locations have unreliable electricity)
- Software updates need to be applied at each site

### The Verdict

**There is no single right answer.** The best deployment depends on the site:

| Situation | Best option |
|-----------|-------------|
| Remote village, no internet | Local hardware (the only option) |
| Urban church, reliable internet, tight budget | Cloud on-demand for conferences, Lite mode locally for conversations |
| Organisation with 10+ sites, central IT team | Hub-and-spoke: shared cloud GPU + local Lite servers at each site |
| One-off event or trial deployment | Cloud (no upfront cost, spin up and test) |
| Permanent installation, daily use | Local hardware (pays for itself within months) |
| Unreliable power + unreliable internet | Local laptop with battery + Lite mode as fallback |

### The Hybrid Recommendation

For most charity deployments, the answer is **both**:

1. **Each field site runs Lite mode locally** — cheap hardware (RPi or old laptop), offline-capable, handles conversation rooms and daily meetings. ~$100-400 one-time cost.
2. **The organisation runs one shared cloud GPU server** — spun up on-demand for conferences that need full Whisper STT from a PA mic. $2-30/month depending on frequency. All field sites can connect to it when internet is available.
3. **Sites with donated GPU hardware run Full mode locally** — completely independent, no cloud needed, no ongoing cost.

This gives every site a working baseline (Lite mode) while sharing the expensive resource (GPU compute) across the organisation. Sites gradually move to local Full mode as hardware becomes available through donations or purchases.

---

## Implementation Roadmap

### Phase 0 — Pluggable Engine Architecture — COMPLETE (v1.7.3–1.7.5)
All three pipeline stages (STT, Translation, TTS) now have registries, interfaces, and factory methods. UI is fully generic — no engine-specific labels. Adding a new engine = implement interface + one registry line. Options dialog auto-populates from registries. Translation backend hot-switching works (auto-restart sidecar on change). Multiple translation models available (NLLB 1.3B, NLLB 3.3B, MADLAD-400). `SttBackendRegistry.CreateBackend(key)` factory replaces all hardcoded backend creation. This phase is the foundation that makes Phases 1–4 straightforward.

### Phase 1 — Cross-GPU STT (v1.8.0) — COMPLETE
Strategy: **CUDA → Vulkan → CPU** fallback with manual override in Options.

**Shared sidecar approach** — `live-server/server.py` extended with whisper-server.exe support (no separate sidecar). The Python server manages whisper-server.exe as a subprocess, proxying audio through its `/inference` endpoint.

**What was built:**
- `WhisperCppBackend.vb` — implements `ISttBackend`, delegates to `LiveStreamRunner` with `whisper-cpp-vulkan` or `whisper-cpp-cpu` backend key
- `LiveStreamRunner` — extended to configure the shared sidecar for whisper.cpp backends
- `HardwareScanner` — CUDA detection (nvidia-smi), Vulkan detection (vulkan-1.dll), `SuggestSttBackend()` auto-select
- `SttBackendRegistry` — three entries: `faster-whisper`, `whisper-cpp-vulkan`, `whisper-cpp-cpu`
- `DependencyManager` — whisper-server.exe and GGML model dependency checks
- `FormDownloadManager` — whisper.cpp tools category with whisper-server + GGML model downloads
- `FormOptions` — path controls for whisper-server.exe and GGML model, STT Engine combo on Hardware panel (auto-suggests on rescan, user can override)
- First-run auto-detection — `CheckDependenciesAsync` sets `SttBackend` based on hardware scan
- **STT Comparison Benchmark** — `SttComparisonRunner` tests all available backends with same audio, shows side-by-side latency/speedup in `FormTranslationBenchmark`

### Phase 2 — Lite Mode
4. **Web Speech API STT** — phone-side speech recognition, sends text via WebSocket. Minimal server change (text chat translation path already exists).
5. **Graceful degradation** — conference mode adapts UI when no STT is available. Conversation rooms seamlessly use phone-side STT.
6. **Lite build** — strip audio dependencies for a minimal download/footprint.

### Phase 3 — Cross-Platform
7. **Extract EveryTongue.Core** — pull all non-WinForms, non-audio code into a shared net8.0 library.
8. **EveryTongue.Server console app** — headless entry point, cross-platform. Web-based admin dashboard replaces WinForms.
9. **Docker image** — single container with server + NLLB + Piper + whisper.cpp. One command to deploy.

### Phase 4 — Cloud Relay
10. **Cloud relay backends** — `ISttBackend` and `ITranslationBackend` that forward to a remote EveryTongue server's API.
11. **Hub-and-spoke deployment** — one cloud GPU server, multiple field Lite servers connecting to it.
12. **Direct cloud mode** — phones connect to cloud server with no local server needed.

---

## Summary

The goal is one codebase that adapts to what's available:

| What you have | What you get | Status |
|---------------|-------------|--------|
| NVIDIA GPU + Windows | Full offline conference mode (Faster Whisper + NLLB 3.3B + Piper) | **Working now** |
| AMD/Intel GPU + Windows | Conference mode via whisper.cpp + Vulkan | **Phase 1 — complete** |
| Modern CPU, no GPU | Conference mode via whisper.cpp CPU (smaller models) | **Phase 1 — complete** |
| Old laptop or RPi | Lite mode: conversation rooms, phone-side STT, NLLB on CPU | Phase 2 |
| Internet + cloud budget | Cloud relay: conference mode without any local GPU | Phase 4 |
| Just phones + WiFi router | Lite server on anything + Web Speech API on phones | Phase 2 |

The pluggable engine architecture (Phase 0) is complete — all registries, interfaces, factory methods, and generic UI are in place. Each future phase is now a matter of implementing new backend classes, not refactoring existing code.

Every Tongue, every budget, every platform.
