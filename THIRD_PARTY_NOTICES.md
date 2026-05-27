# Third-Party Notices

Every Tongue incorporates and depends on the following open-source projects.
We are grateful to these authors and communities for making their work freely
available.

---

## Bundled with the Application

These components are compiled into or shipped alongside Every Tongue.

### Microsoft.Data.Sqlite
- **License:** MIT
- **Copyright:** .NET Foundation and Contributors
- **URL:** https://github.com/dotnet/efcore
- **Used for:** SQLite database access (Bible verse lookup)

### Microsoft.Web.WebView2
- **License:** MIT (SDK); Chromium runtime under BSD 3-Clause
- **Copyright:** Microsoft Corporation
- **URL:** https://github.com/MicrosoftEdge/WebView2
- **Used for:** Embedded browser for live output display

### NAudio
- **License:** MIT
- **Copyright:** Mark Heath
- **URL:** https://github.com/naudio/NAudio
- **Used for:** Audio device capture and playback

### ASP.NET Core (Kestrel)
- **License:** MIT
- **Copyright:** .NET Foundation and Contributors
- **URL:** https://github.com/dotnet/aspnetcore
- **Used for:** Embedded HTTP/WebSocket server for phone subtitle clients

---

## Bundled Binaries (Installer)

These pre-built binaries are included in the installer package.

### whisper.cpp
- **License:** MIT
- **Copyright:** Georgi Gerganov
- **URL:** https://github.com/ggml-org/whisper.cpp
- **Used for:** Batch speech-to-text transcription engine
- **Includes:** whisper-cli.exe, whisper.dll, ggml.dll, ggml-base.dll, ggml-cpu.dll, ggml-cuda.dll

### NVIDIA CUDA Runtime Libraries
- **License:** NVIDIA Software License Agreement (proprietary, redistribution permitted)
- **Copyright:** NVIDIA Corporation
- **URL:** https://developer.nvidia.com/cuda-toolkit
- **Used for:** GPU-accelerated inference for whisper.cpp
- **Includes:** cublas64_12.dll, cublasLt64_12.dll, cudart64_12.dll
- **Note:** Redistribution is permitted under NVIDIA's EULA. These libraries are
  provided by NVIDIA and are not open source. See NVIDIA's license terms at
  https://docs.nvidia.com/cuda/eula/

---

## Python Sidecar Services

These Python packages are bundled as self-contained sidecar servers.

### faster-whisper
- **License:** MIT
- **Copyright:** SYSTRAN
- **URL:** https://github.com/SYSTRAN/faster-whisper
- **Used for:** Live speech-to-text with voice activity detection

### CTranslate2
- **License:** MIT
- **Copyright:** OpenNMT
- **URL:** https://github.com/OpenNMT/CTranslate2
- **Used for:** Optimised inference engine for translation models

### SentencePiece
- **License:** Apache 2.0
- **Copyright:** Google LLC
- **URL:** https://github.com/google/sentencepiece
- **Used for:** Tokenisation for NLLB translation model

### FastAPI
- **License:** MIT
- **Copyright:** Sebastian Ramirez
- **URL:** https://github.com/tiangolo/fastapi
- **Used for:** REST API framework for sidecar servers

### Uvicorn
- **License:** BSD 3-Clause
- **Copyright:** Encode OSS Ltd
- **URL:** https://github.com/encode/uvicorn
- **Used for:** ASGI HTTP server

### sounddevice
- **License:** MIT
- **Copyright:** Matthias Geier
- **URL:** https://github.com/spatialaudio/python-sounddevice
- **Used for:** Audio device capture for live transcription

### sse-starlette
- **License:** BSD 3-Clause
- **Copyright:** sysid
- **URL:** https://github.com/sysid/sse-starlette
- **Used for:** Server-Sent Events streaming

### Hugging Face Transformers
- **License:** Apache 2.0
- **Copyright:** Hugging Face, Inc.
- **URL:** https://github.com/huggingface/transformers
- **Used for:** Model loading for MMS-TTS text-to-speech

### PyTorch
- **License:** BSD 3-Clause
- **Copyright:** Meta Platforms, Inc.
- **URL:** https://github.com/pytorch/pytorch
- **Used for:** Deep learning framework for model inference

### NumPy
- **License:** BSD 3-Clause
- **Copyright:** NumPy Developers
- **URL:** https://github.com/numpy/numpy
- **Used for:** Numerical computing

### edge-tts
- **License:** GPL 3.0
- **Copyright:** rany2
- **URL:** https://github.com/rany2/edge-tts
- **Used for:** Text-to-speech synthesis via Microsoft Edge voices
- **Note:** This package interacts with Microsoft's Edge Read Aloud service.
  It is a third-party project not affiliated with Microsoft.

---

## ML Models (Downloaded on First Use)

These models are not bundled with Every Tongue. They are downloaded by the
user on first use and are subject to their own license terms.

### OpenAI Whisper (ggml-large-v3)
- **License:** MIT
- **Copyright:** OpenAI
- **URL:** https://github.com/openai/whisper
- **Used for:** Speech recognition model weights

### Silero VAD
- **License:** MIT
- **Copyright:** Silero Team
- **URL:** https://github.com/snakers4/silero-vad
- **Used for:** Voice activity detection (bundled within faster-whisper)

### NLLB-200 (1.3B, CTranslate2 quantised)
- **License:** CC-BY-NC-4.0
- **Copyright:** Meta Platforms, Inc.
- **URL:** https://huggingface.co/JustFrederik/nllb-200-1.3B-ct2-float16
- **Original:** https://github.com/facebookresearch/fairseq
- **Used for:** Multilingual translation (200 languages)
- **Note:** This model is licensed for non-commercial use only.
  Every Tongue does not bundle this model. Users download it on first use
  and are responsible for complying with Meta's license terms.

### Meta MMS-TTS
- **License:** CC-BY-NC-4.0
- **Copyright:** Meta Platforms, Inc.
- **URL:** https://huggingface.co/facebook/mms-tts
- **Used for:** Multilingual text-to-speech (1,100+ languages)
- **Note:** Non-commercial use only. Downloaded on demand.

### Piper TTS (optional)
- **License:** MIT
- **Copyright:** Michael Hansen (rhasspy)
- **URL:** https://github.com/rhasspy/piper
- **Used for:** High-quality offline text-to-speech (optional alternative)

---

## External Tools (Downloaded on First Launch)

These tools are downloaded automatically on first launch. They run as
separate processes and are not linked into Every Tongue.

### yt-dlp
- **License:** Unlicense (public domain)
- **Copyright:** yt-dlp contributors
- **URL:** https://github.com/yt-dlp/yt-dlp
- **Used for:** Downloading video/audio from online platforms

### FFmpeg
- **License:** LGPL 2.1+ (some builds include GPL 2.0+ components)
- **Copyright:** FFmpeg developers
- **URL:** https://ffmpeg.org/
- **Used for:** Audio/video format conversion and processing
- **Note:** Every Tongue does not modify or link against FFmpeg. It is
  invoked as a separate process. Source code is available at https://ffmpeg.org/

### SubtitleEdit
- **License:** GPL 3.0
- **Copyright:** Nikolaj Olsson
- **URL:** https://github.com/SubtitleEdit/subtitleedit
- **Used for:** Subtitle file editing (optional, launched as separate process)

---

## Build Tools (Development Only)

These are used only during development and are not distributed with Every Tongue.

### .NET SDK 8.0
- **License:** MIT
- **Copyright:** .NET Foundation and Contributors
- **URL:** https://dotnet.microsoft.com/

### Inno Setup 6
- **License:** Inno Setup License (free for any use, including commercial)
- **Copyright:** Jordan Russell
- **URL:** https://jrsoftware.org/isinfo.php
- **Used for:** Windows installer creation

---

## Summary of License Compatibility

| License | Components | Compatible with GPL-3.0? |
|---------|-----------|:------------------------:|
| MIT | whisper.cpp, NAudio, faster-whisper, CTranslate2, FastAPI, Whisper model, Silero VAD, Piper | Yes |
| Apache 2.0 | SentencePiece, Transformers | Yes |
| BSD 3-Clause | Uvicorn, PyTorch, NumPy, sounddevice | Yes |
| Unlicense | yt-dlp | Yes |
| GPL 3.0 | edge-tts, SubtitleEdit | Yes (same license) |
| LGPL 2.1+ | FFmpeg | Yes (separate process) |
| CC-BY-NC-4.0 | NLLB-200, MMS-TTS | N/A (not bundled, user-downloaded) |
| Proprietary | NVIDIA CUDA | N/A (system library exception) |
