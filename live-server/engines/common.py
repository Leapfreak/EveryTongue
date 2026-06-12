"""Shared building blocks + plugin registry for live-server STT engines.

An "engine" is a transcription backend selected via ``--backend``. There are two
kinds:

  * Local engines (whisper-cpp, faster-whisper) run through the local VAD
    pipeline plus a *transcribe function* ``(audio, language, beam_size,
    best_of, initial_prompt) -> (segments, info)``. Their implementations live
    in server.py (model loading / whisper-server process management stays
    there), but server.py registers them here at startup (``is_local=True``)
    so all dispatch goes through this registry uniformly.

  * Online streaming engines (Google gRPC, Speechmatics) are *self-endpointing*:
    they capture/stream audio themselves and emit update/commit events directly,
    bypassing the VAD pipeline entirely. These register themselves here.

server.py stays engine-agnostic by going through this registry. To add an online
engine, create a module under ``engines/`` that calls :func:`register_engine` at
import time, then import that module from ``engines/__init__.py``.

Streaming-pipeline contract (what ``create_streaming`` must return):
    Constructed via ``create_streaming(api_key=, config=, broadcast_fn=,
    stats=, options=)`` and exposing:
        start()                       -> None      (begin capture/streaming)
        stop()                        -> None      (stop and join threads)
        is_alive()                    -> (bool, str)
        update_config(**kwargs)       -> None
        stats                         -> SessionStats-like (property)
    ``broadcast_fn(event_type, text, lang="")`` matches server.broadcast_event;
    ``config`` is the VadConfig (device_index, language); ``options`` is the raw
    ``/start`` request body (engine-specific keys, e.g. region/operating_point).
"""
import io
import threading
import wave

import numpy as np

# Audio sample rate used throughout the live-server (mono 16-bit PCM).
SAMPLE_RATE = 16000


# ---------------------------------------------------------------------------
# Normalized segment classes (bridge whisper-server JSON and internal pipeline)
# ---------------------------------------------------------------------------
class WordInfo:
    """Word-level timestamp."""
    __slots__ = ('word', 'start', 'end', 'probability')

    def __init__(self, word="", start=0.0, end=0.0, probability=0.0):
        self.word = word
        self.start = start
        self.end = end
        self.probability = probability


class SegmentInfo:
    """Transcription segment."""
    __slots__ = ('start', 'end', 'text', 'no_speech_prob', 'avg_logprob', 'words')

    def __init__(self, start=0.0, end=0.0, text="", no_speech_prob=0.0,
                 avg_logprob=0.0, words=None):
        self.start = start
        self.end = end
        self.text = text
        self.no_speech_prob = no_speech_prob
        self.avg_logprob = avg_logprob
        self.words = words or []


class TranscribeInfo:
    """Transcription metadata."""
    __slots__ = ('language',)

    def __init__(self, language=""):
        self.language = language


def audio_to_wav_bytes(audio_array: np.ndarray) -> bytes:
    """Convert float32 numpy audio to 16-bit WAV bytes."""
    buf = io.BytesIO()
    audio_int16 = (np.clip(audio_array, -1.0, 1.0) * 32767).astype(np.int16)
    with wave.open(buf, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(audio_int16.tobytes())
    return buf.getvalue()


# ---------------------------------------------------------------------------
# Per-engine API-key store (set from the /start body, read by engine modules)
# ---------------------------------------------------------------------------
_api_keys = {}
_api_keys_lock = threading.Lock()


def set_api_key(key: str, api_key: str):
    with _api_keys_lock:
        _api_keys[key] = api_key or ""


def get_api_key(key: str) -> str:
    with _api_keys_lock:
        return _api_keys.get(key, "")


# ---------------------------------------------------------------------------
# Engine registry
# ---------------------------------------------------------------------------
class _Engine:
    __slots__ = ('key', 'requires_model', 'create_streaming', 'transcribe_fn',
                 'vad_preset', 'is_local', 'load_model', 'is_ready')

    def __init__(self, key, requires_model, create_streaming, transcribe_fn,
                 vad_preset, is_local, load_model, is_ready):
        self.key = key
        self.requires_model = requires_model
        self.create_streaming = create_streaming
        self.transcribe_fn = transcribe_fn
        self.vad_preset = vad_preset
        self.is_local = is_local
        self.load_model = load_model
        self.is_ready = is_ready


_engines = {}


def register_engine(key, *, requires_model=True, create_streaming=None,
                    transcribe_fn=None, vad_preset=None, is_local=False,
                    load_model=None, is_ready=None):
    """Register an engine.

    Args:
        key: backend key, e.g. "google-cloud-stt" / "speechmatics".
        requires_model: whether a local model must be loaded before use.
        create_streaming: factory returning a streaming pipeline, or ``None`` to
            fall back to the VAD pipeline. May itself return ``None`` at call
            time (e.g. Google returns None when the gRPC package is missing).
        transcribe_fn: optional ``(audio, language, beam_size, best_of,
            initial_prompt) -> (segments, info)`` for the VAD-pipeline path.
        vad_preset: optional ``(vad_config) -> None`` to tweak VAD config when
            this engine uses the VAD-pipeline path.
        is_local: True for local engines registered by server.py
            (whisper-cpp, faster-whisper). Excluded from :func:`online_keys`.
        load_model: optional ``(model_path, body) -> (ok, detail)`` that loads
            the local model / starts the local inference server. Used by
            /start and /load-model for engines with ``requires_model=True``.
        is_ready: optional ``() -> (ok, detail)`` readiness probe for one-shot
            endpoints (/transcribe, /benchmark) on local engines.
    """
    _engines[key] = _Engine(key, requires_model, create_streaming, transcribe_fn,
                            vad_preset, is_local, load_model, is_ready)


def is_registered(key) -> bool:
    return key in _engines


def online_keys():
    """Keys of all registered online engines (local engines excluded)."""
    return [k for k, e in _engines.items() if not e.is_local]


def registered_keys():
    """Keys of ALL registered engines, local and online (for --backend validation)."""
    return list(_engines.keys())


def requires_model(key) -> bool:
    """Whether the engine needs a local model loaded. Unknown engines are
    assumed to require a model."""
    e = _engines.get(key)
    return e.requires_model if e else True


def get_transcribe_fn(key):
    e = _engines.get(key)
    return e.transcribe_fn if e else None


def load_model(key, model_path, body):
    """Load the engine's local model (or start its inference server).
    Returns ``(ok, detail)``. Engines without a loader succeed trivially."""
    e = _engines.get(key)
    if not e or e.load_model is None:
        return True, ""
    return e.load_model(model_path, body)


def is_ready(key):
    """Readiness probe for one-shot transcription endpoints.
    Returns ``(ok, detail)``. Engines without a probe are considered ready."""
    e = _engines.get(key)
    if not e or e.is_ready is None:
        return True, ""
    return e.is_ready()


def apply_vad_preset(key, vad_config):
    e = _engines.get(key)
    if e and e.vad_preset:
        e.vad_preset(vad_config)


def create_streaming_pipeline(key, *, api_key, config, broadcast_fn, stats, options):
    """Return a streaming pipeline instance, or ``None`` if this engine should
    use the VAD-pipeline path instead (offline engine, or an online engine whose
    streaming dependency is unavailable)."""
    e = _engines.get(key)
    if not e or e.create_streaming is None:
        return None
    return e.create_streaming(api_key=api_key, config=config,
                              broadcast_fn=broadcast_fn, stats=stats, options=options)
