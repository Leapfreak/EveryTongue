"""Speechmatics real-time Speech-to-Text engine (online) — fully self-contained.

Streams microphone audio to the Speechmatics real-time WebSocket API. Speechmatics
handles endpointing, so there is no local VAD: partial transcripts are emitted as
``update`` events and final transcripts as ``commit`` events — a 1:1 map to the
live-server's update/commit model.

Audio is captured with sounddevice (matching the rest of the app's device
selection) rather than the SDK's PyAudio ``Microphone`` helper. The async
``speechmatics.rt.AsyncClient`` runs inside a dedicated thread's event loop.

Requires: pip install speechmatics-rt
Registered at import time via engines.common.register_engine("speechmatics").
"""
import asyncio
import logging
import queue
import threading
import time

import sounddevice as sd

from . import common
from .common import SAMPLE_RATE

logger = logging.getLogger("live-server")

_ENGINE_KEY = "speechmatics"

# Real-time WebSocket endpoints by region.
REGION_URLS = {
    "eu2": "wss://eu2.rt.speechmatics.com/v2",
    "us": "wss://us.rt.speechmatics.com/v2",
}
DEFAULT_REGION = "eu2"
DEFAULT_OPERATING_POINT = "enhanced"

# Silence (seconds) that ends an utterance → triggers END_OF_UTTERANCE / commit.
DEFAULT_EOU_SILENCE_S = 0.8

# Sentence-terminating punctuation (Latin + CJK + Arabic) used as an early
# commit trigger when a final segment already closes a sentence.
_SENTENCE_END = ".?!…。？！۔؟"

# 100ms chunks of 16-bit mono PCM.
_CHUNK_SAMPLES = int(SAMPLE_RATE * 0.1)


class SpeechmaticsStreamingPipeline:
    """Streams audio to Speechmatics real-time API via an AsyncClient on a
    background event loop."""

    def __init__(self, api_key, config, broadcast_fn, stats,
                 region=DEFAULT_REGION, operating_point=DEFAULT_OPERATING_POINT,
                 translation_targets=None, eou_silence_s=None):
        self._api_key = api_key
        self._config = config
        self._broadcast_fn = broadcast_fn
        self._stats = stats
        self._region = region if region in REGION_URLS else DEFAULT_REGION
        self._operating_point = operating_point or DEFAULT_OPERATING_POINT
        # Speechmatics real-time needs an explicit language (no auto-detect).
        lang = (config.language or "en").strip()
        self._language = "en" if lang in ("", "auto") else lang
        self._eou_silence = (eou_silence_s if eou_silence_s and eou_silence_s > 0
                             else DEFAULT_EOU_SILENCE_S)
        # Speechmatics translation targets (its own ISO codes, e.g. "es","de","cmn").
        # Set from /start options; English-pivot only, capped at 5 by the caller.
        self._translation_targets = list(translation_targets or [])
        self._translation_enabled = len(self._translation_targets) > 0
        self._tx_grace = 0.3  # secs to collect trailing translation segments after EOU
        self._stop_event = threading.Event()
        self._audio_queue = queue.Queue(maxsize=1000)
        self._stream = None
        self._thread = None
        self._audio_callback_count = 0
        self._thread_error = None
        # Accumulates incremental final segments until an utterance boundary.
        self._pending = ""
        # Accumulates incremental translation segments per target language.
        self._pending_tx = {}

    @property
    def stats(self):
        return self._stats

    def start(self):
        cfg = self._config
        self._stop_event.clear()  # allow restart (stop() sets the event)
        logger.info(
            f"[SPEECHMATICS] Starting: device={cfg.device_index} lang={self._language} "
            f"region={self._region} operating_point={self._operating_point} "
            f"eou_silence={self._eou_silence}s "
            f"translation_targets={self._translation_targets}")

        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE, channels=1, dtype="int16",
            blocksize=_CHUNK_SAMPLES, device=cfg.device_index,
            callback=self._audio_callback,
        )

        self._thread = threading.Thread(
            target=self._thread_main, name="speechmatics-stream", daemon=True)
        self._thread.start()

        self._stream.start()
        logger.info("[SPEECHMATICS] Audio capture started")

    def stop(self):
        logger.info("[SPEECHMATICS] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[SPEECHMATICS] Error closing stream: {e}")
            self._stream = None
        if self._thread:
            self._thread.join(timeout=5.0)
            self._thread = None
        logger.info("[SPEECHMATICS] Stopped")

    def is_alive(self):
        if self._thread is None:
            return False, "no thread"
        if not self._thread.is_alive():
            return False, f"thread dead ({self._thread_error or 'exited'})"
        return True, f"ok (callbacks={self._audio_callback_count})"

    def update_config(self, **kwargs):
        # Speechmatics fixes language + translation targets at session start, so a
        # change requires restarting the session (stop + start). The user accepted
        # the brief audio gap this causes.
        restart = False
        if "language" in kwargs:
            new_lang = (kwargs["language"] or "en").strip()
            new_lang = "en" if new_lang in ("", "auto") else new_lang
            if new_lang != self._language:
                self._language = new_lang
                restart = True
        if "translation_targets" in kwargs:
            new_targets = list(kwargs["translation_targets"] or [])
            if new_targets != self._translation_targets:
                self._translation_targets = new_targets
                self._translation_enabled = len(new_targets) > 0
                restart = True
        if restart:
            logger.info(
                f"[SPEECHMATICS] config change → lang={self._language} "
                f"targets={self._translation_targets}; restarting session")
            try:
                self.stop()
                self.start()
            except Exception as e:
                logger.error(f"[SPEECHMATICS] restart after config change failed: {e}")

    def _audio_callback(self, indata, frames, time_info, status):
        try:
            if status:
                logger.warning(f"[SPEECHMATICS] Audio status: {status}")
            try:
                self._audio_queue.put_nowait(indata.tobytes())
            except queue.Full:
                pass  # drop frame rather than block
            self._audio_callback_count += 1
        except Exception as e:
            if self._audio_callback_count < 5:
                logger.error(f"[SPEECHMATICS] Audio callback error: {e}")

    def _thread_main(self):
        try:
            asyncio.run(self._run())
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[SPEECHMATICS] Streaming thread error: {e}")

    async def _run(self):
        try:
            from speechmatics.rt import (
                AsyncClient, AudioEncoding, AudioFormat, ConversationConfig,
                OperatingPoint, ServerMessageType, TranscriptionConfig,
                TranscriptResult, TranslationConfig,
            )
        except ImportError as e:
            self._thread_error = f"Missing package: {e}"
            logger.error(
                f"[SPEECHMATICS] speechmatics-rt not installed. "
                f"Run: pip install speechmatics-rt\n{e}")
            return

        try:
            operating_point = OperatingPoint(self._operating_point)
        except Exception:
            operating_point = OperatingPoint.ENHANCED

        url = REGION_URLS.get(self._region, REGION_URLS[DEFAULT_REGION])
        loop = asyncio.get_running_loop()

        def _extract(message):
            # NOTE: do NOT strip — Speechmatics' per-segment transcript carries the
            # leading space needed to concatenate segments into a correct sentence.
            try:
                return TranscriptResult.from_message(message).metadata.transcript or ""
            except Exception:
                try:
                    return (message.get("metadata", {}) or {}).get("transcript", "") or ""
                except Exception:
                    return ""

        def _extract_translation(message):
            # AddTranslation: {"language": "es", "results": [{"content": "..."}]}.
            # No SDK wrapper exists for translation, so parse the raw message.
            try:
                lang = message.get("language", "") if isinstance(message, dict) else ""
                results = (message.get("results", []) if isinstance(message, dict) else []) or []
            except Exception:
                return "", ""
            parts = []
            for r in results:
                try:
                    c = r.get("content", "")
                except Exception:
                    c = ""
                if c:
                    parts.append(c)
            return " ".join(parts), lang

        def _flush_commit():
            # Emit the accumulated utterance as a single commit (full sentence(s)),
            # carrying any accumulated per-language translations. Then reset.
            text = self._pending.strip()
            translations = {k: v.strip() for k, v in self._pending_tx.items() if v.strip()}
            self._pending = ""
            self._pending_tx = {}
            if not text:
                return
            self._broadcast_fn("commit", text, lang=self._language,
                               translations=(translations or None))
            tx_note = f" tx={list(translations.keys())}" if translations else ""
            logger.info(f"[SPEECHMATICS] COMMIT: \"{text}\"{tx_note}")
            if self._stats:
                self._stats.record_commit(
                    "speechmatics-final", 0.0, text, self._language, sentence_count=1)

        try:
            async with AsyncClient(api_key=self._api_key, url=url) as client:

                @client.on(ServerMessageType.ADD_PARTIAL_TRANSCRIPT)
                def _on_partial(message):
                    # Live preview = committed-so-far buffer + the in-progress partial.
                    live = (self._pending + _extract(message)).strip()
                    if live:
                        self._broadcast_fn("update", live)

                @client.on(ServerMessageType.ADD_TRANSCRIPT)
                def _on_final(message):
                    # ADD_TRANSCRIPT is an *incremental* final segment, not a whole
                    # sentence. Accumulate, then commit at an utterance boundary
                    # (END_OF_UTTERANCE) or, when translation is OFF, when the buffer
                    # already ends a sentence. With translation ON we wait for EOU so
                    # the translations (which lag the transcript) line up.
                    self._pending += _extract(message)
                    stripped = self._pending.strip()
                    if stripped and not self._translation_enabled and stripped[-1] in _SENTENCE_END:
                        _flush_commit()
                    elif stripped:
                        self._broadcast_fn("update", stripped)

                @client.on(ServerMessageType.ADD_TRANSLATION)
                def _on_translation(message):
                    text, lang = _extract_translation(message)
                    if lang and text:
                        prev = self._pending_tx.get(lang, "")
                        self._pending_tx[lang] = (prev + " " + text).strip() if prev else text.strip()

                @client.on(ServerMessageType.END_OF_UTTERANCE)
                def _on_eou(message):
                    # When translating, defer briefly to collect trailing translation
                    # segments that arrive just after the transcript's EOU.
                    if self._translation_enabled:
                        loop.call_later(self._tx_grace, _flush_commit)
                    else:
                        _flush_commit()

                @client.on(ServerMessageType.ERROR)
                def _on_error(message):
                    logger.error(f"[SPEECHMATICS] Server error: {message}")

                transcription_config = TranscriptionConfig(
                    language=self._language,
                    operating_point=operating_point,
                    enable_partials=True,
                    max_delay=1.0,
                    conversation_config=ConversationConfig(
                        end_of_utterance_silence_trigger=self._eou_silence),
                )
                audio_format = AudioFormat(
                    encoding=AudioEncoding.PCM_S16LE,
                    sample_rate=SAMPLE_RATE,
                    chunk_size=_CHUNK_SAMPLES * 2,  # bytes (int16 = 2 bytes/sample)
                )
                translation_config = None
                if self._translation_targets:
                    # Partials disabled: the app's update model carries one text, so
                    # per-language live translation preview isn't supported.
                    translation_config = TranslationConfig(
                        target_languages=list(self._translation_targets),
                        enable_partials=False)

                await client.start_session(
                    transcription_config=transcription_config,
                    audio_format=audio_format,
                    translation_config=translation_config,
                )
                logger.info("[SPEECHMATICS] Session started")

                await self._pump_audio(client, loop)

                _flush_commit()  # emit any trailing accumulated utterance
                try:
                    await client.stop_session()
                except Exception as e:
                    logger.debug(f"[SPEECHMATICS] stop_session: {e}")
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[SPEECHMATICS] Session error: {e}")

    async def _pump_audio(self, client, loop):
        """Drain captured PCM frames and forward them to Speechmatics."""
        while not self._stop_event.is_set():
            frame = await loop.run_in_executor(None, self._next_frame)
            if frame is None:
                continue
            try:
                await client.send_audio(frame)
            except Exception as e:
                logger.error(f"[SPEECHMATICS] send_audio failed: {e}")
                break

    def _next_frame(self):
        try:
            return self._audio_queue.get(timeout=0.2)
        except queue.Empty:
            return None


def _create_streaming(api_key, config, broadcast_fn, stats, options):
    options = options or {}
    targets = options.get("translation_targets") or []
    if not options.get("enable_translation", False):
        targets = []
    return SpeechmaticsStreamingPipeline(
        api_key=api_key, config=config, broadcast_fn=broadcast_fn, stats=stats,
        region=options.get("speechmatics_region", DEFAULT_REGION),
        operating_point=options.get("speechmatics_operating_point", DEFAULT_OPERATING_POINT),
        translation_targets=targets,
        eou_silence_s=options.get("speechmatics_eou_silence_s"),
    )


common.register_engine(
    _ENGINE_KEY,
    requires_model=False,
    create_streaming=_create_streaming,
)
