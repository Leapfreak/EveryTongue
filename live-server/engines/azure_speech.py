"""Azure AI Speech real-time Speech-to-Text engine (online) — fully self-contained.

Streams microphone audio to Azure AI Speech via the official
``azure-cognitiveservices-speech`` SDK (PushAudioInputStream + continuous
recognition). Azure endpoints server-side, so there is no local VAD:
``recognizing`` events are emitted as ``update`` events and ``recognized``
(RecognizedSpeech) results as ``commit`` events — the same update/commit model
the other online engines use.

Audio is captured with sounddevice (matching the rest of the app's device
selection) as 16 kHz mono 16-bit PCM and pushed into the SDK's input stream.
The SDK import is LAZY (inside the streaming thread): the live-server boots and
all other engines work even when the package is not installed — the missing
package surfaces as a clear engine-start error via ``_thread_error``.

Requires: pip install azure-cognitiveservices-speech
Registered at import time via engines.common.register_engine("azure-speech").
"""
import logging
import queue
import threading

import sounddevice as sd

from . import common
from .common import SAMPLE_RATE

logger = logging.getLogger("live-server")

_ENGINE_KEY = "azure-speech"

DEFAULT_REGION = "westeurope"

# Continuous language identification accepts at most 10 candidate locales
# (at-start LID would cap at 4; we run continuous recognition so we use
# continuous LID via SpeechServiceConnection_LanguageIdMode=Continuous).
_MAX_AUTODETECT_CANDIDATES = 10

# Default auto-detect candidate set (ISO 639-1) when none is configured.
DEFAULT_AUTODETECT = ["en", "es", "fr", "de", "it", "pt", "nl", "pl"]

# Whisper ISO 639-1 → Azure BCP-47 locale for codes where the simple
# f"{iso}-{ISO.upper()}" heuristic picks a wrong/nonexistent locale, plus the
# ambiguous pluricentric languages (en/es/pt/zh) pinned to sensible defaults.
_LOCALE_MAP = {
    "af": "af-ZA",
    "am": "am-ET",
    "ar": "ar-SA",
    "as": "as-IN",
    "be": "be-BY",
    "bn": "bn-IN",
    "bs": "bs-BA",
    "ca": "ca-ES",
    "cs": "cs-CZ",
    "cy": "cy-GB",
    "da": "da-DK",
    "el": "el-GR",
    "en": "en-US",
    "es": "es-ES",
    "et": "et-EE",
    "eu": "eu-ES",
    "fa": "fa-IR",
    "ga": "ga-IE",
    "gl": "gl-ES",
    "gu": "gu-IN",
    "he": "he-IL",
    "hi": "hi-IN",
    "hy": "hy-AM",
    "ja": "ja-JP",
    "jw": "jv-ID",
    "ka": "ka-GE",
    "kk": "kk-KZ",
    "km": "km-KH",
    "kn": "kn-IN",
    "ko": "ko-KR",
    "lo": "lo-LA",
    "ml": "ml-IN",
    "mr": "mr-IN",
    "ms": "ms-MY",
    "my": "my-MM",
    "nb": "nb-NO",
    "ne": "ne-NP",
    "nn": "nb-NO",
    "no": "nb-NO",
    "pa": "pa-IN",
    "ps": "ps-AF",
    "pt": "pt-PT",
    "si": "si-LK",
    "sl": "sl-SI",
    "sq": "sq-AL",
    "sr": "sr-RS",
    "su": "su-ID",
    "sv": "sv-SE",
    "sw": "sw-KE",
    "ta": "ta-IN",
    "te": "te-IN",
    "tl": "fil-PH",
    "uk": "uk-UA",
    "ur": "ur-PK",
    "vi": "vi-VN",
    "wo": "wo-SN",
    "yue": "zh-HK",
    "zh": "zh-CN",
    "zu": "zu-ZA",
}

# 100ms chunks of 16-bit mono PCM.
_CHUNK_SAMPLES = int(SAMPLE_RATE * 0.1)


def _iso_to_locale(code):
    """Map a whisper ISO 639-1 code to an Azure BCP-47 locale."""
    code = (code or "").strip()
    if not code or code.lower() == "auto":
        return "en-US"
    if "-" in code:
        return code  # already a BCP-47 locale (user supplied)
    iso = code.lower()
    if iso in _LOCALE_MAP:
        return _LOCALE_MAP[iso]
    # Heuristic: most remaining codes follow xx → xx-XX (it-IT, pl-PL, ...).
    return f"{iso}-{iso.upper()}"


class AzureSpeechStreamingPipeline:
    """Streams audio to Azure AI Speech via the official SDK's push stream on a
    background thread (same threading shape as the other online engines)."""

    def __init__(self, api_key, config, broadcast_fn, stats,
                 region=DEFAULT_REGION, segmentation_ms=None,
                 autodetect_languages=None):
        self._api_key = api_key
        self._config = config
        self._broadcast_fn = broadcast_fn
        self._stats = stats
        self._region = (region or DEFAULT_REGION).strip() or DEFAULT_REGION
        # Whisper ISO 639-1 code, or "auto" → continuous language identification.
        self._language = (config.language or "auto").strip() or "auto"
        # Segmentation silence in ms; None/0 = Azure default.
        try:
            self._segmentation_ms = int(segmentation_ms) if segmentation_ms else 0
        except (TypeError, ValueError):
            self._segmentation_ms = 0
        # Auto-detect candidate languages (CSV of ISO 639-1 codes).
        raw = (autodetect_languages or "").strip()
        self._autodetect = [c.strip() for c in raw.split(",") if c.strip()] if raw else []
        self._stop_event = threading.Event()
        self._audio_queue = queue.Queue(maxsize=1000)
        self._stream = None
        self._thread = None
        self._audio_callback_count = 0
        self._thread_error = None
        # Last language Azure reported (auto mode), ISO 639-1.
        self._last_detected_lang = ""

    @property
    def stats(self):
        return self._stats

    # ---------------------------------------------------------------- control
    def start(self):
        cfg = self._config
        self._stop_event.clear()  # allow restart (stop() sets the event)
        logger.info(
            f"[AZURE-SPEECH] Starting: device={cfg.device_index} lang={self._language} "
            f"region={self._region} segmentation_ms={self._segmentation_ms or 'default'}")

        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE, channels=1, dtype="int16",
            blocksize=_CHUNK_SAMPLES, device=cfg.device_index,
            callback=self._audio_callback,
        )

        self._thread = threading.Thread(
            target=self._thread_main, name="azure-speech-stream", daemon=True)
        self._thread.start()

        self._stream.start()
        logger.info("[AZURE-SPEECH] Audio capture started")

    def stop(self):
        logger.info("[AZURE-SPEECH] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[AZURE-SPEECH] Error closing stream: {e}")
            self._stream = None
        if self._thread:
            self._thread.join(timeout=10.0)
            self._thread = None
        logger.info("[AZURE-SPEECH] Stopped")

    def is_alive(self):
        if self._thread is None:
            return False, "no thread"
        if not self._thread.is_alive():
            return False, f"thread dead ({self._thread_error or 'exited'})"
        return True, f"ok (callbacks={self._audio_callback_count})"

    def update_config(self, **kwargs):
        # Azure fixes language / auto-detect candidates per recognizer, so a
        # language change requires restarting the session (stop + start) — same
        # approach as the other online engines.
        if "language" in kwargs:
            new_lang = (kwargs["language"] or "auto").strip() or "auto"
            if new_lang != self._language:
                self._language = new_lang
                logger.info(f"[AZURE-SPEECH] language change → {new_lang}; restarting session")
                try:
                    self.stop()
                    self.start()
                except Exception as e:
                    logger.error(f"[AZURE-SPEECH] restart after config change failed: {e}")

    # ------------------------------------------------------------------ audio
    def _audio_callback(self, indata, frames, time_info, status):
        try:
            if status:
                logger.warning(f"[AZURE-SPEECH] Audio status: {status}")
            try:
                self._audio_queue.put_nowait(indata.tobytes())
            except queue.Full:
                pass  # drop frame rather than block
            self._audio_callback_count += 1
        except Exception as e:
            if self._audio_callback_count < 5:
                logger.error(f"[AZURE-SPEECH] Audio callback error: {e}")

    def _next_frame(self):
        try:
            return self._audio_queue.get(timeout=0.2)
        except queue.Empty:
            return None

    # -------------------------------------------------------------- streaming
    def _thread_main(self):
        try:
            self._run()
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[AZURE-SPEECH] Streaming thread error: {e}")

    def _autodetect_candidates(self):
        """BCP-47 candidate locales for continuous language identification."""
        codes = self._autodetect or DEFAULT_AUTODETECT
        locales = [_iso_to_locale(c) for c in codes]
        deduped = list(dict.fromkeys(locales))
        if len(deduped) > _MAX_AUTODETECT_CANDIDATES:
            logger.warning(
                f"[AZURE-SPEECH] {len(deduped)} auto-detect languages configured; "
                f"Azure allows {_MAX_AUTODETECT_CANDIDATES} — truncating")
            deduped = deduped[:_MAX_AUTODETECT_CANDIDATES]
        return deduped

    def _commit_lang(self, result, speechsdk):
        """Language code attached to commits (ISO 639-1)."""
        if self._language not in ("", "auto"):
            return self._language
        try:
            detected = speechsdk.AutoDetectSourceLanguageResult(result).language or ""
        except Exception:
            detected = ""
        if detected:
            self._last_detected_lang = detected.split("-")[0].lower()
        return self._last_detected_lang or "en"

    def _run(self):
        # LAZY import — the live-server must boot (and every other engine must
        # work) without this SDK installed. A missing package is surfaced as an
        # engine-start error via _thread_error / is_alive, like the other engines.
        try:
            import azure.cognitiveservices.speech as speechsdk
        except ImportError as e:
            self._thread_error = f"Missing package: {e}"
            logger.error(
                f"[AZURE-SPEECH] azure-cognitiveservices-speech not installed. "
                f"Run: pip install azure-cognitiveservices-speech\n{e}")
            return

        speech_config = speechsdk.SpeechConfig(
            subscription=self._api_key, region=self._region)
        if self._segmentation_ms > 0:
            speech_config.set_property(
                speechsdk.PropertyId.Speech_SegmentationSilenceTimeoutMs,
                str(self._segmentation_ms))

        fmt = speechsdk.audio.AudioStreamFormat(
            samples_per_second=SAMPLE_RATE, bits_per_sample=16, channels=1)
        push_stream = speechsdk.audio.PushAudioInputStream(stream_format=fmt)
        audio_config = speechsdk.audio.AudioConfig(stream=push_stream)

        if self._language in ("", "auto"):
            candidates = self._autodetect_candidates()
            speech_config.set_property(
                speechsdk.PropertyId.SpeechServiceConnection_LanguageIdMode,
                "Continuous")
            auto_cfg = speechsdk.languageconfig.AutoDetectSourceLanguageConfig(
                languages=candidates)
            recognizer = speechsdk.SpeechRecognizer(
                speech_config=speech_config,
                auto_detect_source_language_config=auto_cfg,
                audio_config=audio_config)
            logger.info(f"[AZURE-SPEECH] Auto-detect candidates: {candidates}")
        else:
            speech_config.speech_recognition_language = _iso_to_locale(self._language)
            recognizer = speechsdk.SpeechRecognizer(
                speech_config=speech_config, audio_config=audio_config)

        done = threading.Event()

        # SDK events fire on Azure SDK worker threads; broadcast_fn is
        # thread-safe (same usage as the other engines' worker threads).
        def _on_recognizing(evt):
            try:
                text = (evt.result.text or "").strip()
                if text:
                    self._broadcast_fn("update", text)
            except Exception as e:
                logger.error(f"[AZURE-SPEECH] recognizing handler error: {e}")

        def _on_recognized(evt):
            try:
                if evt.result.reason != speechsdk.ResultReason.RecognizedSpeech:
                    return  # NoMatch etc.
                text = (evt.result.text or "").strip()
                if not text:
                    return
                lang = self._commit_lang(evt.result, speechsdk)
                self._broadcast_fn("commit", text, lang=lang)
                logger.info(f"[AZURE-SPEECH] COMMIT: \"{text}\"")
                if self._stats:
                    self._stats.record_commit(
                        "azure-final", 0.0, text, lang, sentence_count=1)
            except Exception as e:
                logger.error(f"[AZURE-SPEECH] recognized handler error: {e}")

        def _on_canceled(evt):
            try:
                details = getattr(evt, "cancellation_details", None)
                if details is not None and details.reason == speechsdk.CancellationReason.Error:
                    self._thread_error = f"Azure error: {details.error_details}"
                    logger.error(f"[AZURE-SPEECH] Canceled (error): {details.error_details}")
                else:
                    logger.info(f"[AZURE-SPEECH] Canceled: {getattr(details, 'reason', evt)}")
            except Exception as e:
                logger.error(f"[AZURE-SPEECH] canceled handler error: {e}")
            done.set()

        def _on_session_stopped(evt):
            logger.info("[AZURE-SPEECH] Session stopped")
            done.set()

        recognizer.recognizing.connect(_on_recognizing)
        recognizer.recognized.connect(_on_recognized)
        recognizer.canceled.connect(_on_canceled)
        recognizer.session_stopped.connect(_on_session_stopped)

        try:
            recognizer.start_continuous_recognition()
            logger.info(f"[AZURE-SPEECH] Session started (region={self._region})")

            # Pump captured PCM frames into the SDK push stream until stopped
            # or the session dies (canceled / session_stopped).
            while not self._stop_event.is_set() and not done.is_set():
                frame = self._next_frame()
                if frame is None:
                    continue
                try:
                    push_stream.write(frame)
                except Exception as e:
                    logger.error(f"[AZURE-SPEECH] write audio failed: {e}")
                    break
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[AZURE-SPEECH] Session error: {e}")
        finally:
            # Signal end-of-stream so Azure flushes any trailing final result,
            # then stop recognition (blocks briefly).
            try:
                push_stream.close()
            except Exception as e:
                logger.debug(f"[AZURE-SPEECH] push_stream.close: {e}")
            try:
                recognizer.stop_continuous_recognition()
            except Exception as e:
                logger.debug(f"[AZURE-SPEECH] stop_continuous_recognition: {e}")

        # Keep the error visible if the session died on its own (done set while
        # capture was still requested).
        if done.is_set() and not self._stop_event.is_set() and not self._thread_error:
            self._thread_error = "session ended unexpectedly"


def _create_streaming(api_key, config, broadcast_fn, stats, options):
    options = options or {}
    return AzureSpeechStreamingPipeline(
        api_key=api_key, config=config, broadcast_fn=broadcast_fn, stats=stats,
        region=options.get("azure_region", DEFAULT_REGION),
        segmentation_ms=options.get("azure_segmentation_ms"),
        autodetect_languages=options.get("azure_autodetect_languages"),
    )


common.register_engine(
    _ENGINE_KEY,
    requires_model=False,
    create_streaming=_create_streaming,
)
