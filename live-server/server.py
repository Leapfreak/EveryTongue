"""
Live Transcription Server for Every Tongue.
FastAPI server: sounddevice audio capture -> Silero VAD -> whisper-server -> SSE events.
"""

import argparse
import asyncio
import io
import json
import logging
import logging.handlers
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

# Ensure script directory is on sys.path (embedded Python's ._pth file excludes it)
_script_dir = os.path.dirname(os.path.abspath(__file__))
if _script_dir not in sys.path:
    sys.path.insert(0, _script_dir)

import numpy as np
import sounddevice as sd
from difflib import SequenceMatcher
from fastapi import FastAPI, Request, WebSocket, WebSocketDisconnect
from fastapi.responses import JSONResponse
from sse_starlette.sse import EventSourceResponse

# Silero VAD — standalone package for speech detection
try:
    import torch
    from silero_vad import load_silero_vad
    _silero_vad_model = load_silero_vad()
    _has_silero_vad = True
except Exception as _vad_err:
    _has_silero_vad = False
    _silero_vad_model = None
    print(f"[LIVE] WARNING: Silero VAD import failed: {_vad_err}", file=sys.stderr)
    sys.stderr.flush()

# VAD pipeline — lazy-safe import (depends on torch via frame_vad.py)
# Catch Exception (not just ImportError) because torch can raise OSError,
# RuntimeError, etc. during import depending on CUDA/GPU state.
try:
    from vad import VadPipeline, VadConfig
    from vad.segment import SessionStats
    # The vad package itself imports without torch (VadConfig/SessionStats are
    # torch-free); VadPipeline is None when torch is absent — streaming engines
    # still work, only the offline whisper path is unavailable.
    _has_vad_pipeline = VadPipeline is not None
except Exception as _vad_pipe_err:
    _has_vad_pipeline = False
    VadPipeline = None
    VadConfig = None
    SessionStats = None
    import traceback as _tb
    print(f"[LIVE] WARNING: VAD pipeline import failed: {_vad_pipe_err}", file=sys.stderr)
    print(_tb.format_exc(), file=sys.stderr)
    sys.stderr.flush()

# ---------------------------------------------------------------------------
# Pluggable STT engine registry. Online engines (Google gRPC, Speechmatics)
# register themselves on import; the local engines (whisper-cpp,
# faster-whisper) are registered further down, after their implementations.
# server.py dispatches everything through this registry — to add an online
# engine, drop a module in engines/ (see engines/common.py for the contract).
# ---------------------------------------------------------------------------
import engines
import sat_segmenter
from engines.common import (
    SAMPLE_RATE, SegmentInfo, WordInfo, TranscribeInfo, audio_to_wav_bytes,
)

# ---------------------------------------------------------------------------
# Logging — file-based via RotatingFileHandler
# ---------------------------------------------------------------------------
# Logs go to a file in --log-dir (passed by .NET). The file is tailed by
# PythonSidecarHost which feeds lines to AppLogger/UI. This completely avoids
# the 4KB Windows pipe buffer bottleneck that caused cascading deadlocks.
#
# QueueHandler ensures logger.info/debug() never blocks the caller — records
# go to an in-memory queue, drained by a background writer thread to the file.
# ---------------------------------------------------------------------------
import queue as _queue_mod

logger = logging.getLogger("live-server")
logger.setLevel(logging.DEBUG)
logger.propagate = False

_log_queue = _queue_mod.Queue(maxsize=5000)
_queue_handler = logging.handlers.QueueHandler(_log_queue)
_queue_handler.setLevel(logging.DEBUG)
logger.addHandler(_queue_handler)

# File handler — configured in __main__ once --log-dir is known.
# Until then, a fallback stderr handler catches early messages.
_file_handler = None
_active_handler = logging.StreamHandler()
_active_handler.setLevel(logging.INFO)
_active_handler.setFormatter(logging.Formatter("[LIVE] %(message)s"))


def _setup_file_logging(log_dir: str):
    """Switch from stderr fallback to RotatingFileHandler."""
    global _file_handler, _active_handler
    os.makedirs(log_dir, exist_ok=True)
    log_path = os.path.join(log_dir, "live-server.log")
    _file_handler = logging.handlers.RotatingFileHandler(
        log_path, maxBytes=2 * 1024 * 1024, backupCount=2, encoding="utf-8")
    _file_handler.setLevel(logging.DEBUG)
    _file_handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(message)s"))
    _active_handler = _file_handler


def _log_writer_thread():
    """Drain log queue to file (or stderr fallback). Runs as a daemon thread."""
    while True:
        try:
            record = _log_queue.get(timeout=1.0)
            handler = _active_handler
            if handler.level <= record.levelno:
                try:
                    handler.emit(record)
                except Exception:
                    pass
        except _queue_mod.Empty:
            continue
        except Exception:
            break

_log_writer = threading.Thread(target=_log_writer_thread, daemon=True, name="log-writer")
_log_writer.start()


def _apply_log_level(level_name: str):
    """Set file handler level from config: minimal/normal/verbose."""
    level_map = {"minimal": logging.WARNING, "normal": logging.INFO, "verbose": logging.DEBUG}
    if _file_handler:
        _file_handler.setLevel(level_map.get(level_name.lower(), logging.DEBUG))
    else:
        _active_handler.setLevel(level_map.get(level_name.lower(), logging.INFO))

# Suppress noisy loggers
logging.basicConfig(level=logging.WARNING)
logging.getLogger("uvicorn").setLevel(logging.WARNING)
logging.getLogger("uvicorn.access").setLevel(logging.WARNING)


app = FastAPI()

# Log import status now that the logger is ready
logger.debug(f"Silero VAD: {'loaded' if _has_silero_vad else 'NOT AVAILABLE'}")
logger.debug(f"VAD pipeline: {'loaded' if _has_vad_pipeline else 'NOT AVAILABLE'}")
if not _has_vad_pipeline:
    logger.warning("VAD pipeline unavailable (torch not installed) — offline whisper "
                   "engines are blocked; online streaming engines still work")

# ---------------------------------------------------------------------------
# Global state
# ---------------------------------------------------------------------------
model_path_global: str = ""

capturing: bool = False
_vad_pipeline = None  # type: VadPipeline or None

# SSE subscribers (asyncio queues)
subscribers = []  # list of asyncio.Queue
subscribers_lock: threading.Lock = threading.Lock()

# Audio config — SAMPLE_RATE imported from engines.common.

# Capture config (set via /start, read by /config)
current_config: dict = {}

# Current session stats (accessible via /stats endpoint)
current_stats = None  # type: SessionStats or None

# Uvicorn server reference for graceful shutdown
_server = None
_shutting_down: bool = False

# Backend selection — set from --backend arg in __main__. All engines (local
# whisper-cpp/faster-whisper and online) are dispatched through the engines
# registry; local engines are registered below at module load.
_backend_mode: str = "whisper-cpp"

# whisper-server process management (whisper-cpp backend)
_whisper_server_path: str = ""
_whisper_server_port: int = 0
_no_gpu: bool = False
_whisper_server_process = None  # type: subprocess.Popen or None


# SegmentInfo / WordInfo / TranscribeInfo are imported from engines.common —
# shared between whisper-server, faster-whisper, and the Google REST engine.


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

    # whisper.cpp's ggml installs a std::terminate handler in a static initializer
    # (ggml.cpp) that GGML_ASSERTs if it runs twice. It DOES run twice when ggml is
    # present in more than one loaded module (ggml-base + ggml-cpu + ggml-cuda/-vulkan;
    # note backends=2). The failed assert aborts immediately after device init, before
    # the model loads, surfacing as exit 3221226505 / 0xC0000409. GGML_NO_BACKTRACE
    # makes the initializer return early — before the assert and set_terminate — so the
    # handler is never double-registered. Affects both the CUDA and Vulkan builds.
    ws_env = dict(os.environ)
    ws_env["GGML_NO_BACKTRACE"] = "1"

    _whisper_server_process = subprocess.Popen(
        cmd, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE,
        creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        env=ws_env,
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
    # Keep the last few stderr lines so a launch failure can report the actual cause
    # (e.g. a GGML_ASSERT / CUDA error) instead of just an opaque exit code.
    _ws_stderr_tail: list = []

    def _drain_whisper_stderr(proc):
        try:
            for line in proc.stderr:
                decoded = line.decode(errors="replace").rstrip()
                if decoded:
                    _ws_stderr_tail.append(decoded)
                    del _ws_stderr_tail[:-15]
                    logger.debug(f"WHISPER-SERVER: {decoded}")
        except Exception:
            pass
    _ws_drain = threading.Thread(target=_drain_whisper_stderr, args=(_whisper_server_process,), daemon=True)
    _ws_drain.start()

    # Wait for server to be ready (up to 60s for large model load)
    for i in range(120):
        if _whisper_server_process.poll() is not None:
            time.sleep(0.1)  # let the drain thread flush the final stderr lines
            tail = " | ".join(_ws_stderr_tail[-6:]) if _ws_stderr_tail else "(no stderr captured)"
            raise RuntimeError(
                f"whisper-server exited with code {_whisper_server_process.returncode}. Last output: {tail}")
        try:
            req = urllib.request.Request(f"http://127.0.0.1:{port}/health")
            with urllib.request.urlopen(req, timeout=1) as resp:
                if resp.status == 200:
                    logger.info(f"WHISPER-SERVER READY on port {port} (took ~{i * 0.5:.0f}s)")
                    _warmup_whisper_server(port)
                    return
        except Exception:
            pass
        time.sleep(0.5)
    raise RuntimeError("whisper-server startup timeout (60s)")


def _warmup_whisper_server(port: int):
    """Run one throwaway inference so the first REAL request doesn't pay GPU
    warm-up (CUDA graph init / Vulkan shader compile — 15-30s on some GPUs).
    /health returns 200 once the model WEIGHTS are loaded, so without this the
    app's readiness signals (dictation chime, room banners) fire before the
    engine can actually transcribe promptly."""
    try:
        t0 = time.time()
        silence = np.zeros(SAMPLE_RATE, dtype=np.float32)  # 1s of silence
        wav_data = audio_to_wav_bytes(silence)
        _post_multipart(
            f"http://127.0.0.1:{port}/inference",
            {"temperature": "0.0", "response_format": "verbose_json"},
            {"file": ("warmup.wav", wav_data, "audio/wav")},
            timeout=120,
        )
        logger.info(f"WHISPER-SERVER warm-up inference done in {time.time() - t0:.1f}s")
    except Exception as e:
        logger.warning(f"WHISPER-SERVER warm-up inference failed (non-fatal): {e}")


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


_whisper_lock = threading.Lock()

def _transcribe_whisper_cpp(audio_array: np.ndarray, language=None,
                            beam_size=5, best_of=1, initial_prompt=""):
    """Transcribe audio via whisper-server.exe /inference endpoint.
    Serialized with _whisper_lock -- whisper-server hangs on concurrent requests.
    Returns (segments, info) or (None, None) on error."""
    audio_duration = len(audio_array) / SAMPLE_RATE
    logger.debug(
        f"INFERENCE START: {audio_duration:.1f}s audio to port {_whisper_server_port} "
        f"beam={beam_size} best_of={best_of} lang={language} prompt_len={len(initial_prompt)}"
    )
    t0 = time.time()
    wav_data = audio_to_wav_bytes(audio_array)
    fields = {
        "temperature": "0.0",
        "temperature_inc": "0.2",
        "response_format": "verbose_json",
    }
    if beam_size and beam_size > 0:
        fields["beam_size"] = str(beam_size)
    if best_of and best_of > 1:
        fields["best_of"] = str(best_of)
    if language:
        fields["language"] = language
    if initial_prompt:
        fields["prompt"] = initial_prompt
    files = {"file": ("audio.wav", wav_data, "audio/wav")}

    with _whisper_lock:
        try:
            result = _post_multipart(
                f"http://127.0.0.1:{_whisper_server_port}/inference",
                fields, files, timeout=30,
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
# faster-whisper transcription (CTranslate2 backend)
# ---------------------------------------------------------------------------
_faster_whisper_model = None
_faster_whisper_lock = threading.Lock()

def _load_faster_whisper_model(model_path, device="cuda", compute_type="int8_float16"):
    """Load the faster-whisper model (once). Thread-safe."""
    global _faster_whisper_model
    if _faster_whisper_model is not None:
        return _faster_whisper_model
    with _faster_whisper_lock:
        if _faster_whisper_model is not None:
            return _faster_whisper_model
        from faster_whisper import WhisperModel
        logger.info(f"Loading faster-whisper model from {model_path} on {device} ({compute_type})")
        _faster_whisper_model = WhisperModel(
            model_path, device=device, compute_type=compute_type
        )
        logger.info(f"faster-whisper model loaded on {device}")
        return _faster_whisper_model


def _transcribe_faster_whisper(audio_array: np.ndarray, language=None,
                               beam_size=5, best_of=1, initial_prompt=""):
    """Transcribe audio via faster-whisper (CTranslate2).
    Returns (segments, info) matching the same interface as _transcribe_whisper_cpp."""
    if _faster_whisper_model is None:
        logger.error("faster-whisper model not loaded")
        return None, None

    audio_duration = len(audio_array) / SAMPLE_RATE
    logger.debug(
        f"INFERENCE START: {audio_duration:.1f}s audio via faster-whisper "
        f"beam={beam_size} best_of={best_of} lang={language} prompt_len={len(initial_prompt)}"
    )
    t0 = time.time()

    fw_segments, fw_info = _faster_whisper_model.transcribe(
        audio_array,
        language=language if language and language != "auto" else None,
        beam_size=beam_size,
        best_of=best_of,
        initial_prompt=initial_prompt if initial_prompt else None,
        vad_filter=True,
        vad_parameters={
            "threshold": 0.3,
            "min_silence_duration_ms": 300,
            "max_speech_duration_s": 30,
            "speech_pad_ms": 100,
        },
        word_timestamps=True,
        no_repeat_ngram_size=3,
        repetition_penalty=1.1,
    )

    # faster-whisper returns a generator — consume it
    segments = []
    for seg in fw_segments:
        words = []
        if seg.words:
            for w in seg.words:
                words.append(WordInfo(
                    word=w.word,
                    start=w.start,
                    end=w.end,
                    probability=w.probability,
                ))
        segments.append(SegmentInfo(
            start=seg.start,
            end=seg.end,
            text=seg.text,
            no_speech_prob=seg.no_speech_prob,
            avg_logprob=seg.avg_logprob,
            words=words,
        ))

    detected_lang = fw_info.language if fw_info else ""
    elapsed = time.time() - t0
    logger.debug(f"INFERENCE DONE: {elapsed:.1f}s elapsed, {len(segments)} segments, lang={detected_lang}")
    return segments, TranscribeInfo(language=detected_lang)


# ---------------------------------------------------------------------------
# Local engine registration — whisper-cpp and faster-whisper go through the
# same engines registry as the online engines, so all dispatch is uniform.
# The implementations stay above (model loading / whisper-server process
# management); these thin adapters wrap them with the registry contract.
# ---------------------------------------------------------------------------
def _ensure_whisper_cpp_loaded(model_path, body):
    """Start whisper-server if needed. Returns (ok, detail)."""
    if _whisper_server_process is not None and _whisper_server_process.poll() is None:
        return True, ""
    if not _whisper_server_path:
        return False, "whisper-server path not configured"
    if not model_path:
        return False, "Model path not configured"
    try:
        ws_port = body.get("whisper_server_port", _whisper_server_port or 8178)
        _start_whisper_server(_whisper_server_path, model_path, ws_port, _no_gpu)
        return True, ""
    except Exception as e:
        logger.error(f"Failed to start whisper-server: {e}")
        return False, str(e)


def _whisper_cpp_ready():
    if _whisper_server_process is None or _whisper_server_process.poll() is not None:
        return False, "whisper-server not running"
    return True, ""


def _ensure_faster_whisper_loaded(model_path, body):
    """Load the faster-whisper model if needed. Returns (ok, detail)."""
    if not model_path:
        return False, "Model path not configured"
    try:
        device = body.get("device", "cuda")
        compute_type = body.get("compute_type", "int8_float16")
        _load_faster_whisper_model(model_path, device=device, compute_type=compute_type)
        return True, ""
    except Exception as e:
        logger.error(f"Failed to load faster-whisper model: {e}")
        return False, str(e)


def _faster_whisper_ready():
    if _faster_whisper_model is None:
        return False, "faster-whisper model not loaded"
    return True, ""


engines.register_engine(
    "whisper-cpp",
    requires_model=True,
    is_local=True,
    transcribe_fn=_transcribe_whisper_cpp,
    load_model=_ensure_whisper_cpp_loaded,
    is_ready=_whisper_cpp_ready,
)
engines.register_engine(
    "faster-whisper",
    requires_model=True,
    is_local=True,
    transcribe_fn=_transcribe_faster_whisper,
    load_model=_ensure_faster_whisper_loaded,
    is_ready=_faster_whisper_ready,
)


# ---------------------------------------------------------------------------
# Backend dispatcher — routes to the active engine via the registry.
# ---------------------------------------------------------------------------
def _transcribe(audio_array: np.ndarray, language=None,
                beam_size=5, best_of=1, initial_prompt=""):
    """Transcribe audio using whichever backend is configured."""
    fn = engines.get_transcribe_fn(_backend_mode)
    if fn is None:
        # Unknown backend — fall back to whisper-cpp (matches __main__ default).
        fn = engines.get_transcribe_fn("whisper-cpp")
    return fn(audio_array, language, beam_size, best_of, initial_prompt)


# ---------------------------------------------------------------------------
# SSE helpers
# ---------------------------------------------------------------------------
def broadcast_event(event_type: str, text: str, lang: str = "", translations: dict = None):
    """Send an SSE event to all connected subscribers.

    ``translations`` (optional) is a dict of {language_code: translated_text} for
    engines that translate inline (e.g. Speechmatics); it is attached to commit
    payloads so the .NET side can broadcast translated subtitles directly."""
    payload = {"text": text}
    if lang:
        payload["lang"] = lang
    if translations:
        payload["translations"] = translations
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

    full_text_preview = " ".join(seg.text.strip() for seg in segments if seg.text.strip())[:60]

    # Very high no-speech probability — almost certainly not real speech
    if avg_no_speech >= 0.85:
        logger.info(f"[HALLUCINATION] high no_speech={avg_no_speech:.2f}: '{full_text_preview}'")
        return True
    # Short audio (< 1.5s) with high no-speech probability
    if total_speech_dur < 1.5 and avg_no_speech >= 0.7:
        logger.info(f"[HALLUCINATION] short+no_speech (dur={total_speech_dur:.1f}s, nsp={avg_no_speech:.2f}): '{full_text_preview}'")
        return True
    # Single word, short duration, high no-speech — catches filler
    word_count = sum(1 for seg in segments for w in seg.text.split() if w.strip())
    if word_count <= 1 and total_speech_dur < 2.0 and avg_no_speech > 0.6:
        logger.info(f"[HALLUCINATION] single-word (dur={total_speech_dur:.1f}s, nsp={avg_no_speech:.2f}): '{full_text_preview}'")
        return True

    # Low confidence on very short audio (likely hallucinated filler)
    if avg_logprob < -0.8 and total_speech_dur < 1.0:
        logger.info(f"[HALLUCINATION] low-confidence (dur={total_speech_dur:.1f}s, logprob={avg_logprob:.2f}): '{full_text_preview}'")
        return True

    full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())

    # Language-mismatch detection
    if detected_lang and recent_langs and len(recent_langs) >= 3:
        dominant_lang = max(set(recent_langs), key=recent_langs.count)
        if detected_lang != dominant_lang and _is_script_mismatch(detected_lang, dominant_lang):
            if avg_no_speech > 0.3 or avg_logprob < -0.2:
                logger.info(f"[HALLUCINATION] lang mismatch: '{detected_lang}' vs dominant '{dominant_lang}' "
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
            logger.info(f"[HALLUCINATION] self-repetition: halves {half_ratio:.0%} similar")
            return True

    # Repetition of previous commit
    if last_commit_text:
        norm_new = " ".join(re.sub(r"[^\w\s]", "", full_text.lower()).split())
        norm_prev = " ".join(re.sub(r"[^\w\s]", "", last_commit_text.lower()).split())
        norm_new = re.sub(r"^\d+\s*", "", norm_new)
        norm_prev = re.sub(r"^\d+\s*", "", norm_prev)
        if len(norm_prev) <= 20:
            if norm_new == norm_prev:
                logger.info("[HALLUCINATION] repetition of previous commit (exact short match)")
                return True
        else:
            ratio = SequenceMatcher(None, norm_new, norm_prev).ratio()
            if ratio > 0.85:
                logger.info(f"[HALLUCINATION] repetition of previous commit (similarity={ratio:.0%}, {total_speech_dur:.1f}s)")
                return True

    return False


# ---------------------------------------------------------------------------
# Known hallucination phrase blocklist
# ---------------------------------------------------------------------------
_hallucination_phrases = []  # list of str
# Per-session override set via /start's optional hallucinations_path field.
# Each room runs its own live-server instance, so a process-wide override IS
# the per-room filter set. Empty = the default file next to this script.
_hallucinations_path_override = ""


def _load_hallucination_phrases():
    """Load known hallucination phrases from hallucinations.json (or the
    session's override path). Supports both legacy format (plain string arrays)
    and new format (objects with "text" and "enabled" fields). Disabled items
    are skipped.
    """
    global _hallucination_phrases
    json_path = _hallucinations_path_override or os.path.join(
        os.path.dirname(os.path.abspath(__file__)), "hallucinations.json")
    try:
        with open(json_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        phrases = []
        for lang_phrases in data.values():
            for p in lang_phrases:
                if isinstance(p, str):
                    # Legacy format: plain string (always enabled)
                    phrases.append(p.lower())
                elif isinstance(p, dict):
                    # New format: {"text": "...", "enabled": true/false}
                    if p.get("enabled", True):
                        phrases.append(p.get("text", "").lower())
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
            logger.info(f"[HALLUCINATION] known phrase ({ratio:.0%} match): '{text}' ~ '{phrase}'")
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
        # Web-mic sessions: "capturing" must mean frames are actually FLOWING,
        # not merely "session started" — readiness banners/chimes depend on it.
        if getattr(_vad_pipeline, "audio_source", "local") == "web":
            connected = bool(getattr(_vad_pipeline, "web_feed_recent", lambda: False)())
            result["web_mic_connected"] = connected
            result["capturing"] = capturing and connected
    return result


@app.websocket("/audio-in")
async def audio_ingest(ws: WebSocket):
    """Web-mic ingest: binary 16kHz mono s16 PCM frames pushed into the active
    streaming pipeline's queue (the app's hub relays the host browser's mic here).
    Only valid while a web-source session is capturing."""
    pipeline = _vad_pipeline
    if (not capturing or pipeline is None
            or getattr(pipeline, "audio_source", "local") != "web"
            or not hasattr(pipeline, "feed")):
        await ws.close(code=4003, reason="no web-audio session")
        return
    await ws.accept()
    frames = 0
    try:
        while True:
            data = await ws.receive_bytes()
            if data:
                pipeline.feed(data)
                frames += 1
    except WebSocketDisconnect:
        pass
    except Exception as e:
        logger.warning(f"[AUDIO-IN] ingest closed with error after {frames} frames: {e}")
    finally:
        logger.info(f"[AUDIO-IN] web-mic stream ended ({frames} frames)")


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

    # SaT sentence segmentation (downstream, engine-agnostic). Warm the model in
    # a background thread so the first real /segment on a pause isn't slow.
    if body.get("speechmatics_sat") or body.get("sat_segment"):
        sat_model = body.get("sat_model") or None
        threading.Thread(target=sat_segmenter.load, args=(sat_model,), daemon=True).start()

    # Per-session hallucination filter set (empty = the default file).
    global _hallucinations_path_override
    new_halluc_path = body.get("hallucinations_path", "") or ""
    if new_halluc_path != _hallucinations_path_override:
        _hallucinations_path_override = new_halluc_path
        _load_hallucination_phrases()
        logger.info(f"Hallucination filter set: {new_halluc_path or '(default)'} "
                    f"({len(_hallucination_phrases)} phrases)")

    # Start transcription backend
    requested_model_path = body.get("model_path", model_path_global)

    # Online engines carry their API key in the request body.
    stt_api_key = body.get("stt_api_key", "")
    if stt_api_key:
        engines.set_api_key(_backend_mode, stt_api_key)

    if not engines.requires_model(_backend_mode):
        # Online engine — no local model. Registered engines that need a key
        # require one to be present (set above or from a previous /start).
        if engines.is_registered(_backend_mode) and not engines.get_api_key(_backend_mode):
            return JSONResponse({"status": "error", "detail": f"{_backend_mode}: API key not provided"}, status_code=400)
        logger.info(f"{_backend_mode} backend configured (online, no local model)")
    else:
        # Local engine — load its model / start its server via the registry.
        ok, detail = engines.load_model(_backend_mode, requested_model_path, body)
        if not ok:
            return JSONResponse({"status": "error", "detail": detail}, status_code=500)
    model_path_global = requested_model_path

    # NOTE: the Silero/torch requirement is checked AFTER the streaming branch below —
    # self-endpointing streaming engines (Speechmatics, Deepgram, …) never touch the
    # VAD pipeline, so a torch-less install (Lite/Docker) can still run them.

    # Create VAD config from request body
    vad_config = VadConfig(
        device_index=body.get("device_index", 0),
        language=body.get("language", "auto"),
        beam_size=body.get("beam_size", 5),
        best_of=body.get("best_of", 1),
        initial_prompt=body.get("initial_prompt", ""),
        soft_commit_ms=body.get("soft_commit_ms", 400),
        vad_silence_ms=body.get("vad_min_silence_ms", 750),
        vad_max_segment_s=body.get("vad_max_segment_s", 25),
        vad_preroll_ms=body.get("vad_preroll_ms", 400),
        vad_speech_threshold=body.get("vad_speech_threshold", 0.7),
        vad_silence_threshold=body.get("vad_silence_threshold", 0.45),
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

    # Online streaming engines (self-endpointing) bypass the VAD pipeline —
    # they capture audio themselves and emit update/commit events directly.
    streaming = engines.create_streaming_pipeline(
        _backend_mode,
        api_key=engines.get_api_key(_backend_mode),
        config=vad_config,
        broadcast_fn=broadcast_event,
        stats=stats,
        options=body,
    )
    if streaming is not None:
        try:
            streaming.start()
        except Exception as e:
            logger.error(f"Failed to start {_backend_mode} streaming pipeline: {e}")
            return JSONResponse({"status": "error", "detail": f"Audio device error: {e}"}, status_code=500)
        _vad_pipeline = streaming
        capturing = True
        logger.info(
            f"CAPTURE START device={vad_config.device_index} lang={vad_config.language} "
            f"mode=streaming engine={_backend_mode}"
        )
        return {"status": "ok", "pipeline": "streaming", "engine": _backend_mode}

    # Offline whisper path from here on — NOW torch/silero are genuinely required.
    if not _has_silero_vad or not _has_vad_pipeline:
        detail = "Silero VAD not available" if not _has_silero_vad else "VAD pipeline import failed"
        return JSONResponse(
            {"status": "error", "detail": f"{detail} (torch/silero-vad not installed)"},
            status_code=500
        )

    # VAD-pipeline path (offline whisper engines, or an online engine's fallback
    # such as Google REST). Let the engine tweak VAD config if it wants to.
    engines.apply_vad_preset(_backend_mode, vad_config)
    # Default OFF: local whisper engines use the clean per-segment VadPipeline (the
    # healthy v1.8.6 path). The separate AccumulatingPipeline is opt-in only. Engines
    # that want audio accumulation enable it via their vad_preset (e.g. Google sets
    # accumulate_audio=True on the VadPipeline) — they do NOT need this class.
    use_accumulating = (body.get("use_accumulating_pipeline", False)
                        and not getattr(vad_config, "force_vad_pipeline", False))
    PipelineClass = VadPipeline
    if use_accumulating:
        try:
            from vad import AccumulatingPipeline
            if AccumulatingPipeline is not None:
                PipelineClass = AccumulatingPipeline
            else:
                logger.warning("AccumulatingPipeline is None (import failed at startup), using VadPipeline")
        except Exception as e:
            logger.warning(f"AccumulatingPipeline import failed: {e}, using VadPipeline")
    logger.info(f"Pipeline mode: {'accumulating' if PipelineClass.__name__ == 'AccumulatingPipeline' else 'vad-chunked'}")
    pipeline = PipelineClass(
        silero_model=_silero_vad_model,
        config=vad_config,
        transcribe_fn=_transcribe,
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
    logger.info(
        f"CAPTURE START device={vad_config.device_index} lang={vad_config.language} "
        f"beam_size={vad_config.beam_size} best_of={vad_config.best_of} "
        f"vad_silence={vad_config.vad_silence_ms}ms max_seg={vad_config.vad_max_segment_s}s"
    )

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
    for key in ("vad_min_silence_ms", "vad_max_segment_s", "soft_commit_ms",
                "language", "translation_targets"):
        if key in body:
            current_config[key] = body[key]
            updated.append(key)

    # Forward to pipeline for runtime update. reset_pace is a transient command
    # (not stored in current_config) — the host-pause / context reset trigger.
    forward = {k: body[k] for k in updated if k in body}
    if body.get("reset_pace"):
        forward["reset_pace"] = True
        updated.append("reset_pace")
    if _vad_pipeline and forward:
        _vad_pipeline.update_config(**forward)

    logger.info(f"CONFIG UPDATE: {', '.join(str(k) for k in updated)}")
    return {"status": "ok", "updated": updated}


@app.post("/segment")
async def segment_text(request: Request):
    """Split a buffered clause into proper sentences via SaT (engine-agnostic).
    Called by the .NET clause accumulator at a real pause. Falls back to the
    text unchanged if SaT is unavailable — never fails the caller."""
    body = await request.json()
    text = body.get("text", "") or ""
    threshold = float(body.get("threshold", 0.1))
    model = body.get("model") or None
    sentences = sat_segmenter.segment(text, threshold=threshold, model_name=model)
    return {"sentences": sentences, "available": sat_segmenter._available}


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

    # Conversation rooms deliver the STT key here (/start never runs on that path).
    lm_key = body.get("stt_api_key", "")
    if lm_key:
        engines.set_api_key(_backend_mode, lm_key)

    if not engines.requires_model(_backend_mode):
        # Online engine — no local model to load.
        pass
    else:
        # Local engine — load its model / start its server via the registry.
        ok, detail = engines.load_model(_backend_mode, requested_model_path, body)
        if not ok:
            return JSONResponse({"status": "error", "detail": detail}, status_code=500)
    model_path_global = requested_model_path
    return {"status": "loaded"}


@app.post("/transcribe")
async def transcribe_audio(request: Request):
    """One-shot transcription of uploaded audio data.
    Accepts WAV, MP3, or raw 16kHz float32 PCM.
    Used by conversation rooms for push-to-talk and benchmarks."""

    # Gate-check: ensure the active backend is ready
    if not engines.requires_model(_backend_mode):
        # Online engine. One-shot /transcribe needs a registry transcribe fn
        # (streaming-only engines like Speechmatics don't support it).
        if engines.get_transcribe_fn(_backend_mode) is None:
            return JSONResponse({"status": "error", "detail": f"{_backend_mode} does not support one-shot /transcribe"}, status_code=503)
        if engines.is_registered(_backend_mode) and not engines.get_api_key(_backend_mode):
            return JSONResponse({"status": "error", "detail": f"{_backend_mode}: API key not configured"}, status_code=503)
    else:
        # Local engine — registry readiness probe (model loaded / server running).
        ready, detail = engines.is_ready(_backend_mode)
        if not ready:
            return JSONResponse({"status": "error", "detail": detail}, status_code=503)

    body = await request.body()
    if not body:
        return JSONResponse({"status": "error", "detail": "No audio data"}, status_code=400)

    lang = request.query_params.get("lang", None)
    if lang == "auto":
        lang = None
    beam_size = int(request.query_params.get("beam_size", 5))
    best_of = int(request.query_params.get("best_of", 1))
    prompt = request.query_params.get("prompt", "")
    logger.info(
        f"TRANSCRIBE request: lang={lang} beam_size={beam_size} best_of={best_of} "
        f"prompt_len={len(prompt)} audio_bytes={len(body)}"
    )

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
        segments, info = _transcribe(audio, language=lang, beam_size=beam_size, best_of=best_of, initial_prompt=prompt)
        text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
        detected = info.language if info else ""

        if not text:
            return {"status": "ok", "text": "", "lang": detected}

        if _is_hallucination(segments):
            logger.info(f"[HALLUCINATION] transcribe-API blocked: {text}")
            return {"status": "ok", "text": "", "lang": detected}

        logger.debug(f"TRANSCRIBE-API [{detected}]: {text}")
        return {"status": "ok", "text": text, "lang": detected}

    except Exception as e:
        import traceback
        logger.error(f"Transcribe API error: {e}\n{traceback.format_exc()}")
        return JSONResponse({"status": "error", "detail": str(e)}, status_code=500)


# ---------------------------------------------------------------------------
# Benchmark endpoint — feed audio file through the real pipeline
# ---------------------------------------------------------------------------
@app.post("/benchmark")
async def benchmark_endpoint(request: Request):
    """Run a benchmark: feed an audio file through the pipeline and collect commits.

    Runs synchronously (blocks until audio is fully processed). Does NOT require
    or interfere with a live capture session.

    Request body (JSON):
    {
        "audio_path": "C:\\path\\to\\audio.wav",
        "language": "ca",
        "realtime_factor": 0.0,
        "pipeline": "vad" | "accumulating",
        "beam_size": 5, "best_of": 1, "temperature": 0.0,
        "initial_prompt": "", "no_repeat_ngram_size": 0, "repetition_penalty": 1.0,
        "vad_min_silence_ms": 750, "vad_max_segment_s": 25,
        "interim_interval_ms": 1000
    }
    """
    import soundfile  # lazy import — only needed for benchmark

    body = await request.json()
    audio_path = body.get("audio_path", "")
    if not audio_path or not os.path.isfile(audio_path):
        return JSONResponse({"status": "error", "detail": f"Audio file not found: {audio_path}"}, status_code=400)

    # Gate-check: ensure backend is ready
    if not engines.requires_model(_backend_mode):
        if engines.get_transcribe_fn(_backend_mode) is None:
            return JSONResponse({"status": "error", "detail": f"{_backend_mode} does not support /benchmark (streaming-only)"}, status_code=503)
        if engines.is_registered(_backend_mode) and not engines.get_api_key(_backend_mode):
            return JSONResponse({"status": "error", "detail": f"{_backend_mode}: API key not configured"}, status_code=503)
    else:
        # Local engine — registry readiness probe (model loaded / server running).
        ready, detail = engines.is_ready(_backend_mode)
        if not ready:
            return JSONResponse({"status": "error", "detail": detail}, status_code=503)

    # VAD is only needed by LOCAL engines' chunking; online one-shot engines
    # (Speechmatics) receive the complete utterance and endpoint server-side.
    if engines.requires_model(_backend_mode) and (not _has_silero_vad or not _has_vad_pipeline):
        return JSONResponse({"status": "error", "detail": "VAD pipeline not available"}, status_code=503)

    # Load audio file
    try:
        audio_data, sr = soundfile.read(audio_path, dtype="float32", always_2d=False)
        if len(audio_data.shape) > 1:
            audio_data = audio_data[:, 0]  # take first channel
        if sr != SAMPLE_RATE:
            # Simple linear resample
            ratio = SAMPLE_RATE / sr
            new_len = int(len(audio_data) * ratio)
            indices = np.linspace(0, len(audio_data) - 1, new_len)
            audio_data = np.interp(indices, np.arange(len(audio_data)), audio_data).astype(np.float32)
        total_duration = len(audio_data) / SAMPLE_RATE
    except Exception as e:
        return JSONResponse({"status": "error", "detail": f"Failed to load audio: {e}"}, status_code=400)

    logger.info(f"[BENCHMARK] Starting: {audio_path} ({total_duration:.1f}s), backend={_backend_mode}")

    # Parse parameters
    language = body.get("language", "auto")
    realtime_factor = float(body.get("realtime_factor", 0.0))
    pipeline_type = body.get("pipeline", "vad").lower()
    beam_size = int(body.get("beam_size", 5))
    best_of = int(body.get("best_of", 1))
    initial_prompt = body.get("initial_prompt", "")
    vad_min_silence_ms = int(body.get("vad_min_silence_ms", 750))
    vad_max_segment_s = int(body.get("vad_max_segment_s", 25))
    interim_interval_ms = int(body.get("interim_interval_ms", 1000))

    # Build config
    vad_config = VadConfig(
        device_index=0,
        language=language,
        beam_size=beam_size,
        best_of=best_of,
        initial_prompt=initial_prompt,
        vad_silence_ms=vad_min_silence_ms,
        vad_max_segment_s=vad_max_segment_s,
        interim_interval_s=interim_interval_ms / 1000.0,
        enable_interim=False,
        enable_sentence_split=True,
    )

    # Collect commits
    commits = []
    hallucination_count = [0]
    bench_start = time.time()

    def bench_broadcast(event_type, text, lang=""):
        if event_type == "commit":
            elapsed = time.time() - bench_start
            commits.append({"text": text, "lang": lang, "timestamp": round(elapsed, 2)})

    def bench_hallucination(segments, last_text="", detected_lang="", recent_langs=None):
        is_hal = _is_hallucination(segments, last_text, detected_lang, recent_langs)
        if is_hal:
            hallucination_count[0] += 1
        return is_hal

    # Create pipeline
    stats = SessionStats()
    use_accum = (pipeline_type == "accumulating")
    if use_accum:
        from vad import AccumulatingPipeline as BenchAccum
        if BenchAccum is None:
            return JSONResponse({"status": "error", "detail": "AccumulatingPipeline not available"}, status_code=503)
        pipeline = BenchAccum(
            silero_model=_silero_vad_model,
            config=vad_config,
            transcribe_fn=_transcribe,
            broadcast_fn=bench_broadcast,
            hallucination_fn=bench_hallucination,
            stats=stats,
        )
    else:
        pipeline = VadPipeline(
            silero_model=_silero_vad_model,
            config=vad_config,
            transcribe_fn=_transcribe,
            broadcast_fn=bench_broadcast,
            hallucination_fn=bench_hallucination,
            stats=stats,
        )

    # Run the benchmark
    done_event = threading.Event()
    pipeline.start_from_file(audio_data, realtime_factor=realtime_factor, done_event=done_event)

    # Wait for completion (with timeout: 3x audio duration + 60s margin)
    timeout = max(total_duration * 3, 120) + 60
    done_event.wait(timeout=timeout)

    # Ensure pipeline is cleaned up
    try:
        pipeline.stop()
    except Exception:
        pass

    bench_duration = time.time() - bench_start
    logger.info(
        f"[BENCHMARK] Done: {len(commits)} commits, {hallucination_count[0]} hallucinations, "
        f"{bench_duration:.1f}s elapsed for {total_duration:.1f}s audio"
    )

    parameters_used = {
        "backend": _backend_mode,
        "pipeline": pipeline_type,
        "language": language,
        "beam_size": beam_size,
        "best_of": best_of,
        "initial_prompt": initial_prompt,
        "vad_min_silence_ms": vad_min_silence_ms,
        "vad_max_segment_s": vad_max_segment_s,
        "interim_interval_ms": interim_interval_ms,
        "realtime_factor": realtime_factor,
    }

    return {
        "status": "ok",
        "commits": commits,
        "hallucination_count": hallucination_count[0],
        "total_duration_s": round(total_duration, 1),
        "elapsed_s": round(bench_duration, 1),
        "commit_count": len(commits),
        "parameters_used": parameters_used,
        "stats_summary": stats.summary() if stats else "",
    }


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
                        help="Transcription backend: whisper-cpp (via whisper-server.exe), "
                             "faster-whisper (CTranslate2), or any registered online engine "
                             f"({', '.join(engines.online_keys())})")
    parser.add_argument("--whisper-server-path", type=str, default="",
                        help="Path to whisper-server.exe")
    parser.add_argument("--whisper-server-port", type=int, default=8178,
                        help="Port for whisper-server.exe (default: 8178)")
    parser.add_argument("--no-gpu", action="store_true",
                        help="Disable GPU (CPU-only mode for whisper-cpp)")
    parser.add_argument("--log-level", type=str, default="normal",
                        choices=["minimal", "normal", "verbose"],
                        help="Log verbosity: minimal (errors only), normal (default), verbose (all debug)")
    parser.add_argument("--vad-model-path", type=str, default="",
                        help="(ignored, kept for backward compatibility)")
    parser.add_argument("--log-dir", type=str, default="",
                        help="Directory for log files (RotatingFileHandler)")
    args = parser.parse_args()

    if args.log_dir:
        _setup_file_logging(args.log_dir)

    _backend_mode = args.backend
    _valid_backends = set(engines.registered_keys())
    if _backend_mode not in _valid_backends:
        logger.warning(f"Unknown --backend '{_backend_mode}', defaulting to whisper-cpp")
        _backend_mode = "whisper-cpp"
    _whisper_server_path = args.whisper_server_path
    _whisper_server_port = args.whisper_server_port
    _no_gpu = args.no_gpu
    _apply_log_level(args.log_level)

    logger.info(f"Running in {_backend_mode} mode with VAD pipeline")

    _load_hallucination_phrases()

    import uvicorn

    config = uvicorn.Config(app, host=args.host, port=args.port, log_level="warning")
    _server = uvicorn.Server(config)
    _server.run()
