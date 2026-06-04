"""
Live Transcription Server for Every Tongue.
FastAPI server: sounddevice audio capture -> Silero VAD -> faster-whisper -> SSE events.
"""

import argparse
import asyncio
import json
import logging
import os
import signal
import sys
import threading
import time

import io
import re
import subprocess
import tempfile
import urllib.request
import wave

import numpy as np
import sounddevice as sd
from difflib import SequenceMatcher
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from sse_starlette.sse import EventSourceResponse

# Backend-specific imports (faster-whisper may not be installed for whisper-cpp users)
try:
    from faster_whisper import WhisperModel
    _has_faster_whisper = True
except ImportError:
    WhisperModel = None
    _has_faster_whisper = False

# Silero VAD — standalone package for speech detection (whisper-cpp path)
try:
    import torch
    from silero_vad import load_silero_vad, get_speech_timestamps as _silero_get_speech_timestamps
    _silero_vad_model = load_silero_vad()
    _has_silero_vad = True
except ImportError as _vad_err:
    _has_silero_vad = False
    _silero_vad_model = None
    import sys
    print(f"[LIVE] WARNING: Silero VAD import failed: {_vad_err}", file=sys.stderr)

# ---------------------------------------------------------------------------
# Logging — stderr only, captured by PythonSidecarHost → AppLogger
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
logging.getLogger("faster_whisper").setLevel(logging.WARNING)
logging.getLogger("ctranslate2").setLevel(logging.WARNING)
logging.getLogger("uvicorn").setLevel(logging.WARNING)
logging.getLogger("uvicorn.access").setLevel(logging.WARNING)


app = FastAPI()

# ---------------------------------------------------------------------------
# Global state
# ---------------------------------------------------------------------------
model = None  # WhisperModel (faster-whisper) or None
model_path_global: str = ""
compute_type_global: str = "int8_float16"
device_global: str = "cuda"

capturing: bool = False
capture_thread: threading.Thread | None = None
stop_event: threading.Event = threading.Event()

# SSE subscribers (asyncio queues)
subscribers: list[asyncio.Queue] = []
subscribers_lock: threading.Lock = threading.Lock()

# Audio config
SAMPLE_RATE = 16000

# Capture config (set via /start)
current_config: dict = {}

# Current session stats (accessible via /stats endpoint)
current_stats: "SessionStats | None" = None

# Uvicorn server reference for graceful shutdown
_server = None
_shutting_down: bool = False

# Backend selection (set from CLI args)
_backend: str = "faster-whisper"
_whisper_server_path: str = ""
_vad_model_path: str = ""
_no_gpu: bool = False
_whisper_server_process: subprocess.Popen | None = None
_whisper_server_port: int = 0


# ---------------------------------------------------------------------------
# Normalized segment class (bridges faster-whisper and whisper-cpp formats)
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

    # Note: --vad flag causes whisper-server to hang on inference with some models.
    # VAD is handled Python-side using Silero VAD instead.

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
        total_bytes = 0
        try:
            for line in proc.stderr:
                total_bytes += len(line)
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


def _transcribe_whisper_cpp(audio_array: np.ndarray, language=None, beam_size=5):
    """Transcribe audio via whisper-server.exe /inference endpoint.
    Returns (segments, info) matching faster-whisper's interface.
    Returns (None, None) on error (timeout, connection refused, etc.)."""
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
    files = {"file": ("audio.wav", wav_data, "audio/wav")}

    try:
        result = _post_multipart(
            f"http://127.0.0.1:{_whisper_server_port}/inference",
            fields, files, timeout=120,
        )
    except Exception as e:
        elapsed = time.time() - t0
        logger.error(f"whisper-server /inference failed after {elapsed:.1f}s: {e}")
        return None, None  # None = error (distinct from [] = VAD found no speech)

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
    detected_lang = result.get("language", "")

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
# Helpers
# ---------------------------------------------------------------------------
def _strip_boundary_overlap(new_text: str, prev_text: str, max_overlap_words: int = 4) -> str:
    """Remove overlapping words from the start of new_text that match the end of prev_text."""
    if not prev_text or not new_text:
        return new_text
    prev_words = prev_text.lower().split()
    new_words = new_text.split()
    if not prev_words or not new_words:
        return new_text
    # Check if 1-4 words at the start of new_text match the end of prev_text
    for n in range(min(max_overlap_words, len(prev_words), len(new_words)), 0, -1):
        prev_tail = [re.sub(r"[^\w]", "", w) for w in prev_words[-n:]]
        new_head = [re.sub(r"[^\w]", "", w.lower()) for w in new_words[:n]]
        if prev_tail == new_head:
            # For single-word overlap, only strip if the word is 4+ chars
            # (avoids false matches on common short words like "i", "de", "el", "a")
            if n == 1 and len(prev_tail[0]) < 4:
                continue
            remaining = new_words[n:]
            if not remaining:
                continue
            stripped = " ".join(remaining)
            logger.debug(f"  BOUNDARY-DEDUP: stripped {n} overlapping words: {' '.join(new_words[:n])}")
            return stripped
    return new_text


def _find_sentence_boundary_word(words, min_time: float, audio_end: float) -> tuple:
    """Find the last word that ends a sentence (after min_time seconds).
    Only returns a boundary if it's within 2s of the audio end — meaning
    the speaker has actually paused/stopped after the sentence, not just
    a period mid-speech.
    Returns (text_up_to_boundary, audio_end_time) or (None, None)."""
    if not words:
        return None, None

    last_boundary_idx = -1
    for i, w in enumerate(words):
        word_stripped = w.word.rstrip()
        # Must end with sentence punctuation but NOT ellipsis (...)
        if w.end >= min_time and word_stripped[-1:] in ".!?;" and not word_stripped.endswith("..."):
            # Verify next word starts uppercase (true sentence boundary)
            if i + 1 < len(words):
                next_word = words[i + 1].word.lstrip()
                if next_word and next_word[0].isupper():
                    last_boundary_idx = i
            else:
                # Last word in the list — accept as boundary
                last_boundary_idx = i

    if last_boundary_idx < 0:
        return None, None

    boundary_time = words[last_boundary_idx].end
    # Only commit if the boundary is near the end of current audio
    # (within 2s means no significant speech follows the period)
    if audio_end - boundary_time > 2.0:
        return None, None

    text = "".join(w.word for w in words[:last_boundary_idx + 1]).strip()
    # Minimum length to avoid committing garbage fragments like "20 for every one."
    if len(text) < 30:
        return None, None
    return text, boundary_time


def _find_sentence_boundary_segment(segments, min_time: float, audio_end: float) -> tuple:
    """Fallback sentence boundary finder for whisper-cpp (no word timestamps).
    Finds sentence-ending punctuation within the combined text and estimates
    the audio cut position proportionally from segment timing.
    Returns (text_up_to_boundary, audio_end_time) or (None, None)."""
    if not segments:
        return None, None

    full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
    if not full_text:
        return None, None

    # Find the last sentence boundary: ". ", "! ", "? " followed by uppercase
    last_boundary_pos = -1
    for match in re.finditer(r'[.!?;]\s+(?=[A-Z])', full_text):
        last_boundary_pos = match.start() + 1  # include the punctuation char

    # Also check if text ends with sentence punctuation
    stripped = full_text.rstrip()
    if stripped and stripped[-1] in ".!?;" and not stripped.endswith("..."):
        last_boundary_pos = len(stripped)

    if last_boundary_pos <= 0:
        return None, None

    commit_text = full_text[:last_boundary_pos].strip()
    if not commit_text:
        return None, None

    # Estimate audio time proportionally from text position
    text_ratio = last_boundary_pos / max(len(full_text), 1)
    real_end = min(segments[-1].end, audio_end)
    real_start = segments[0].start
    boundary_time = real_start + (real_end - real_start) * text_ratio

    # Ensure we cut at least 0.5s into the audio
    if boundary_time < 0.5:
        boundary_time = 0.5

    return commit_text, boundary_time


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
    # Single word, short duration, moderate no-speech — catches "Gràcies"/"Gracias" filler
    # that leaks through the above thresholds (no_speech 0.45-0.7 range)
    word_count = sum(1 for seg in segments for w in seg.text.split() if w.strip())
    if word_count <= 1 and total_speech_dur < 2.0 and avg_no_speech > 0.45:
        return True
    # Low confidence on very short audio (likely hallucinated filler)
    if avg_logprob < -0.8 and total_speech_dur < 1.0:
        return True

    full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())

    # Language-mismatch detection: if recent commits are in one script and this
    # segment suddenly switches to a different script, it's very likely a hallucination
    # (e.g. transcribing Catalan and whisper hallucinates Russian "Субтитры сделал...")
    if detected_lang and recent_langs and len(recent_langs) >= 3:
        dominant_lang = max(set(recent_langs), key=recent_langs.count)
        if detected_lang != dominant_lang and _is_script_mismatch(detected_lang, dominant_lang):
            # Different script — require very high confidence to allow through
            if avg_no_speech > 0.3 or avg_logprob < -0.2:
                logger.debug(f"  LANG MISMATCH: '{detected_lang}' vs dominant '{dominant_lang}' "
                             f"(no_speech={avg_no_speech:.2f}, logprob={avg_logprob:.2f})")
                return True

    # Known hallucination phrases (e.g. "Thanks for watching")
    if _is_known_hallucination(full_text):
        return True

    # Self-repetition: only catch near-exact looping (>90% similar halves).
    # Parallel structures ("not X, but Y") typically score 60-80% — must not filter those.
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
        # Normalize: lowercase, strip ALL punctuation and leading numbers, compare words only
        norm_new = " ".join(re.sub(r"[^\w\s]", "", full_text.lower()).split())
        norm_prev = " ".join(re.sub(r"[^\w\s]", "", last_commit_text.lower()).split())
        # Strip leading numbers from both
        norm_new = re.sub(r"^\d+\s*", "", norm_new)
        norm_prev = re.sub(r"^\d+\s*", "", norm_prev)
        # Short texts: exact match only (avoids false positives on common short phrases)
        if len(norm_prev) <= 20:
            if norm_new == norm_prev:
                logger.debug(f"  REPETITION detected (exact short match)")
                return True
        else:
            # Use sequence matching for contiguous similarity
            ratio = SequenceMatcher(None, norm_new, norm_prev).ratio()
            # Only flag very high similarity (>85%) — obvious exact repeats
            if ratio > 0.85:
                logger.debug(f"  REPETITION detected (similarity={ratio:.0%}, {total_speech_dur:.1f}s)")
                return True

    return False


# ---------------------------------------------------------------------------
# Known hallucination phrase blocklist
# ---------------------------------------------------------------------------
_hallucination_phrases: list[str] = []


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
        # Exact containment (phrase in text or text in phrase)
        phrase_clean = re.sub(r"[^\w\s]", "", phrase).strip()
        if not phrase_clean:
            continue
        # Check similarity — high threshold for short texts
        ratio = SequenceMatcher(None, cleaned, phrase_clean).ratio()
        if ratio > 0.8:
            logger.debug(f"  KNOWN HALLUCINATION ({ratio:.0%} match): '{text}' ~ '{phrase}'")
            return True
    return False


# ---------------------------------------------------------------------------
# Session statistics — captures speaker cadence for profile tuning
# ---------------------------------------------------------------------------
class SessionStats:
    def __init__(self):
        self.start_time = time.time()
        self.commits = []          # list of {type, duration, chars, words, lang, time}
        self.hallucinations = 0
        self.silence_gaps = []     # seconds between end of one commit and start of next
        self._last_commit_time = None

    def record_commit(self, commit_type: str, duration: float, text: str, lang: str):
        now = time.time()
        words = len(text.split())
        entry = {
            "type": commit_type,    # "vad", "sentence", "force"
            "duration": duration,
            "chars": len(text),
            "words": words,
            "wps": words / duration if duration > 0.5 else 0,
            "lang": lang,
            "time": now,
        }
        self.commits.append(entry)
        if self._last_commit_time is not None:
            gap = now - self._last_commit_time
            if gap < 30:  # ignore long pauses (breaks, not speech gaps)
                self.silence_gaps.append(gap)
        self._last_commit_time = now

    def record_hallucination(self):
        self.hallucinations += 1

    def to_dict(self) -> dict:
        """Return stats as a JSON-serializable dict for the /stats endpoint."""
        if not self.commits:
            return {"elapsed": time.time() - self.start_time, "commits": 0}

        n = len(self.commits)
        durations = [c["duration"] for c in self.commits]
        word_counts = [c["words"] for c in self.commits]
        wps_vals = [c["wps"] for c in self.commits if c["wps"] > 0]
        char_counts = [c["chars"] for c in self.commits]
        types = {}
        for c in self.commits:
            types[c["type"]] = types.get(c["type"], 0) + 1
        langs = {}
        for c in self.commits:
            lang = c["lang"] or "unknown"
            langs[lang] = langs.get(lang, 0) + 1

        result = {
            "elapsed": time.time() - self.start_time,
            "commits": n,
            "hallucinations": self.hallucinations,
            "commit_types": types,
            "languages": langs,
            "duration": {
                "min": round(min(durations), 1),
                "max": round(max(durations), 1),
                "avg": round(sum(durations) / n, 1),
                "median": round(sorted(durations)[n // 2], 1),
            },
            "words": {
                "min": min(word_counts),
                "max": max(word_counts),
                "avg": round(sum(word_counts) / n),
                "median": sorted(word_counts)[n // 2],
            },
            "chars": {
                "min": min(char_counts),
                "max": max(char_counts),
                "avg": round(sum(char_counts) / n),
            },
        }
        if wps_vals:
            result["wps"] = {
                "min": round(min(wps_vals), 1),
                "max": round(max(wps_vals), 1),
                "avg": round(sum(wps_vals) / len(wps_vals), 1),
            }
        if self.silence_gaps:
            gaps = self.silence_gaps
            gn = len(gaps)
            result["silence_gaps"] = {
                "min": round(min(gaps), 1),
                "max": round(max(gaps), 1),
                "avg": round(sum(gaps) / gn, 1),
                "median": round(sorted(gaps)[gn // 2], 1),
            }
        # Short segment ratio (< 3s commits that might be fragmented)
        short_count = sum(1 for d in durations if d < 3)
        result["short_segment_ratio"] = round(short_count / n, 2)
        # Force-commit ratio (indicates max segment is too low)
        result["force_commit_ratio"] = round(types.get("force", 0) / n, 2)

        return result

    def summary(self) -> str:
        if not self.commits:
            return "SESSION STATS: no commits recorded"

        elapsed = time.time() - self.start_time
        n = len(self.commits)
        durations = [c["duration"] for c in self.commits]
        word_counts = [c["words"] for c in self.commits]
        wps_vals = [c["wps"] for c in self.commits if c["wps"] > 0]
        char_counts = [c["chars"] for c in self.commits]

        # Commit type breakdown
        types = {}
        for c in self.commits:
            types[c["type"]] = types.get(c["type"], 0) + 1

        # Language breakdown
        langs = {}
        for c in self.commits:
            lang = c["lang"] or "unknown"
            langs[lang] = langs.get(lang, 0) + 1

        # Build summary
        lines = [
            "=" * 60,
            f"SESSION STATS  ({elapsed/60:.0f}m {elapsed%60:.0f}s total)",
            f"  Commits: {n}  |  Hallucinations skipped: {self.hallucinations}",
            f"  Commit types: {', '.join(f'{k}={v}' for k, v in sorted(types.items()))}",
            f"  Languages detected: {', '.join(f'{k}={v}' for k, v in sorted(langs.items()))}",
            "",
            "  Segment duration (audio seconds per commit):",
            f"    min={min(durations):.1f}s  max={max(durations):.1f}s  avg={sum(durations)/n:.1f}s  median={sorted(durations)[n//2]:.1f}s",
            "",
            "  Words per commit:",
            f"    min={min(word_counts)}  max={max(word_counts)}  avg={sum(word_counts)/n:.0f}  median={sorted(word_counts)[n//2]}",
            "",
            "  Characters per commit:",
            f"    min={min(char_counts)}  max={max(char_counts)}  avg={sum(char_counts)/n:.0f}",
        ]

        if wps_vals:
            lines += [
                "",
                "  Speaking rate (words/sec):",
                f"    min={min(wps_vals):.1f}  max={max(wps_vals):.1f}  avg={sum(wps_vals)/len(wps_vals):.1f}",
            ]

        if self.silence_gaps:
            gaps = self.silence_gaps
            gn = len(gaps)
            lines += [
                "",
                "  Silence gaps between commits:",
                f"    min={min(gaps):.1f}s  max={max(gaps):.1f}s  avg={sum(gaps)/gn:.1f}s  median={sorted(gaps)[gn//2]:.1f}s",
            ]

        # Distribution buckets for segment duration
        buckets = {"<2s": 0, "2-5s": 0, "5-10s": 0, "10-20s": 0, ">20s": 0}
        for d in durations:
            if d < 2: buckets["<2s"] += 1
            elif d < 5: buckets["2-5s"] += 1
            elif d < 10: buckets["5-10s"] += 1
            elif d < 20: buckets["10-20s"] += 1
            else: buckets[">20s"] += 1
        lines += [
            "",
            "  Duration distribution:",
            f"    {', '.join(f'{k}={v}' for k, v in buckets.items())}",
            "=" * 60,
        ]

        return "\n".join(lines)


# ---------------------------------------------------------------------------
# Energy-based silence detection for whisper-cpp audio cutting
# ---------------------------------------------------------------------------
def _find_silence_cut_point(audio, sample_rate, search_after_s=3.0):
    """Find the best quiet point in audio after search_after_s seconds.
    Scans 50ms windows for the lowest energy point.
    Returns sample position or None if no quiet point found."""
    search_start = int(search_after_s * sample_rate)
    if search_start >= len(audio) - int(0.1 * sample_rate):
        return None

    chunk_size = int(0.05 * sample_rate)  # 50ms windows
    best_pos = None
    best_energy = float('inf')

    for pos in range(search_start, len(audio) - chunk_size, chunk_size):
        chunk = audio[pos:pos + chunk_size]
        energy = float(np.mean(chunk ** 2))
        if energy < best_energy:
            best_energy = energy
            best_pos = pos

    # Only return if the quiet point is actually quiet
    if best_pos is not None and best_energy < 0.005:
        return best_pos
    return None


def _detect_trailing_silence(audio, sample_rate, silence_threshold=None):
    """Detect how many seconds of silence are at the end of the audio.
    Uses RMS energy in 100ms windows scanning backwards.
    If no threshold given, auto-detect from the audio's peak RMS."""
    chunk_size = int(0.1 * sample_rate)  # 100ms windows

    if silence_threshold is None:
        # Auto-threshold: compute peak RMS across the whole audio, then
        # treat anything below 10% of peak as silence.
        # This adapts to any microphone gain level.
        n_chunks = len(audio) // chunk_size
        if n_chunks == 0:
            return len(audio) / sample_rate
        peak_rms = 0.0
        for i in range(n_chunks):
            chunk = audio[i * chunk_size:(i + 1) * chunk_size]
            rms = float(np.sqrt(np.mean(chunk ** 2)))
            if rms > peak_rms:
                peak_rms = rms
        silence_threshold = max(peak_rms * 0.10, 1e-6)
        logger.debug(f"SILENCE: peak_rms={peak_rms:.6f} threshold={silence_threshold:.6f}")

    silence_samples = 0
    pos = len(audio) - chunk_size
    while pos >= 0:
        chunk = audio[pos:pos + chunk_size]
        rms = float(np.sqrt(np.mean(chunk ** 2)))
        if rms > silence_threshold:
            break
        silence_samples += chunk_size
        pos -= chunk_size

    return silence_samples / sample_rate


# ---------------------------------------------------------------------------
# Audio capture + transcription thread
# ---------------------------------------------------------------------------
def capture_and_transcribe():
    """Main capture loop running in a background thread."""
    global capturing, model, current_stats

    cfg = current_config
    device_index = cfg.get("device_index", None)
    translate = cfg.get("translate", False)
    initial_prompt = cfg.get("initial_prompt", "")
    beam_size = cfg.get("beam_size", 5)

    # Audio buffer
    audio_buffer = []
    buffer_lock = threading.Lock()

    def audio_callback(indata, frames, time_info, status):
        if status:
            logger.warning(f"Audio status: {status}")
        with buffer_lock:
            audio_buffer.append(indata[:, 0].copy())

    task = "translate" if translate else "transcribe"

    try:
        stream = sd.InputStream(
            samplerate=SAMPLE_RATE,
            channels=1,
            dtype="float32",
            device=device_index,
            callback=audio_callback,
            blocksize=int(SAMPLE_RATE * 0.1),  # 100ms blocks
        )
        stream.start()
        init_lang = cfg.get("language", "auto")
        logger.debug(f"CAPTURE START device={device_index} lang={init_lang} task={task} beam={beam_size} vad_silence={cfg.get('vad_min_silence_ms', 300)}ms max_seg={cfg.get('vad_max_segment_s', 30)}s interim={cfg.get('interim_interval_ms', 1000)}ms")
    except Exception as e:
        logger.error(f"Failed to open audio stream: {e}")
        capturing = False
        broadcast_event("error", f"Audio device error: {e}")
        return

    if _backend == "whisper-cpp":
        _capture_loop_whisper_cpp(stream, audio_buffer, buffer_lock, cfg)
    else:
        _capture_loop_faster_whisper(stream, audio_buffer, buffer_lock, cfg)


def _capture_loop_whisper_cpp(stream, audio_buffer, buffer_lock, cfg):
    """Capture loop for whisper-cpp backend.

    Silero VAD → Whisper pipeline:
    - Accumulate audio in a growing buffer
    - Every 500ms, run Silero VAD on the whole buffer
    - If speech detected and ends before buffer end → transcribe speech region → commit
    - If speech runs to end of buffer → still talking, send interim update
    - Force-commit at max segment duration
    """
    global capturing, current_stats

    beam_size = cfg.get("beam_size", 5)
    last_commit_text = ""
    consec_hallucinations = 0
    recent_langs = []
    stats = SessionStats()
    current_stats = stats
    last_interim_time = 0.0

    # Silero VAD setup
    logger.debug(f"CAPTURE-LOOP: _has_silero_vad={_has_silero_vad}")
    if not _has_silero_vad:
        logger.error("Silero VAD not available (faster-whisper not installed). Falling back to no-VAD loop.")
        _capture_loop_whisper_cpp_no_vad(stream, audio_buffer, buffer_lock, cfg)
        return

    logger.debug("CAPTURE-LOOP: Silero VAD model loaded, entering main loop")

    CHECK_INTERVAL_S = 0.5
    INTERIM_INTERVAL_S = 5.0  # less frequent interims to conserve whisper-server requests
    SILENCE_COMMIT_S = 0.4    # commit after 0.4s silence (Silero already pads)
    loop_count = 0

    def _do_commit(speech_audio, commit_type):
        """Transcribe speech audio and commit. Returns True if committed."""
        nonlocal last_commit_text, consec_hallucinations
        speech_dur = len(speech_audio) / SAMPLE_RATE

        logger.debug(f"TRANSCRIBE for {commit_type}: {speech_dur:.1f}s audio")
        try:
            segments, info = _transcribe_whisper_cpp(speech_audio, language=language, beam_size=beam_size)
        except Exception as e:
            logger.error(f"Transcription error: {e}")
            return False

        if segments is None:
            logger.debug("Inference error during commit")
            return False

        detected_lang = info.language if info else ""
        full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
        if not full_text:
            logger.debug(f"{commit_type}: speech region was empty after transcription")
            return False

        is_hal = _is_hallucination(segments, last_commit_text, detected_lang, recent_langs)
        if is_hal:
            consec_hallucinations += 1
            logger.debug(f">>> HALLUCINATION #{consec_hallucinations}: {full_text}")
            stats.record_hallucination()
            return False

        full_text = _strip_boundary_overlap(full_text, last_commit_text)
        broadcast_event("commit", full_text, lang=detected_lang)
        logger.debug(f">>> {commit_type} [{detected_lang}] ({speech_dur:.1f}s): {full_text}")
        last_commit_text = full_text[-200:]
        stats.record_commit(commit_type.lower(), speech_dur, full_text, detected_lang)
        if detected_lang:
            recent_langs.append(detected_lang)
            if len(recent_langs) > 10:
                recent_langs.pop(0)
        consec_hallucinations = 0
        return True

    def _cut_buffer_after(sample_pos):
        """Cut buffer, keeping audio after sample_pos."""
        with buffer_lock:
            full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
            audio_buffer.clear()
            remaining = full[sample_pos:]
            if len(remaining) > 0:
                audio_buffer.append(remaining)

    def _trim_buffer(keep_s=1.0):
        """Trim buffer to keep only last keep_s seconds."""
        with buffer_lock:
            full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
            keep = int(keep_s * SAMPLE_RATE)
            audio_buffer.clear()
            audio_buffer.append(full[-keep:] if len(full) > keep else full)

    try:
        while not stop_event.is_set():
            vad_max_segment_s = current_config.get("vad_max_segment_s", 30)
            language = current_config.get("language", None)
            if language == "auto":
                language = None

            time.sleep(CHECK_INTERVAL_S)

            # Snapshot current audio (don't clear — growing buffer)
            with buffer_lock:
                if not audio_buffer:
                    continue
                current_audio = np.concatenate(audio_buffer)

            audio_duration = len(current_audio) / SAMPLE_RATE
            if audio_duration < 0.5:
                continue

            loop_count += 1
            if loop_count <= 3 or loop_count % 20 == 0:
                logger.debug(f"LOOP #{loop_count}: buf={audio_duration:.1f}s")

            # Run Silero VAD on the full buffer
            try:
                audio_tensor = torch.from_numpy(current_audio)
                speech_timestamps = _silero_get_speech_timestamps(
                    audio_tensor,
                    _silero_vad_model,
                    sampling_rate=SAMPLE_RATE,
                    threshold=0.4,
                    min_speech_duration_ms=250,
                    min_silence_duration_ms=500,
                    speech_pad_ms=300,
                )
            except Exception as e:
                logger.error(f"VAD error: {e}")
                continue

            if not speech_timestamps:
                if audio_duration > 5.0:
                    logger.debug(f"VAD: no speech in {audio_duration:.1f}s, trimming")
                    _trim_buffer(1.0)
                continue

            # Speech regions found
            n_regions = len(speech_timestamps)
            last_speech_end = speech_timestamps[-1]["end"]
            first_speech_start = speech_timestamps[0]["start"]
            silence_after = (len(current_audio) - last_speech_end) / SAMPLE_RATE

            logger.debug(
                f"VAD: {n_regions} region(s), "
                f"speech={first_speech_start / SAMPLE_RATE:.1f}-{last_speech_end / SAMPLE_RATE:.1f}s, "
                f"silence_after={silence_after:.2f}s, buf={audio_duration:.1f}s"
            )

            # Multiple regions = gaps between them = sentence breaks.
            # Commit everything up to the last gap, keep only the last region.
            if n_regions >= 2:
                # Commit all regions except the last one
                commit_end = speech_timestamps[-2]["end"]  # end of second-to-last region
                speech_audio = current_audio[first_speech_start:commit_end]
                commit_dur = len(speech_audio) / SAMPLE_RATE
                logger.debug(f"VAD: {n_regions - 1} completed region(s), committing {commit_dur:.1f}s")
                _do_commit(speech_audio, "COMMIT")
                # Cut buffer — keep from start of last region onward
                last_region_start = speech_timestamps[-1]["start"]
                _cut_buffer_after(last_region_start)
                last_interim_time = 0.0

            elif silence_after >= SILENCE_COMMIT_S:
                # Single region, speaker stopped — commit it
                speech_audio = current_audio[first_speech_start:last_speech_end]
                _do_commit(speech_audio, "COMMIT")
                _cut_buffer_after(last_speech_end)
                last_interim_time = 0.0

            else:
                # Single region, speech ongoing — periodic interim update
                speech_duration = (last_speech_end - first_speech_start) / SAMPLE_RATE
                now = time.time()

                # Force-commit if single region exceeds max duration
                if speech_duration >= vad_max_segment_s:
                    speech_audio = current_audio[first_speech_start:last_speech_end]
                    _do_commit(speech_audio, "FORCE-COMMIT")
                    _trim_buffer(1.0)
                    last_interim_time = 0.0

                elif speech_duration >= 2.0 and (now - last_interim_time) >= INTERIM_INTERVAL_S:
                    speech_audio = current_audio[first_speech_start:last_speech_end]
                    try:
                        segments, info = _transcribe_whisper_cpp(speech_audio, language=language, beam_size=beam_size)
                    except Exception:
                        continue
                    if segments is not None:
                        detected_lang = info.language if info else ""
                        # Find the last segment that ends a sentence (. ? !)
                        last_sentence_idx = -1
                        for i, seg in enumerate(segments):
                            txt = seg.text.strip()
                            if txt and txt[-1] in ".?!":
                                last_sentence_idx = i

                        if last_sentence_idx >= 0:
                            # We have completed sentences — commit them and trim buffer.
                            commit_segs = segments[:last_sentence_idx + 1]
                            commit_text = " ".join(seg.text.strip() for seg in commit_segs if seg.text.strip())
                            remain_segs = segments[last_sentence_idx + 1:]
                            remain_text = " ".join(seg.text.strip() for seg in remain_segs if seg.text.strip())

                            if commit_text and not _is_hallucination(commit_segs, last_commit_text, detected_lang, recent_langs):
                                commit_text = _strip_boundary_overlap(commit_text, last_commit_text)
                                broadcast_event("commit", commit_text, lang=detected_lang)
                                commit_dur = segments[last_sentence_idx].end
                                logger.debug(f">>> COMMIT (sentence) [{detected_lang}] ({commit_dur:.1f}s): {commit_text}")
                                last_commit_text = commit_text[-200:]
                                stats.record_commit("sentence", commit_dur, commit_text, detected_lang)
                                if detected_lang:
                                    recent_langs.append(detected_lang)
                                    if len(recent_langs) > 10:
                                        recent_langs.pop(0)
                                consec_hallucinations = 0

                                # Trim buffer: cut everything before the remaining speech
                                # Segment timestamps are relative to speech_audio, which starts at first_speech_start
                                cut_time = segments[last_sentence_idx].end
                                cut_sample = first_speech_start + int(cut_time * SAMPLE_RATE)
                                _cut_buffer_after(cut_sample)

                            if remain_text:
                                broadcast_event("update", remain_text)
                                logger.debug(f">>> UPDATE (remainder): {remain_text}")
                        else:
                            # No sentence-ending punctuation yet — broadcast as interim update
                            full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
                            if full_text and not _is_hallucination(segments, last_commit_text, detected_lang, recent_langs):
                                broadcast_event("update", full_text)
                                logger.debug(f">>> UPDATE ({speech_duration:.1f}s): {full_text}")
                    last_interim_time = now

    except Exception as e:
        import traceback
        logger.error(f"Capture loop CRASHED: {e}")
        logger.error(traceback.format_exc())
    finally:
        stream.stop()
        stream.close()
        capturing = False
        logger.debug("CAPTURE STOP")
        logger.debug("\n" + stats.summary())


def _capture_loop_whisper_cpp_no_vad(stream, audio_buffer, buffer_lock, cfg):
    """Fallback capture loop without Silero VAD — always transcribe, commit when whisper returns empty."""
    global capturing, current_stats

    beam_size = cfg.get("beam_size", 5)
    last_commit_text = ""
    recent_langs = []
    stats = SessionStats()
    current_stats = stats
    had_text = False
    pending_text = ""
    pending_lang = ""
    pending_duration = 0.0

    logger.debug("NO-VAD LOOP: starting (Silero VAD unavailable)")

    try:
        while not stop_event.is_set():
            vad_max_segment_s = current_config.get("vad_max_segment_s", 30)
            interim_interval_ms = current_config.get("interim_interval_ms", 1500)
            language = current_config.get("language", None)
            if language == "auto":
                language = None

            time.sleep(interim_interval_ms / 1000.0)

            with buffer_lock:
                if not audio_buffer:
                    continue
                current_audio = np.concatenate(audio_buffer)

            audio_duration = len(current_audio) / SAMPLE_RATE
            if audio_duration < 1.0:
                continue

            logger.debug(f"NO-VAD: transcribing {audio_duration:.1f}s")

            try:
                segments, info = _transcribe_whisper_cpp(current_audio, language=language, beam_size=beam_size)
            except Exception as e:
                logger.error(f"Transcription error: {e}")
                continue

            if segments is None:
                logger.debug(f"NO-VAD: inference error, retrying")
                continue

            detected_lang = info.language if info else ""
            full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())

            if full_text:
                if not _is_hallucination(segments, last_commit_text, detected_lang, recent_langs):
                    had_text = True
                    pending_text = full_text
                    pending_lang = detected_lang
                    pending_duration = audio_duration

                    if audio_duration >= vad_max_segment_s:
                        pending_text = _strip_boundary_overlap(pending_text, last_commit_text)
                        broadcast_event("commit", pending_text, lang=pending_lang)
                        logger.debug(f">>> FORCE-COMMIT [{pending_lang}] ({audio_duration:.1f}s): {pending_text}")
                        last_commit_text = pending_text[-200:]
                        stats.record_commit("force", audio_duration, pending_text, pending_lang)
                        with buffer_lock:
                            full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
                            keep = int(0.5 * SAMPLE_RATE)
                            audio_buffer.clear()
                            audio_buffer.append(full[-keep:] if len(full) > keep else full)
                        had_text = False
                        pending_text = ""
                    else:
                        broadcast_event("update", full_text)
                        logger.debug(f">>> UPDATE ({audio_duration:.1f}s): {full_text}")
            else:
                if had_text and pending_text:
                    pending_text = _strip_boundary_overlap(pending_text, last_commit_text)
                    broadcast_event("commit", pending_text, lang=pending_lang)
                    logger.debug(f">>> COMMIT [{pending_lang}] ({pending_duration:.1f}s): {pending_text}")
                    last_commit_text = pending_text[-200:]
                    stats.record_commit("silence", pending_duration, pending_text, pending_lang)
                    with buffer_lock:
                        full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
                        keep = int(0.5 * SAMPLE_RATE)
                        audio_buffer.clear()
                        audio_buffer.append(full[-keep:] if len(full) > keep else full)
                    had_text = False
                    pending_text = ""
                elif audio_duration > 10.0:
                    logger.debug(f"NO-VAD: trimming stale buffer ({audio_duration:.1f}s)")
                    with buffer_lock:
                        full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
                        keep = int(1.0 * SAMPLE_RATE)
                        audio_buffer.clear()
                        audio_buffer.append(full[-keep:] if len(full) > keep else full)

    except Exception as e:
        logger.error(f"No-VAD loop error: {e}")
    finally:
        stream.stop()
        stream.close()
        capturing = False
        logger.debug("CAPTURE STOP (no-vad)")
        logger.debug("\n" + stats.summary())


def _capture_loop_faster_whisper(stream, audio_buffer, buffer_lock, cfg):
    """Capture loop for faster-whisper backend (original logic).
    Uses faster-whisper's built-in Silero VAD and word timestamps."""
    global capturing, model, current_stats

    device_index = cfg.get("device_index", None)
    translate = cfg.get("translate", False)
    initial_prompt = cfg.get("initial_prompt", "")
    beam_size = cfg.get("beam_size", 5)
    task = "translate" if translate else "transcribe"

    last_interim_time = time.time()
    last_committed_pos = 0  # samples already committed
    last_commit_text = ""  # track previous commit for repetition detection only
    consec_hallucinations = 0  # consecutive hallucination skips without real commit
    recent_langs = []  # rolling window of last N committed languages for mismatch detection
    stats = SessionStats()
    current_stats = stats

    try:
        while not stop_event.is_set():
            # Read live-adjustable config each iteration
            vad_min_silence_ms = current_config.get("vad_min_silence_ms", 300)
            vad_max_segment_s = current_config.get("vad_max_segment_s", 30)
            interim_interval_ms = current_config.get("interim_interval_ms", 1000)
            language = current_config.get("language", None)
            if language == "auto":
                language = None

            # Sleep for the interim interval before checking
            time.sleep(interim_interval_ms / 1000.0)

            # Get current audio
            with buffer_lock:
                if not audio_buffer:
                    continue
                current_audio = np.concatenate(audio_buffer)

            total_samples = len(current_audio)
            uncommitted_samples = total_samples - last_committed_pos
            uncommitted_duration = uncommitted_samples / SAMPLE_RATE

            # Only process if we have at least 1s of new audio
            if uncommitted_duration < 1.0:
                continue

            now = time.time()

            audio_to_process = current_audio[last_committed_pos:]

            try:
                segments_iter, info = model.transcribe(
                    audio_to_process,
                    language=language,
                    task=task,
                    beam_size=beam_size,
                    initial_prompt=initial_prompt or None,
                    vad_filter=True,
                    vad_parameters={
                        "threshold": 0.3,
                        "min_silence_duration_ms": vad_min_silence_ms,
                        "max_speech_duration_s": vad_max_segment_s,
                        "speech_pad_ms": 100,
                    },
                    word_timestamps=True,
                    no_repeat_ngram_size=3,
                    repetition_penalty=1.1,
                )
                segments = list(segments_iter)
            except Exception as e:
                logger.error(f"Transcription error: {e}")
                continue

            if not segments:
                logger.debug(f"VAD: no speech in {uncommitted_duration:.1f}s of audio — discarding")
                with buffer_lock:
                    current_audio_full = np.concatenate(audio_buffer)
                    keep_samples = int(0.5 * SAMPLE_RATE)
                    remaining = current_audio_full[-keep_samples:] if len(current_audio_full) > keep_samples else current_audio_full
                    audio_buffer.clear()
                    audio_buffer.append(remaining)
                    last_committed_pos = 0
                continue

            # Gather all words across segments
            all_words = []
            for seg in segments:
                if seg.words:
                    all_words.extend(seg.words)

            # Full text and timing
            audio_duration = len(audio_to_process) / SAMPLE_RATE
            last_seg_end = segments[-1].end if segments else 0
            silence_at_end = audio_duration - last_seg_end
            all_final = silence_at_end >= (vad_min_silence_ms / 1000.0)

            full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())

            logger.debug(f"TRANSCRIBE dur={audio_duration:.1f}s segs={len(segments)} last_end={last_seg_end:.1f}s silence_tail={silence_at_end:.2f}s final={all_final} words={len(all_words)}")
            for seg in segments:
                logger.debug(f"  [{seg.start:.1f}-{seg.end:.1f}] no_speech={seg.no_speech_prob:.2f} logprob={seg.avg_logprob:.2f} | {seg.text.strip()}")

            detected_lang = info.language if info else ""
            committed_this_loop = False

            if all_final:
                # VAD detected end of speech — commit everything and cut audio
                if full_text and not _is_hallucination(segments, last_commit_text, detected_lang, recent_langs):
                    full_text = _strip_boundary_overlap(full_text, last_commit_text)
                    broadcast_event("commit", full_text, lang=detected_lang)
                    logger.debug(f">>> COMMIT [{detected_lang}]: {full_text}")
                    last_commit_text = full_text[-200:]
                    committed_this_loop = True
                    consec_hallucinations = 0
                    stats.record_commit("vad", uncommitted_duration, full_text, detected_lang)
                    if detected_lang:
                        recent_langs.append(detected_lang)
                        if len(recent_langs) > 10:
                            recent_langs.pop(0)

                    # Cut audio buffer after successful commit (pad 0.12s past last word)
                    committed_end = last_committed_pos + int((last_seg_end + 0.12) * SAMPLE_RATE)
                    with buffer_lock:
                        current_audio_full = np.concatenate(audio_buffer)
                        remaining = current_audio_full[committed_end:]
                        audio_buffer.clear()
                        if len(remaining) > 0:
                            audio_buffer.append(remaining)
                        last_committed_pos = 0
                    last_interim_time = now
                elif full_text:
                    consec_hallucinations += 1
                    logger.debug(f">>> HALLUCINATION SKIPPED #{consec_hallucinations} (no_speech={sum(s.no_speech_prob for s in segments)/len(segments):.2f} logprob={sum(s.avg_logprob for s in segments)/len(segments):.2f}): {full_text}")
                    stats.record_hallucination()
                    # After 3 consecutive hallucinations at same position, cut buffer
                    # (stuck on a noise blip — no real speech to preserve)
                    if consec_hallucinations >= 3:
                        logger.debug(f">>> CUTTING BUFFER after {consec_hallucinations} consecutive hallucinations")
                        with buffer_lock:
                            current_audio_full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
                            committed_end = last_committed_pos + int(last_seg_end * SAMPLE_RATE)
                            remaining = current_audio_full[committed_end:]
                            audio_buffer.clear()
                            if len(remaining) > 0:
                                audio_buffer.append(remaining)
                            last_committed_pos = 0
                        consec_hallucinations = 0
                        last_interim_time = now

            elif uncommitted_duration >= 6.0:
                # Try to find a sentence boundary to commit at using word timestamps
                # Look for sentence-ending punctuation with audio time > 5s
                boundary_text, boundary_time = _find_sentence_boundary_word(all_words, 5.0, audio_duration)

                if boundary_text and boundary_time:
                    # Check for hallucination before committing
                    if _is_hallucination(segments, last_commit_text, detected_lang, recent_langs):
                        logger.debug(f">>> SENTENCE-COMMIT BLOCKED (hallucination): {boundary_text}")
                        # Discard the bad audio and reset
                        with buffer_lock:
                            current_audio_full = np.concatenate(audio_buffer)
                            cut_pos = last_committed_pos + int(boundary_time * SAMPLE_RATE)
                            remaining = current_audio_full[cut_pos:]
                            audio_buffer.clear()
                            if len(remaining) > 0:
                                audio_buffer.append(remaining)
                            last_committed_pos = 0
                        last_interim_time = now
                    else:
                        boundary_text = _strip_boundary_overlap(boundary_text, last_commit_text)
                        broadcast_event("commit", boundary_text, lang=detected_lang)
                        logger.debug(f">>> SENTENCE-COMMIT [{detected_lang}] @{boundary_time:.1f}s ({uncommitted_duration:.1f}s): {boundary_text}")
                        last_commit_text = boundary_text[-200:]
                        committed_this_loop = True
                        consec_hallucinations = 0
                        stats.record_commit("sentence", boundary_time, boundary_text, detected_lang)
                        if detected_lang:
                            recent_langs.append(detected_lang)
                            if len(recent_langs) > 10:
                                recent_langs.pop(0)

                        # Cut audio at sentence boundary + small pad to avoid word tail leaking
                        cut_pos = last_committed_pos + int((boundary_time + 0.12) * SAMPLE_RATE)
                        with buffer_lock:
                            current_audio_full = np.concatenate(audio_buffer)
                            remaining = current_audio_full[cut_pos:]
                            audio_buffer.clear()
                            if len(remaining) > 0:
                                audio_buffer.append(remaining)
                            last_committed_pos = 0
                    last_interim_time = now
                else:
                    # No sentence boundary — emit update
                    if full_text:
                        broadcast_event("update", full_text)
                        logger.debug(f">>> UPDATE: {full_text}")
                    last_interim_time = now

            else:
                # Short audio — just emit update (skip hallucinations)
                if full_text and not _is_hallucination(segments, last_commit_text, detected_lang, recent_langs):
                    broadcast_event("update", full_text)
                    logger.debug(f">>> UPDATE: {full_text}")
                elif full_text:
                    logger.debug(f">>> HALLUCINATION SKIPPED (no_speech={sum(s.no_speech_prob for s in segments)/len(segments):.2f} logprob={sum(s.avg_logprob for s in segments)/len(segments):.2f}): {full_text}")
                    stats.record_hallucination()
                last_interim_time = now

            # Force-commit if exceeded max segment duration (skip if already committed above)
            if uncommitted_duration >= vad_max_segment_s and not committed_this_loop:
                if full_text and not _is_hallucination(segments, last_commit_text, detected_lang, recent_langs):
                    full_text = _strip_boundary_overlap(full_text, last_commit_text)
                    broadcast_event("commit", full_text, lang=detected_lang)
                    logger.debug(f">>> FORCE-COMMIT [{detected_lang}] ({uncommitted_duration:.1f}s): {full_text}")
                    last_commit_text = full_text[-200:]
                    consec_hallucinations = 0
                    stats.record_commit("force", uncommitted_duration, full_text, detected_lang)
                    if detected_lang:
                        recent_langs.append(detected_lang)
                        if len(recent_langs) > 10:
                            recent_langs.pop(0)
                elif full_text:
                    logger.debug(f">>> FORCE-COMMIT SKIPPED (hallucination, cutting buffer): {full_text}")
                    stats.record_hallucination()

                # Always cut buffer at max segment to prevent unbounded growth
                consec_hallucinations = 0
                with buffer_lock:
                    current_audio_full = np.concatenate(audio_buffer) if audio_buffer else np.array([], dtype=np.float32)
                    committed_end = int(last_seg_end * SAMPLE_RATE) + last_committed_pos
                    remaining = current_audio_full[committed_end:]
                    audio_buffer.clear()
                    if len(remaining) > 0:
                        audio_buffer.append(remaining)
                    last_committed_pos = 0
                last_interim_time = now

    except Exception as e:
        logger.error(f"Capture loop error: {e}")
    finally:
        stream.stop()
        stream.close()
        capturing = False
        logger.debug("CAPTURE STOP")
        logger.debug("\n" + stats.summary())


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------
@app.get("/health")
async def health():
    return {
        "status": "ok",
        "model_loaded": model is not None,
        "capturing": capturing,
    }


@app.get("/devices")
async def get_devices():
    devices = sd.query_devices()
    # Filter to host API of the default input device (avoids duplicates from MME/DirectSound/WASAPI)
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
async def start_capture(request: Request):
    global capturing, capture_thread, model, current_config
    global model_path_global, compute_type_global, device_global

    if capturing:
        return JSONResponse({"status": "already_capturing"}, status_code=409)

    body = await request.json()
    current_config = body

    requested_model_path = body.get("model_path", model_path_global)
    requested_compute_type = body.get("compute_type", compute_type_global)
    requested_device = body.get("device", device_global)

    if _backend == "whisper-cpp":
        # Start whisper-server.exe (model loaded in memory, Vulkan/CPU)
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
    else:
        # faster-whisper: load model in-process
        if not _has_faster_whisper:
            return JSONResponse({"status": "error", "detail": "faster-whisper not installed"}, status_code=500)
        if model is None or requested_model_path != model_path_global:
            logger.debug(f"MODEL LOAD path={requested_model_path} device={requested_device} compute={requested_compute_type}")
            try:
                model = WhisperModel(
                    requested_model_path,
                    device=requested_device,
                    compute_type=requested_compute_type,
                )
                model_path_global = requested_model_path
                compute_type_global = requested_compute_type
                device_global = requested_device
                logger.debug("MODEL LOAD OK")
            except Exception as e:
                logger.error(f"Failed to load model: {e}")
                return JSONResponse({"status": "error", "detail": str(e)}, status_code=500)

    # Start capture
    stop_event.clear()
    capturing = True
    capture_thread = threading.Thread(target=capture_and_transcribe, daemon=True)
    capture_thread.start()

    return {"status": "started"}


@app.post("/stop")
async def stop_capture():
    global capturing
    if not capturing:
        return {"status": "not_capturing"}

    stop_event.set()
    if capture_thread is not None:
        capture_thread.join(timeout=5)
    capturing = False
    logger.debug("Capture stopped via /stop")
    return {"status": "stopped"}


@app.post("/config")
async def update_config(request: Request):
    """Update live-adjustable config values without restarting capture."""
    body = await request.json()
    updated = []
    for key in ("vad_min_silence_ms", "vad_max_segment_s", "interim_interval_ms", "language"):
        if key in body:
            current_config[key] = body[key]
            updated.append(key)
    logger.debug(f"CONFIG UPDATE: {', '.join(f'{k}={current_config[k]}' for k in updated)}")
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
    global model, model_path_global, compute_type_global, device_global

    body = await request.json()
    requested_model_path = body.get("model_path", model_path_global)
    requested_compute_type = body.get("compute_type", compute_type_global)
    requested_device = body.get("device", device_global)

    if _backend == "whisper-cpp":
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

    # faster-whisper: load model in-process
    if model is not None and requested_model_path == model_path_global:
        return {"status": "already_loaded"}

    logger.debug(f"MODEL LOAD path={requested_model_path} device={requested_device} compute={requested_compute_type}")
    try:
        model = WhisperModel(
            requested_model_path,
            device=requested_device,
            compute_type=requested_compute_type,
        )
        model_path_global = requested_model_path
        compute_type_global = requested_compute_type
        device_global = requested_device
        logger.debug("MODEL LOAD OK")
        return {"status": "loaded"}
    except Exception as e:
        logger.error(f"Failed to load model: {e}")
        return JSONResponse({"status": "error", "detail": str(e)}, status_code=500)


@app.post("/transcribe")
async def transcribe_audio(request: Request):
    """One-shot transcription of uploaded audio data.
    Accepts WAV, MP3, or raw 16kHz float32 PCM.
    Used by conversation rooms for push-to-talk and benchmarks."""

    if _backend == "faster-whisper" and model is None:
        return JSONResponse({"status": "error", "detail": "Model not loaded"}, status_code=503)
    if _backend == "whisper-cpp" and (_whisper_server_process is None or _whisper_server_process.poll() is not None):
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

    # If WAV failed and content looks like MP3/opus/compressed audio, decode via ffmpeg
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
                import os
                try: os.unlink(tmp_in_path)
                except: pass
                try: os.unlink(tmp_out_path)
                except: pass
        except Exception as e:
            logger.warning(f"ffmpeg decode failed: {e}")

    # Last resort: treat as raw 16kHz float32 PCM
    if audio is None:
        audio = np.frombuffer(body, dtype=np.float32)

    if len(audio) < SAMPLE_RATE * 0.3:
        return JSONResponse({"status": "error", "detail": "Audio too short"}, status_code=400)

    try:
        if _backend == "whisper-cpp":
            segments, info = _transcribe_whisper_cpp(audio, language=lang, beam_size=5)
        else:
            segments_iter, info = model.transcribe(
                audio,
                language=lang,
                task="transcribe",
                beam_size=5,
                vad_filter=True,
                vad_parameters={
                    "threshold": 0.3,
                    "min_silence_duration_ms": 300,
                },
                word_timestamps=False,
            )
            segments = list(segments_iter)
        text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
        detected = info.language if info else ""

        if not text:
            return {"status": "ok", "text": "", "lang": detected}

        # Basic hallucination check
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
    global capturing, _shutting_down
    logger.debug("Shutdown requested")
    _shutting_down = True

    # Stop capture if running
    if capturing:
        stop_event.set()
        capturing = False
        # Don't join capture_thread here — it can block the response

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
    parser.add_argument("--backend", type=str, default="faster-whisper",
                        choices=["faster-whisper", "whisper-cpp"],
                        help="Transcription backend (default: faster-whisper)")
    parser.add_argument("--whisper-server-path", type=str, default="",
                        help="Path to whisper-server.exe (required for whisper-cpp backend)")
    parser.add_argument("--whisper-server-port", type=int, default=8178,
                        help="Port for whisper-server.exe (default: 8178)")
    parser.add_argument("--no-gpu", action="store_true",
                        help="Disable GPU (CPU-only mode for whisper-cpp)")
    parser.add_argument("--vad-model-path", type=str, default="",
                        help="Path to Silero VAD GGML model for whisper-server built-in VAD")
    args = parser.parse_args()

    _backend = args.backend
    _whisper_server_path = args.whisper_server_path
    _whisper_server_port = args.whisper_server_port
    _no_gpu = args.no_gpu
    _vad_model_path = args.vad_model_path

    if _backend == "whisper-cpp" and not _has_faster_whisper:
        logger.debug("Running in whisper-cpp mode (faster-whisper not available)")
    elif _backend == "whisper-cpp":
        logger.debug("Running in whisper-cpp mode")
    else:
        if not _has_faster_whisper:
            logger.error("faster-whisper backend selected but package not installed!")
            sys.exit(1)
        logger.debug("Running in faster-whisper mode")

    _load_hallucination_phrases()

    import uvicorn

    config = uvicorn.Config(app, host=args.host, port=args.port, log_level="warning")
    _server = uvicorn.Server(config)
    _server.run()
