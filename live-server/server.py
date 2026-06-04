"""
Live Transcription Server for Every Tongue.
FastAPI server: sounddevice audio capture -> Silero VAD -> whisper-server -> SSE events.
"""

import argparse
import asyncio
import io
import json
import logging
import os
import re
import signal
import subprocess
import sys
import tempfile
import threading
import time
import urllib.request
import wave

import numpy as np
import sounddevice as sd
from difflib import SequenceMatcher
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from sse_starlette.sse import EventSourceResponse

# Silero VAD — standalone package for speech detection
try:
    import torch
    from silero_vad import load_silero_vad
    _silero_vad_model = load_silero_vad()
    _has_silero_vad = True
except ImportError as _vad_err:
    _has_silero_vad = False
    _silero_vad_model = None
    print(f"[LIVE] WARNING: Silero VAD import failed: {_vad_err}", file=sys.stderr)

# VAD pipeline — lazy-safe import (depends on torch via frame_vad.py)
try:
    from vad import VadPipeline, VadConfig
    from vad.segment import SessionStats
    _has_vad_pipeline = True
except ImportError as _vad_pipe_err:
    _has_vad_pipeline = False
    VadPipeline = None
    VadConfig = None
    SessionStats = None
    print(f"[LIVE] WARNING: VAD pipeline import failed: {_vad_pipe_err}", file=sys.stderr)

# ---------------------------------------------------------------------------
# Logging — stderr only, captured by PythonSidecarHost -> AppLogger
# ---------------------------------------------------------------------------
logger = logging.getLogger("live-server")
logger.setLevel(logging.DEBUG)
logger.propagate = False
_stderr_handler = logging.StreamHandler()
_stderr_handler.setLevel(logging.DEBUG)
_stderr_handler.setFormatter(logging.Formatter("[LIVE] %(message)s"))
logger.addHandler(_stderr_handler)

# Suppress noisy loggers
logging.basicConfig(level=logging.WARNING)
logging.getLogger("uvicorn").setLevel(logging.WARNING)
logging.getLogger("uvicorn.access").setLevel(logging.WARNING)


app = FastAPI()

# ---------------------------------------------------------------------------
# Global state
# ---------------------------------------------------------------------------
model_path_global: str = ""

capturing: bool = False
_vad_pipeline = None  # type: VadPipeline or None

# SSE subscribers (asyncio queues)
subscribers = []  # list of asyncio.Queue
subscribers_lock: threading.Lock = threading.Lock()

# Audio config
SAMPLE_RATE = 16000

# Capture config (set via /start, read by /config)
current_config: dict = {}

# Current session stats (accessible via /stats endpoint)
current_stats = None  # type: SessionStats or None

# Uvicorn server reference for graceful shutdown
_server = None
_shutting_down: bool = False

# whisper-server process management
_whisper_server_path: str = ""
_whisper_server_port: int = 0
_no_gpu: bool = False
_whisper_server_process = None  # type: subprocess.Popen or None


# ---------------------------------------------------------------------------
# Normalized segment class (bridges whisper-server JSON and internal pipeline)
# ---------------------------------------------------------------------------
class WordInfo:
    """Word-level timestamp matching faster-whisper's Word interface."""
    __slots__ = ('word', 'start', 'end', 'probability')
    def __init__(self, word="", start=0.0, end=0.0, probability=0.0):
        self.word = word
        self.start = start
        self.end = end
        self.probability = probability

class SegmentInfo:
    """Segment with attributes matching faster-whisper's Segment interface."""
    __slots__ = ('start', 'end', 'text', 'no_speech_prob', 'avg_logprob', 'words')
    def __init__(self, start=0.0, end=0.0, text="", no_speech_prob=0.0, avg_logprob=0.0, words=None):
        self.start = start
        self.end = end
        self.text = text
        self.no_speech_prob = no_speech_prob
        self.avg_logprob = avg_logprob
        self.words = words or []

class TranscribeInfo:
    """Transcription metadata matching faster-whisper's TranscriptionInfo."""
    __slots__ = ('language',)
    def __init__(self, language=""):
        self.language = language


# ---------------------------------------------------------------------------
# whisper-server process management (for whisper-cpp backend)
# ---------------------------------------------------------------------------
def _start_whisper_server(server_path: str, model_path: str, port: int, no_gpu: bool = False):
    """Start whisper-server.exe as a subprocess. Model stays loaded in memory."""
    global _whisper_server_process, _whisper_server_port
    _stop_whisper_server()

    cmd = [server_path, "-m", model_path, "--port", str(port), "--host", "127.0.0.1"]
    if no_gpu:
        cmd.append("-ng")

    logger.debug(f"WHISPER-SERVER START: {' '.join(cmd)}")

    _whisper_server_process = subprocess.Popen(
        cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
    )
    _whisper_server_port = port

    # =========================================================================
    # CRITICAL: Drain whisper-server stderr in a background thread.
    #
    # whisper-server writes to stderr on every inference request (system_info,
    # processing status, etc.). Python's subprocess.PIPE creates an OS pipe
    # with a finite buffer (~4-64KB on Windows). If nobody reads the pipe,
    # the buffer fills up after ~7 inference requests. Once full, whisper-
    # server's next write() to stderr BLOCKS, which freezes the inference
    # mid-request. Our HTTP call then hangs forever waiting for a response
    # that will never come — a deadlock.
    #
    # This drain thread continuously reads stderr so the pipe never fills.
    # DO NOT REMOVE THIS THREAD. Without it, whisper-server silently hangs
    # after a handful of requests and transcription stops completely.
    # =========================================================================
    def _drain_whisper_stderr(proc):
        try:
            for line in proc.stderr:
                decoded = line.decode(errors="replace").rstrip()
                if decoded:
                    logger.debug(f"WHISPER-SERVER: {decoded}")
        except Exception:
            pass
    _ws_drain = threading.Thread(target=_drain_whisper_stderr, args=(_whisper_server_process,), daemon=True)
    _ws_drain.start()

    # Wait for server to be ready (up to 60s for large model load)
    for i in range(120):
        if _whisper_server_process.poll() is not None:
            stderr_out = _whisper_server_process.stderr.read().decode(errors="replace")
            raise RuntimeError(f"whisper-server exited with code {_whisper_server_process.returncode}: {stderr_out[-500:]}")
        try:
            req = urllib.request.Request(f"http://127.0.0.1:{port}/health")
            with urllib.request.urlopen(req, timeout=1) as resp:
                if resp.status == 200:
                    logger.debug(f"WHISPER-SERVER READY on port {port} (took ~{i * 0.5:.0f}s)")
                    return
        except Exception:
            pass
        time.sleep(0.5)
    raise RuntimeError("whisper-server startup timeout (60s)")


def _stop_whisper_server():
    """Stop whisper-server.exe if running."""
    global _whisper_server_process
    if _whisper_server_process is None:
        return
    if _whisper_server_process.poll() is None:
        logger.debug("WHISPER-SERVER STOP")
        try:
            _whisper_server_process.terminate()
            _whisper_server_process.wait(timeout=5)
        except Exception:
            try:
                _whisper_server_process.kill()
            except Exception:
                pass
    _whisper_server_process = None


def _restart_whisper_server():
    """Restart whisper-server to work around handle leak (ggml-org/whisper.cpp#3358)."""
    if _whisper_server_path and model_path_global and _whisper_server_port:
        logger.debug("WHISPER-SERVER RESTART (handle leak workaround)")
        _start_whisper_server(_whisper_server_path, model_path_global, _whisper_server_port, _no_gpu)


def _post_multipart(url: str, fields: dict, files: dict, timeout: int = 30):
    """POST multipart/form-data using only stdlib."""
    boundary = "----EveryTongueBoundary" + os.urandom(8).hex()
    body = io.BytesIO()
    for key, val in fields.items():
        body.write(f"--{boundary}\r\n".encode())
        body.write(f'Content-Disposition: form-data; name="{key}"\r\n\r\n'.encode())
        body.write(f"{val}\r\n".encode())
    for key, (filename, data, content_type) in files.items():
        body.write(f"--{boundary}\r\n".encode())
        body.write(f'Content-Disposition: form-data; name="{key}"; filename="{filename}"\r\n'.encode())
        body.write(f"Content-Type: {content_type}\r\n\r\n".encode())
        body.write(data)
        body.write(b"\r\n")
    body.write(f"--{boundary}--\r\n".encode())
    req = urllib.request.Request(url, body.getvalue(), {
        "Content-Type": f"multipart/form-data; boundary={boundary}"
    })
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        return json.loads(resp.read())


def _audio_to_wav_bytes(audio_array: np.ndarray) -> bytes:
    """Convert float32 numpy audio to 16-bit WAV bytes."""
    buf = io.BytesIO()
    audio_int16 = (np.clip(audio_array, -1.0, 1.0) * 32767).astype(np.int16)
    with wave.open(buf, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(audio_int16.tobytes())
    return buf.getvalue()


def _transcribe_whisper_cpp(audio_array: np.ndarray, language=None,
                            beam_size=5, initial_prompt=""):
    """Transcribe audio via whisper-server.exe /inference endpoint.
    Returns (segments, info) or (None, None) on error."""
    audio_duration = len(audio_array) / SAMPLE_RATE
    logger.debug(f"INFERENCE START: sending {audio_duration:.1f}s audio to whisper-server port {_whisper_server_port}")
    t0 = time.time()
    wav_data = _audio_to_wav_bytes(audio_array)
    fields = {
        "temperature": "0.0",
        "temperature_inc": "0.2",
        "response_format": "verbose_json",
    }
    if language:
        fields["language"] = language
    if initial_prompt:
        fields["prompt"] = initial_prompt
    files = {"file": ("audio.wav", wav_data, "audio/wav")}

    try:
        result = _post_multipart(
            f"http://127.0.0.1:{_whisper_server_port}/inference",
            fields, files, timeout=120,
        )
    except Exception as e:
        elapsed = time.time() - t0
        logger.error(f"whisper-server /inference failed after {elapsed:.1f}s: {e}")
        return None, None

    elapsed = time.time() - t0
    raw_segments = result.get("segments", [])
    detected_lang = result.get("language", "")
    logger.debug(f"INFERENCE DONE: {elapsed:.1f}s elapsed, {len(raw_segments)} segments, lang={detected_lang}")
    if raw_segments:
        for i, seg in enumerate(raw_segments):
            logger.debug(f"  SEG[{i}] t={seg.get('start',0):.1f}-{seg.get('end',0):.1f}s no_speech={seg.get('no_speech_prob',0):.2f} text='{seg.get('text','').strip()}'")

    segments = []
    for seg in raw_segments:
        # Parse word-level timestamps from DTW tokens (if available)
        words = []
        for tok in seg.get("tokens", []):
            if not isinstance(tok, dict):
                continue
            t_from = tok.get("t_dtw_from", -1)
            t_to = tok.get("t_dtw_to", -1)
            if t_from < 0 and "offsets" in tok:
                offsets = tok["offsets"]
                t_from = offsets.get("from", -1)
                t_to = offsets.get("to", -1)
            tok_text = tok.get("text", "")
            if tok_text and t_from >= 0 and t_to >= 0:
                words.append(WordInfo(
                    word=tok_text,
                    start=t_from / 1000.0,
                    end=t_to / 1000.0,
                    probability=tok.get("p", 0.0),
                ))
        segments.append(SegmentInfo(
            start=seg.get("start", 0.0),
            end=seg.get("end", 0.0),
            text=seg.get("text", ""),
            no_speech_prob=seg.get("no_speech_prob", 0.0),
            avg_logprob=seg.get("avg_logprob", 0.0),
            words=words,
        ))

    return segments, TranscribeInfo(language=detected_lang)


# ---------------------------------------------------------------------------
# SSE helpers
# ---------------------------------------------------------------------------
def broadcast_event(event_type: str, text: str, lang: str = ""):
    """Send an SSE event to all connected subscribers."""
    payload = {"text": text}
    if lang:
        payload["lang"] = lang
    data = json.dumps(payload)
    with subscribers_lock:
        for q in subscribers:
            try:
                q.put_nowait((event_type, data))
            except asyncio.QueueFull:
                pass


# ---------------------------------------------------------------------------
# Hallucination detection
# ---------------------------------------------------------------------------

# Language-to-script mapping for mismatch detection
_LANG_SCRIPT = {
    "ru": "cyrillic", "uk": "cyrillic", "bg": "cyrillic", "sr": "cyrillic", "mk": "cyrillic", "be": "cyrillic",
    "ja": "cjk", "zh": "cjk", "ko": "cjk",
    "ar": "arabic", "fa": "arabic", "ur": "arabic",
    "hi": "devanagari", "mr": "devanagari", "ne": "devanagari",
    "th": "thai", "ka": "georgian", "hy": "armenian", "he": "hebrew", "el": "greek",
}


def _is_script_mismatch(lang_a: str, lang_b: str) -> bool:
    """Return True if two languages use different scripts (Latin vs Cyrillic, etc.)."""
    script_a = _LANG_SCRIPT.get(lang_a, "latin")
    script_b = _LANG_SCRIPT.get(lang_b, "latin")
    return script_a != script_b


def _is_hallucination(segments, last_commit_text: str = "", detected_lang: str = "", recent_langs: list = None) -> bool:
    """Detect likely hallucinations using segment metadata.
    High no_speech_prob or very low avg_logprob on short segments = hallucination.
    Also detects repetition of previously committed text and self-repetition."""
    if not segments:
        return True
    total_speech_dur = sum(seg.end - seg.start for seg in segments)
    avg_no_speech = sum(seg.no_speech_prob for seg in segments) / len(segments)
    avg_logprob = sum(seg.avg_logprob for seg in segments) / len(segments)

    # Very high no-speech probability — almost certainly not real speech
    if avg_no_speech >= 0.85:
        return True
    # Short audio (< 1.5s) with high no-speech probability
    if total_speech_dur < 1.5 and avg_no_speech >= 0.7:
        return True
    # Single word, short duration, moderate no-speech — catches filler
    word_count = sum(1 for seg in segments for w in seg.text.split() if w.strip())
    if word_count <= 1 and total_speech_dur < 2.0 and avg_no_speech > 0.45:
        return True

    # Low confidence on very short audio (likely hallucinated filler)
    if avg_logprob < -0.8 and total_speech_dur < 1.0:
        return True

    full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())

    # Language-mismatch detection
    if detected_lang and recent_langs and len(recent_langs) >= 3:
        dominant_lang = max(set(recent_langs), key=recent_langs.count)
        if detected_lang != dominant_lang and _is_script_mismatch(detected_lang, dominant_lang):
            if avg_no_speech > 0.3 or avg_logprob < -0.2:
                logger.debug(f"  LANG MISMATCH: '{detected_lang}' vs dominant '{dominant_lang}' "
                             f"(no_speech={avg_no_speech:.2f}, logprob={avg_logprob:.2f})")
                return True

    # Known hallucination phrases
    if _is_known_hallucination(full_text):
        return True

    # Self-repetition: only catch near-exact looping (>90% similar halves)
    words = [re.sub(r"[^\w]", "", w) for w in full_text.lower().split()]
    words = [w for w in words if w]
    if len(words) >= 8:
        mid = len(words) // 2
        first_half = " ".join(words[:mid])
        second_half = " ".join(words[mid:])
        half_ratio = SequenceMatcher(None, first_half, second_half).ratio()
        if half_ratio > 0.9:
            logger.debug(f"  SELF-REPETITION detected: halves {half_ratio:.0%} similar")
            return True

    # Repetition of previous commit
    if last_commit_text:
        norm_new = " ".join(re.sub(r"[^\w\s]", "", full_text.lower()).split())
        norm_prev = " ".join(re.sub(r"[^\w\s]", "", last_commit_text.lower()).split())
        norm_new = re.sub(r"^\d+\s*", "", norm_new)
        norm_prev = re.sub(r"^\d+\s*", "", norm_prev)
        if len(norm_prev) <= 20:
            if norm_new == norm_prev:
                logger.debug("  REPETITION detected (exact short match)")
                return True
        else:
            ratio = SequenceMatcher(None, norm_new, norm_prev).ratio()
            if ratio > 0.85:
                logger.debug(f"  REPETITION detected (similarity={ratio:.0%}, {total_speech_dur:.1f}s)")
                return True

    return False


# ---------------------------------------------------------------------------
# Known hallucination phrase blocklist
# ---------------------------------------------------------------------------
_hallucination_phrases = []  # list of str


def _load_hallucination_phrases():
    """Load known hallucination phrases from hallucinations.json."""
    global _hallucination_phrases
    json_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "hallucinations.json")
    try:
        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        phrases = []
        for lang_phrases in data.values():
            for p in lang_phrases:
                phrases.append(p.lower())
        _hallucination_phrases = phrases
        logger.debug(f"Loaded {len(phrases)} hallucination phrases from {json_path}")
    except FileNotFoundError:
        logger.debug("No hallucinations.json found — phrase filter disabled")
    except Exception as e:
        logger.warning(f"Failed to load hallucinations.json: {e}")


def _is_known_hallucination(text: str) -> bool:
    """Check if text closely matches a known hallucination phrase."""
    if not _hallucination_phrases:
        return False
    cleaned = re.sub(r"[^\w\s]", "", text.lower()).strip()
    if not cleaned:
        return False
    for phrase in _hallucination_phrases:
        phrase_clean = re.sub(r"[^\w\s]", "", phrase).strip()
        if not phrase_clean:
            continue
        ratio = SequenceMatcher(None, cleaned, phrase_clean).ratio()
        if ratio > 0.8:
            logger.debug(f"  KNOWN HALLUCINATION ({ratio:.0%} match): '{text}' ~ '{phrase}'")
            return True
    return False


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------
@app.get("/health")
async def health():
    ws_running = _whisper_server_process is not None and _whisper_server_process.poll() is None
    result = {
        "status": "ok",
        "whisper_server_running": ws_running,
        "capturing": capturing,
    }
    if _vad_pipeline is not None:
        alive, reason = _vad_pipeline.is_alive()
        result["pipeline_alive"] = alive
        result["pipeline_status"] = reason
        if not alive:
            result["status"] = "degraded"
    return result


@app.get("/devices")
async def get_devices():
    devices = sd.query_devices()
    try:
        default_input = sd.query_devices(sd.default.device[0])
        default_api = default_input["hostapi"]
    except Exception:
        default_api = None
    result = []
    for i, d in enumerate(devices):
        if d["max_input_channels"] > 0:
            if default_api is None or d["hostapi"] == default_api:
                result.append({"id": i, "name": d["name"]})
    return {"devices": result}


@app.post("/start")
async def start_capture_endpoint(request: Request):
    global capturing, _vad_pipeline, current_config, current_stats, model_path_global

    if capturing:
        return JSONResponse({"status": "already_capturing"}, status_code=409)

    body = await request.json()
    current_config = body

    # Start whisper-server if needed
    requested_model_path = body.get("model_path", model_path_global)
    if _whisper_server_process is None or _whisper_server_process.poll() is not None:
        if not _whisper_server_path:
            return JSONResponse({"status": "error", "detail": "whisper-server path not configured"}, status_code=500)
        if not requested_model_path:
            return JSONResponse({"status": "error", "detail": "Model path not configured"}, status_code=500)
        try:
            ws_port = body.get("whisper_server_port", _whisper_server_port or 8178)
            _start_whisper_server(_whisper_server_path, requested_model_path, ws_port, _no_gpu)
        except Exception as e:
            logger.error(f"Failed to start whisper-server: {e}")
            return JSONResponse({"status": "error", "detail": str(e)}, status_code=500)
    model_path_global = requested_model_path

    # Require Silero VAD + pipeline
    if not _has_silero_vad or not _has_vad_pipeline:
        detail = "Silero VAD not available" if not _has_silero_vad else "VAD pipeline import failed"
        return JSONResponse(
            {"status": "error", "detail": f"{detail} (torch/silero-vad not installed)"},
            status_code=500
        )

    # Create VAD config from request body
    vad_config = VadConfig(
        device_index=body.get("device_index", 0),
        language=body.get("language", "auto"),
        beam_size=body.get("beam_size", 5),
        initial_prompt=body.get("initial_prompt", ""),
        soft_commit_ms=body.get("soft_commit_ms", 400),
        vad_silence_ms=body.get("vad_min_silence_ms", 750),
        vad_max_segment_s=body.get("vad_max_segment_s", 25),
        vad_preroll_ms=body.get("vad_preroll_ms", 400),
        vad_speech_threshold=body.get("vad_speech_threshold", 0.6),
        vad_silence_threshold=body.get("vad_silence_threshold", 0.4),
        vad_speech_confirm_frames=body.get("vad_speech_confirm_frames", 2),
        merge_similarity_threshold=body.get("merge_similarity_threshold", 0.75),
        enable_interim=body.get("enable_interim", True),
        interim_interval_s=body.get("interim_interval_s",
                                    body.get("interim_interval_ms", 3000) / 1000.0),
        enable_sentence_split=body.get("enable_sentence_split", True),
    )

    # Create session stats
    stats = SessionStats()
    current_stats = stats

    # Create and start pipeline
    pipeline = VadPipeline(
        silero_model=_silero_vad_model,
        config=vad_config,
        transcribe_fn=_transcribe_whisper_cpp,
        broadcast_fn=broadcast_event,
        hallucination_fn=_is_hallucination,
        stats=stats,
    )
    try:
        pipeline.start()
    except Exception as e:
        logger.error(f"Failed to start pipeline: {e}")
        return JSONResponse({"status": "error", "detail": f"Audio device error: {e}"}, status_code=500)

    _vad_pipeline = pipeline
    capturing = True
    logger.debug(f"CAPTURE START device={vad_config.device_index} lang={vad_config.language}")

    return {"status": "started"}


@app.post("/stop")
async def stop_capture_endpoint():
    global capturing, _vad_pipeline
    if not capturing:
        return {"status": "not_capturing"}

    if _vad_pipeline:
        _vad_pipeline.stop()
        _vad_pipeline = None

    capturing = False
    logger.debug("Capture stopped via /stop")
    return {"status": "stopped"}


@app.post("/config")
async def update_config(request: Request):
    """Update live-adjustable config values without restarting capture."""
    body = await request.json()
    updated = []
    for key in ("vad_min_silence_ms", "vad_max_segment_s", "soft_commit_ms", "language"):
        if key in body:
            current_config[key] = body[key]
            updated.append(key)

    # Forward to pipeline for runtime update
    if _vad_pipeline:
        _vad_pipeline.update_config(**{k: body[k] for k in updated if k in body})

    logger.debug(f"CONFIG UPDATE: {', '.join(f'{k}={current_config.get(k)}' for k in updated)}")
    return {"status": "ok", "updated": updated}


@app.get("/stats")
async def get_stats():
    """Return current session statistics for tuning recommendations."""
    if current_stats is None:
        return {"status": "no_session"}
    return current_stats.to_dict()


@app.post("/hallucinations/reload")
async def reload_hallucinations():
    """Reload hallucination phrases from disk."""
    _load_hallucination_phrases()
    return {"status": "ok", "count": len(_hallucination_phrases)}


@app.post("/load-model")
async def load_model_endpoint(request: Request):
    """Load the Whisper model without starting audio capture.
    Used by conversation rooms that need /transcribe but not live capture."""
    global model_path_global

    body = await request.json()
    requested_model_path = body.get("model_path", model_path_global)

    # Start whisper-server if not already running
    if _whisper_server_process is None or _whisper_server_process.poll() is not None:
        if not _whisper_server_path:
            return JSONResponse({"status": "error", "detail": "whisper-server path not configured"}, status_code=500)
        try:
            ws_port = _whisper_server_port or 8178
            _start_whisper_server(_whisper_server_path, requested_model_path, ws_port, _no_gpu)
        except Exception as e:
            logger.error(f"Failed to start whisper-server: {e}")
            return JSONResponse({"status": "error", "detail": str(e)}, status_code=500)
    model_path_global = requested_model_path
    return {"status": "loaded"}


@app.post("/transcribe")
async def transcribe_audio(request: Request):
    """One-shot transcription of uploaded audio data.
    Accepts WAV, MP3, or raw 16kHz float32 PCM.
    Used by conversation rooms for push-to-talk and benchmarks."""

    if _whisper_server_process is None or _whisper_server_process.poll() is not None:
        return JSONResponse({"status": "error", "detail": "whisper-server not running"}, status_code=503)

    body = await request.body()
    if not body:
        return JSONResponse({"status": "error", "detail": "No audio data"}, status_code=400)

    lang = request.query_params.get("lang", None)
    if lang == "auto":
        lang = None

    audio = None
    content_type = request.headers.get("content-type", "")

    # Try WAV first
    try:
        wav_io = io.BytesIO(body)
        with wave.open(wav_io, "rb") as wf:
            frames = wf.readframes(wf.getnframes())
            sample_rate = wf.getframerate()
            audio = np.frombuffer(frames, dtype=np.int16).astype(np.float32) / 32768.0
            if sample_rate != SAMPLE_RATE:
                ratio = SAMPLE_RATE / sample_rate
                new_len = int(len(audio) * ratio)
                indices = np.linspace(0, len(audio) - 1, new_len)
                audio = np.interp(indices, np.arange(len(audio)), audio).astype(np.float32)
    except Exception:
        pass

    # If WAV failed and content looks like compressed audio, decode via ffmpeg
    if audio is None and (content_type in ("audio/mpeg", "audio/mp3", "audio/opus", "audio/ogg")
                          or body[:3] == b"ID3" or body[:2] == b"\xff\xfb" or body[:4] == b"OggS"):
        try:
            with tempfile.NamedTemporaryFile(suffix=".audio", delete=False) as tmp_in:
                tmp_in.write(body)
                tmp_in_path = tmp_in.name
            tmp_out_path = tmp_in_path + ".wav"
            try:
                result = subprocess.run(
                    ["ffmpeg", "-y", "-i", tmp_in_path, "-ar", "16000", "-ac", "1", "-f", "wav", tmp_out_path],
                    capture_output=True, timeout=30)
                if result.returncode == 0:
                    with wave.open(tmp_out_path, "rb") as wf:
                        frames = wf.readframes(wf.getnframes())
                        audio = np.frombuffer(frames, dtype=np.int16).astype(np.float32) / 32768.0
            finally:
                try:
                    os.unlink(tmp_in_path)
                except Exception:
                    pass
                try:
                    os.unlink(tmp_out_path)
                except Exception:
                    pass
        except Exception as e:
            logger.warning(f"ffmpeg decode failed: {e}")

    # Last resort: treat as raw 16kHz float32 PCM
    if audio is None:
        audio = np.frombuffer(body, dtype=np.float32)

    if len(audio) < SAMPLE_RATE * 0.3:
        return JSONResponse({"status": "error", "detail": "Audio too short"}, status_code=400)

    try:
        segments, info = _transcribe_whisper_cpp(audio, language=lang, beam_size=5)
        text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
        detected = info.language if info else ""

        if not text:
            return {"status": "ok", "text": "", "lang": detected}

        if _is_hallucination(segments):
            logger.debug(f"TRANSCRIBE-API: hallucination skipped: {text}")
            return {"status": "ok", "text": "", "lang": detected}

        logger.debug(f"TRANSCRIBE-API [{detected}]: {text}")
        return {"status": "ok", "text": text, "lang": detected}

    except Exception as e:
        import traceback
        logger.error(f"Transcribe API error: {e}\n{traceback.format_exc()}")
        return JSONResponse({"status": "error", "detail": str(e)}, status_code=500)


@app.post("/shutdown")
async def shutdown():
    """Gracefully shut down the server."""
    global capturing, _vad_pipeline, _shutting_down
    logger.debug("Shutdown requested")
    _shutting_down = True

    # Stop capture if running
    if capturing and _vad_pipeline:
        _vad_pipeline.stop()
        _vad_pipeline = None
        capturing = False

    # Stop whisper-server subprocess if running
    _stop_whisper_server()

    # Schedule server shutdown
    if _server is not None:
        _server.should_exit = True

    return {"status": "shutting_down"}


@app.get("/stream")
async def stream_events(request: Request):
    """SSE endpoint. Sends update and commit events."""
    q: asyncio.Queue = asyncio.Queue(maxsize=100)
    with subscribers_lock:
        subscribers.append(q)

    async def event_generator():
        try:
            while not _shutting_down:
                if await request.is_disconnected():
                    break
                try:
                    event_type, data = await asyncio.wait_for(q.get(), timeout=2.0)
                    yield {"event": event_type, "data": data}
                except asyncio.TimeoutError:
                    if _shutting_down:
                        break
                    # Send keepalive comment
                    yield {"comment": "keepalive"}
        finally:
            with subscribers_lock:
                if q in subscribers:
                    subscribers.remove(q)

    return EventSourceResponse(event_generator())


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Live transcription server")
    parser.add_argument("--port", type=int, default=5091)
    parser.add_argument("--host", type=str, default="127.0.0.1")
    parser.add_argument("--backend", type=str, default="whisper-cpp",
                        help="(ignored, kept for backward compatibility)")
    parser.add_argument("--whisper-server-path", type=str, default="",
                        help="Path to whisper-server.exe")
    parser.add_argument("--whisper-server-port", type=int, default=8178,
                        help="Port for whisper-server.exe (default: 8178)")
    parser.add_argument("--no-gpu", action="store_true",
                        help="Disable GPU (CPU-only mode for whisper-cpp)")
    parser.add_argument("--vad-model-path", type=str, default="",
                        help="(ignored, kept for backward compatibility)")
    args = parser.parse_args()

    _whisper_server_path = args.whisper_server_path
    _whisper_server_port = args.whisper_server_port
    _no_gpu = args.no_gpu

    logger.debug("Running in whisper-cpp mode with VAD pipeline")

    _load_hallucination_phrases()

    import uvicorn

    config = uvicorn.Config(app, host=args.host, port=args.port, log_level="warning")
    _server = uvicorn.Server(config)
    _server.run()
