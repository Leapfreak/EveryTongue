"""Google Cloud Speech-to-Text engine (online) — fully self-contained.

Two paths, both isolated here so server.py carries no Google-specific code:

  * gRPC streaming (preferred): raw audio is streamed continuously to Google,
    which handles endpointing, sentence segmentation, and punctuation. Requires
    the ``google-cloud-speech`` package. Implemented by GoogleStreamingPipeline.

  * REST fallback: used only when ``google-cloud-speech`` is not installed. Runs
    through the live-server's VAD pipeline via a transcribe function, with a VAD
    preset that accumulates natural utterances before each recognize() call.

Registered at import time via engines.common.register_engine("google-cloud-stt").
"""
import base64
import http.client
import json
import logging
import queue
import ssl
import threading
import time

import numpy as np
import sounddevice as sd

from . import common
from .common import SAMPLE_RATE, SegmentInfo, TranscribeInfo, audio_to_wav_bytes

logger = logging.getLogger("live-server")

# Google streaming limit is 5 minutes. Restart stream before hitting it.
STREAM_TIMEOUT_S = 280  # ~4m40s, restart with margin

# Whisper ISO 639-1 → Google BCP-47 mapping (extend as needed)
def _to_bcp47(code):
    """whisper/ISO code -> Google STT BCP-47 locale, from the canonical table's
    googleStt column (language-codes.json — no static list here). A table value
    is used verbatim (Google's Mandarin tag is bare "zh"); unmapped two-letter
    codes get the xx → xx-XX heuristic, anything else passes through."""
    locale = common.vendor_locale(code, "googleStt")
    if locale:
        return locale
    code = code or ""
    if "-" not in code and len(code) == 2:
        return f"{code}-{code.upper()}"
    return code


_ENGINE_KEY = "google-cloud-stt"


def _api_key() -> str:
    return common.get_api_key(_ENGINE_KEY)


# ---------------------------------------------------------------------------
# REST fallback — persistent HTTPS connection + recognize()
# ---------------------------------------------------------------------------
_consecutive_errors = 0
_backoff_until = 0.0  # time.time() when backoff expires
_model_fallback_langs = set()  # langs where latest_* returned 400 → use "default"

_conn = None  # type: http.client.HTTPSConnection or None
_conn_lock = threading.Lock()


def _get_conn() -> http.client.HTTPSConnection:
    """Get or create a persistent HTTPS connection to speech.googleapis.com."""
    global _conn
    if _conn is not None:
        return _conn
    ctx = ssl.create_default_context()
    _conn = http.client.HTTPSConnection("speech.googleapis.com", timeout=30, context=ctx)
    return _conn


def _api_request(payload_bytes: bytes) -> dict:
    """Send a request to Google Cloud STT, reusing the persistent connection.
    Reconnects on broken pipe / reset; backs off after consecutive errors."""
    global _conn, _consecutive_errors, _backoff_until
    path = f"/v1/speech:recognize?key={_api_key()}"
    headers = {"Content-Type": "application/json", "Connection": "keep-alive"}

    if _backoff_until > 0 and time.time() < _backoff_until:
        return None

    with _conn_lock:
        for attempt in range(2):  # retry once on stale connection
            try:
                conn = _get_conn()
                conn.request("POST", path, body=payload_bytes, headers=headers)
                resp = conn.getresponse()
                body = resp.read()
                if resp.status == 200:
                    _consecutive_errors = 0
                    _backoff_until = 0.0
                    return json.loads(body)
                else:
                    error_text = body.decode("utf-8", errors="replace")[:500]
                    if resp.status == 400 and "not supported for language" in error_text:
                        return {"_model_not_supported": True, "_error": error_text}
                    logger.error(f"Google Cloud STT HTTP {resp.status}: {error_text}")
                    _consecutive_errors += 1
                    if resp.status in (400, 401, 403) and _consecutive_errors >= 3:
                        backoff_secs = min(30, 5 * _consecutive_errors)
                        _backoff_until = time.time() + backoff_secs
                        logger.warning(f"Google STT: {_consecutive_errors} consecutive errors, backing off {backoff_secs}s")
                    return None
            except (http.client.RemoteDisconnected, ConnectionResetError,
                    BrokenPipeError, OSError) as e:
                logger.debug(f"Google connection reset (attempt {attempt+1}): {e}")
                _conn = None  # force reconnect
                if attempt == 1:
                    _consecutive_errors += 1
                    logger.error(f"Google Cloud STT connection failed after retry: {e}")
                    return None
            except Exception as e:
                logger.error(f"Google Cloud STT request failed: {e}")
                _conn = None
                _consecutive_errors += 1
                return None
    return None


def transcribe(audio_array: np.ndarray, language=None,
               beam_size=5, best_of=1, initial_prompt=""):
    """Transcribe audio via Google Cloud STT v1 REST API.
    Returns (segments, info) or (None, None) on error. Registered as the
    transcribe_fn for the VAD-pipeline fallback path."""
    if not _api_key():
        logger.error("Google Cloud STT API key not configured")
        return None, None

    t0 = time.time()
    audio_duration = len(audio_array) / SAMPLE_RATE

    wav_data = audio_to_wav_bytes(audio_array)
    b64_audio = base64.b64encode(wav_data).decode("utf-8")

    lang_code = language or "en"
    if lang_code == "auto":
        lang_code = "en-US"
    else:
        lang_code = _to_bcp47(lang_code)

    # Always try the better latest_* models; no static support list. A language
    # they don't cover gets one 400 (_model_not_supported), the retry below
    # drops to "default" and _model_fallback_langs remembers it for the session.
    if lang_code in _model_fallback_langs:
        model = "default"
    else:
        model = "latest_short" if audio_duration < 15.0 else "latest_long"

    config = {
        "encoding": "LINEAR16",
        "sampleRateHertz": SAMPLE_RATE,
        "languageCode": lang_code,
        "enableAutomaticPunctuation": True,
        "model": model,
    }
    body = {"config": config, "audio": {"content": b64_audio}}

    payload_bytes = json.dumps(body).encode("utf-8")
    result = _api_request(payload_bytes)

    # Auto-fallback: if model not supported for this language, retry with "default"
    if isinstance(result, dict) and result.get("_model_not_supported"):
        logger.warning(f"Model '{model}' not supported for {lang_code}, falling back to 'default'")
        _model_fallback_langs.add(lang_code)
        body["config"]["model"] = "default"
        payload_bytes = json.dumps(body).encode("utf-8")
        result = _api_request(payload_bytes)

    if result is None:
        return None, None

    # Google's confidence on short clips is often low but the text is still valid.
    # Set no_speech_prob=0 so the hallucination filter doesn't reject it.
    segments = []
    for r in result.get("results", []):
        alts = r.get("alternatives", [])
        if not alts:
            continue
        transcript_text = alts[0].get("transcript", "").strip()
        if not transcript_text:
            continue
        segments.append(SegmentInfo(
            text=transcript_text, start=0.0, end=audio_duration,
            no_speech_prob=0.0, avg_logprob=0.0,
        ))

    detected_lang = ""
    if result.get("results"):
        detected_lang = result["results"][0].get("languageCode", lang_code)
    if detected_lang and "-" in detected_lang:
        detected_lang = detected_lang.split("-")[0]

    elapsed = time.time() - t0
    logger.debug(f"GOOGLE STT DONE: {elapsed:.1f}s elapsed, {len(segments)} segments, lang={detected_lang}, model={model}")
    return segments, TranscribeInfo(language=detected_lang)


def vad_preset(cfg):
    """Tighten VAD config for the REST fallback — accumulate natural utterances
    (1-8s) before each recognize() call rather than re-processing a growing
    buffer. Forces the plain VAD pipeline (not the accumulating wrapper)."""
    cfg.vad_silence_ms = 300
    cfg.soft_commit_ms = 300
    cfg.vad_max_segment_s = 25
    cfg.vad_max_soft_segment_s = 10
    cfg.enable_interim = False
    cfg.accumulate_audio = True
    cfg.accumulate_pause_s = 4.0
    cfg.accumulate_max_s = 30.0
    cfg.force_vad_pipeline = True


# ---------------------------------------------------------------------------
# gRPC streaming pipeline (preferred)
# ---------------------------------------------------------------------------
class GoogleStreamingPipeline:
    """Streams audio directly to Google Cloud STT via gRPC.

    No VAD segmentation — Google handles endpointing internally. is_final
    results are natural sentence boundaries with punctuation.
    """

    def __init__(self, api_key, config, broadcast_fn, stats):
        self._api_key = api_key
        self._config = config
        self._broadcast_fn = broadcast_fn
        self._stats = stats
        self._stop_event = threading.Event()
        self._audio_queue = queue.Queue(maxsize=1000)
        self._stream = None
        self._threads = []
        self._audio_callback_count = 0
        self._thread_errors = {}

    @property
    def stats(self):
        return self._stats

    def start(self):
        cfg = self._config
        logger.info(f"[GOOGLE-STREAM] Starting: device={cfg.device_index} lang={cfg.language}")

        chunk_samples = int(SAMPLE_RATE * 0.1)  # 1600 samples = 100ms
        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE, channels=1, dtype="int16",
            blocksize=chunk_samples, device=cfg.device_index,
            callback=self._audio_callback,
        )

        stream_thread = threading.Thread(
            target=self._streaming_thread, name="google-stream", daemon=True)
        self._threads = [stream_thread]
        stream_thread.start()

        self._stream.start()
        logger.info("[GOOGLE-STREAM] Audio capture started")

    def stop(self):
        logger.info("[GOOGLE-STREAM] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[GOOGLE-STREAM] Error closing stream: {e}")
            self._stream = None
        for t in self._threads:
            t.join(timeout=5.0)
        self._threads.clear()
        logger.info("[GOOGLE-STREAM] Stopped")

    def is_alive(self):
        if not self._threads:
            return False, "no threads"
        dead = [t.name for t in self._threads if not t.is_alive()]
        if dead:
            return False, f"dead threads: {dead}"
        return True, f"ok (callbacks={self._audio_callback_count})"

    def update_config(self, **kwargs):
        if "language" in kwargs:
            self._config.language = kwargs["language"]

    def _audio_callback(self, indata, frames, time_info, status):
        try:
            if status:
                logger.warning(f"[GOOGLE-STREAM] Audio status: {status}")
            audio_bytes = indata.tobytes()
            try:
                self._audio_queue.put_nowait(audio_bytes)
            except queue.Full:
                pass  # drop frame rather than block
            self._audio_callback_count += 1
        except Exception as e:
            if self._audio_callback_count < 5:
                logger.error(f"[GOOGLE-STREAM] Audio callback error: {e}")

    def _streaming_thread(self):
        thread_name = "google-stream"
        try:
            from google.cloud.speech_v1 import SpeechClient
            from google.cloud.speech_v1.types import cloud_speech
            from google.api_core.client_options import ClientOptions
            from google.protobuf import duration_pb2
        except ImportError as e:
            self._thread_errors[thread_name] = f"Missing package: {e}"
            logger.error(
                f"[GOOGLE-STREAM] google-cloud-speech not installed. "
                f"Run: pip install google-cloud-speech\n{e}")
            return

        try:
            client = SpeechClient(client_options=ClientOptions(api_key=self._api_key))
            logger.info("[GOOGLE-STREAM] SpeechClient created with API key")
        except Exception as e:
            self._thread_errors[thread_name] = f"Client creation failed: {e}"
            logger.error(f"[GOOGLE-STREAM] Failed to create SpeechClient: {e}")
            return

        lang = self._config.language or "en"
        lang_code = "en-US" if lang == "auto" else _to_bcp47(lang)

        utterance_id = 0
        use_voice_activity = True
        use_enhanced_features = True
        consecutive_errors = 0

        # Always try the better latest_long model; no static support list. If
        # Google rejects it for this language, the error handler below drops to
        # "default" and restarts the stream (one wasted connection, once).
        model = "latest_long"

        while not self._stop_event.is_set():
            try:
                logger.info(f"[GOOGLE-STREAM] Starting stream for lang={lang_code}")
                while not self._audio_queue.empty():
                    try:
                        self._audio_queue.get_nowait()
                    except queue.Empty:
                        break

                rc_kwargs = dict(
                    encoding=cloud_speech.RecognitionConfig.AudioEncoding.LINEAR16,
                    sample_rate_hertz=SAMPLE_RATE,
                    language_code=lang_code,
                    enable_automatic_punctuation=True,
                    max_alternatives=1,
                    model=model,
                )
                if use_enhanced_features:
                    rc_kwargs.update(
                        enable_spoken_punctuation=True,
                        enable_word_confidence=True,
                        use_enhanced=True,
                        metadata=cloud_speech.RecognitionMetadata(
                            interaction_type=cloud_speech.RecognitionMetadata.InteractionType.PRESENTATION,
                            microphone_distance=cloud_speech.RecognitionMetadata.MicrophoneDistance.FARFIELD,
                            recording_device_type=cloud_speech.RecognitionMetadata.RecordingDeviceType.OTHER_INDOOR_DEVICE,
                        ),
                    )

                config = cloud_speech.RecognitionConfig(**rc_kwargs)

                sc_kwargs = dict(config=config, interim_results=True)
                if use_voice_activity:
                    sc_kwargs.update(
                        enable_voice_activity_events=True,
                        voice_activity_timeout=cloud_speech.StreamingRecognitionConfig.VoiceActivityTimeout(
                            speech_start_timeout=duration_pb2.Duration(seconds=30),
                            speech_end_timeout=duration_pb2.Duration(seconds=3),
                        ),
                    )

                streaming_config = cloud_speech.StreamingRecognitionConfig(**sc_kwargs)

                features = []
                if use_enhanced_features:
                    features.append("enhanced")
                if use_voice_activity:
                    features.append("voice_activity_timeout=3s")
                feature_str = ", ".join(features) if features else "minimal"
                logger.info(f"[GOOGLE-STREAM] Config: model={model}, lang={lang_code}, features=[{feature_str}]")

                stream_start = time.time()

                def request_generator():
                    while not self._stop_event.is_set():
                        if time.time() - stream_start > STREAM_TIMEOUT_S:
                            logger.info("[GOOGLE-STREAM] Stream timeout, restarting...")
                            return
                        try:
                            audio_bytes = self._audio_queue.get(timeout=0.1)
                            yield cloud_speech.StreamingRecognizeRequest(audio_content=audio_bytes)
                        except queue.Empty:
                            continue

                responses = client.streaming_recognize(
                    config=streaming_config, requests=request_generator())

                for response in responses:
                    if self._stop_event.is_set():
                        break
                    for result in response.results:
                        if not result.alternatives:
                            continue
                        text = result.alternatives[0].transcript.strip()
                        if not text:
                            continue
                        if result.is_final:
                            utterance_id += 1
                            consecutive_errors = 0
                            detected = lang_code.split("-")[0] if "-" in lang_code else lang_code
                            self._broadcast_fn("commit", text, lang=detected)
                            logger.info(f"[GOOGLE-STREAM] FINAL #{utterance_id}: \"{text}\"")
                            if self._stats:
                                self._stats.record_commit("google-final", 0.0, text, detected, sentence_count=1)
                        else:
                            self._broadcast_fn("update", text)

                logger.info(
                    f"[GOOGLE-STREAM] Stream ended after {time.time() - stream_start:.0f}s, "
                    f"{utterance_id} utterances. Restarting...")
                consecutive_errors = 0

            except Exception as e:
                err_str = str(e)
                if "Exceeded maximum allowed stream duration" in err_str:
                    logger.info("[GOOGLE-STREAM] Hit 5-min limit, restarting")
                    consecutive_errors = 0
                elif self._stop_event.is_set():
                    break
                elif model != "default" and "model" in err_str.lower() and (
                        "not supported" in err_str.lower() or "invalid" in err_str.lower()):
                    logger.warning(
                        f"[GOOGLE-STREAM] model '{model}' rejected for {lang_code} — using 'default'")
                    model = "default"
                else:
                    consecutive_errors += 1
                    logger.error(f"[GOOGLE-STREAM] Stream error (attempt {consecutive_errors}): {e}")
                    if consecutive_errors == 3 and use_voice_activity:
                        logger.warning("[GOOGLE-STREAM] 3 consecutive errors — disabling voice_activity_timeout")
                        use_voice_activity = False
                    elif consecutive_errors == 6 and use_enhanced_features:
                        logger.warning("[GOOGLE-STREAM] 6 consecutive errors — disabling enhanced features")
                        use_enhanced_features = False
                    elif consecutive_errors >= 9:
                        logger.error("[GOOGLE-STREAM] 9+ consecutive errors with minimal config — backing off 10s")
                        time.sleep(10.0)
                        continue
                    time.sleep(2.0)

        logger.info(f"[GOOGLE-STREAM] Thread exiting ({utterance_id} total utterances)")


def _create_streaming(api_key, config, broadcast_fn, stats, options):
    """Streaming factory. Returns a GoogleStreamingPipeline when the gRPC
    package is available, else None → caller uses the REST/VAD fallback."""
    try:
        import google.cloud.speech_v1  # noqa: F401
    except ImportError:
        logger.info(
            "google-cloud-speech not installed, using REST accumulation. "
            "For best quality: pip install google-cloud-speech")
        return None
    return GoogleStreamingPipeline(
        api_key=api_key, config=config, broadcast_fn=broadcast_fn, stats=stats)


common.register_engine(
    _ENGINE_KEY,
    requires_model=False,
    create_streaming=_create_streaming,
    transcribe_fn=transcribe,
    vad_preset=vad_preset,
)
