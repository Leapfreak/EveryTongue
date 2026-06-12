"""Deepgram real-time Speech-to-Text engine (online) — fully self-contained.

Streams microphone audio to the Deepgram streaming WebSocket API
(wss://api.deepgram.com/v1/listen). Deepgram handles endpointing server-side,
so there is no local VAD: interim results are emitted as ``update`` events and
finalized results as ``commit`` events at speech_final boundaries — the same
update/commit model the other online engines use.

Audio is captured with sounddevice (matching the rest of the app's device
selection) as 16 kHz mono 16-bit PCM and sent as binary frames. Uses the
``websockets`` package already bundled for speechmatics-rt — no SDK needed.

Registered at import time via engines.common.register_engine("deepgram").
"""
import asyncio
import json
import logging
import queue
import threading
import urllib.parse

import sounddevice as sd

from . import common
from .common import SAMPLE_RATE

logger = logging.getLogger("live-server")

_ENGINE_KEY = "deepgram"

WS_BASE_URL = "wss://api.deepgram.com/v1/listen"
DEFAULT_MODEL = "nova-3"

# 100ms chunks of 16-bit mono PCM.
_CHUNK_SAMPLES = int(SAMPLE_RATE * 0.1)


class DeepgramStreamingPipeline:
    """Streams audio to the Deepgram real-time API via a websocket on a
    background event loop (same threading shape as the Speechmatics engine)."""

    def __init__(self, api_key, config, broadcast_fn, stats,
                 model=DEFAULT_MODEL, endpointing_ms=None):
        self._api_key = api_key
        self._config = config
        self._broadcast_fn = broadcast_fn
        self._stats = stats
        self._model = (model or DEFAULT_MODEL).strip() or DEFAULT_MODEL
        # Whisper ISO 639-1 code, or "auto" → multi/omitted (see _resolve_language).
        self._language = (config.language or "auto").strip() or "auto"
        # Endpointing silence in ms; None/0 = Deepgram default.
        try:
            self._endpointing_ms = int(endpointing_ms) if endpointing_ms else 0
        except (TypeError, ValueError):
            self._endpointing_ms = 0
        self._stop_event = threading.Event()
        self._audio_queue = queue.Queue(maxsize=1000)
        self._stream = None
        self._thread = None
        self._audio_callback_count = 0
        self._thread_error = None
        # Accumulates is_final segments until a speech_final boundary.
        self._pending = ""
        # Last language(s) Deepgram reported (multi/auto mode).
        self._last_detected_lang = ""

    @property
    def stats(self):
        return self._stats

    # ------------------------------------------------------------------ setup
    def _resolve_language(self):
        """Deepgram ``language`` query param for the session, or "" to omit.

        Deepgram accepts ISO 639-1 codes directly. For "auto": nova-3 supports
        multilingual code-switching via ``language=multi``; older models have no
        streaming auto-detect, so the param is omitted (Deepgram defaults to en).
        """
        if self._language in ("", "auto"):
            return "multi" if self._model.startswith("nova-3") else ""
        return self._language

    def _build_url(self):
        params = {
            "model": self._model,
            "encoding": "linear16",
            "sample_rate": str(SAMPLE_RATE),
            "channels": "1",
            "interim_results": "true",
            "punctuate": "true",
        }
        lang = self._resolve_language()
        if lang:
            params["language"] = lang
        if self._endpointing_ms > 0:
            params["endpointing"] = str(self._endpointing_ms)
        return WS_BASE_URL + "?" + urllib.parse.urlencode(params)

    # ---------------------------------------------------------------- control
    def start(self):
        cfg = self._config
        self._stop_event.clear()  # allow restart (stop() sets the event)
        logger.info(
            f"[DEEPGRAM] Starting: device={cfg.device_index} lang={self._language} "
            f"model={self._model} endpointing_ms={self._endpointing_ms or 'default'}")

        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE, channels=1, dtype="int16",
            blocksize=_CHUNK_SAMPLES, device=cfg.device_index,
            callback=self._audio_callback,
        )

        self._thread = threading.Thread(
            target=self._thread_main, name="deepgram-stream", daemon=True)
        self._thread.start()

        self._stream.start()
        logger.info("[DEEPGRAM] Audio capture started")

    def stop(self):
        logger.info("[DEEPGRAM] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[DEEPGRAM] Error closing stream: {e}")
            self._stream = None
        if self._thread:
            self._thread.join(timeout=5.0)
            self._thread = None
        logger.info("[DEEPGRAM] Stopped")

    def is_alive(self):
        if self._thread is None:
            return False, "no thread"
        if not self._thread.is_alive():
            return False, f"thread dead ({self._thread_error or 'exited'})"
        return True, f"ok (callbacks={self._audio_callback_count})"

    def update_config(self, **kwargs):
        # Deepgram fixes language/model per websocket connection, so a language
        # change requires restarting the session (stop + start) — same approach
        # as the Speechmatics engine.
        if "language" in kwargs:
            new_lang = (kwargs["language"] or "auto").strip() or "auto"
            if new_lang != self._language:
                self._language = new_lang
                logger.info(f"[DEEPGRAM] language change → {new_lang}; restarting session")
                try:
                    self.stop()
                    self.start()
                except Exception as e:
                    logger.error(f"[DEEPGRAM] restart after config change failed: {e}")

    # ------------------------------------------------------------------ audio
    def _audio_callback(self, indata, frames, time_info, status):
        try:
            if status:
                logger.warning(f"[DEEPGRAM] Audio status: {status}")
            try:
                self._audio_queue.put_nowait(indata.tobytes())
            except queue.Full:
                pass  # drop frame rather than block
            self._audio_callback_count += 1
        except Exception as e:
            if self._audio_callback_count < 5:
                logger.error(f"[DEEPGRAM] Audio callback error: {e}")

    # -------------------------------------------------------------- streaming
    def _thread_main(self):
        try:
            asyncio.run(self._run())
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[DEEPGRAM] Streaming thread error: {e}")

    def _commit_lang(self):
        """Language code attached to commits (ISO 639-1)."""
        if self._language not in ("", "auto"):
            return self._language
        return self._last_detected_lang or "en"

    def _flush_commit(self):
        text = self._pending.strip()
        self._pending = ""
        if not text:
            return
        lang = self._commit_lang()
        self._broadcast_fn("commit", text, lang=lang)
        logger.info(f"[DEEPGRAM] COMMIT: \"{text}\"")
        if self._stats:
            self._stats.record_commit(
                "deepgram-final", 0.0, text, lang, sentence_count=1)

    def _handle_message(self, raw):
        try:
            msg = json.loads(raw)
        except Exception:
            return
        if not isinstance(msg, dict):
            return
        mtype = msg.get("type", "")
        if mtype == "Results":
            try:
                alts = ((msg.get("channel") or {}).get("alternatives") or [])
                alt = alts[0] if alts else {}
            except Exception:
                alt = {}
            text = (alt.get("transcript") or "").strip()
            langs = alt.get("languages") or []
            if langs:
                self._last_detected_lang = str(langs[0]).split("-")[0]
            if msg.get("is_final"):
                # Finalized segment — accumulate until the endpoint.
                if text:
                    self._pending = (self._pending + " " + text).strip()
                if msg.get("speech_final"):
                    self._flush_commit()
                elif self._pending:
                    self._broadcast_fn("update", self._pending)
            elif text:
                # Interim — live preview = committed-so-far buffer + interim.
                live = (self._pending + " " + text).strip()
                self._broadcast_fn("update", live)
        elif mtype == "UtteranceEnd":
            # Safety net endpoint marker (only sent when utterance_end_ms is
            # configured; harmless to handle regardless).
            self._flush_commit()
        elif mtype == "Error":
            logger.error(f"[DEEPGRAM] Server error: {msg}")
        # Metadata / SpeechStarted / unknown types: ignore.

    async def _run(self):
        try:
            from websockets.asyncio.client import connect
        except ImportError as e:
            self._thread_error = f"Missing package: {e}"
            logger.error(f"[DEEPGRAM] websockets package not installed: {e}")
            return

        url = self._build_url()
        headers = {"Authorization": f"Token {self._api_key}"}
        loop = asyncio.get_running_loop()

        try:
            async with connect(url, additional_headers=headers,
                               max_size=2 ** 23) as ws:
                logger.info(f"[DEEPGRAM] Session started (model={self._model})")

                async def _recv_loop():
                    async for raw in ws:
                        try:
                            self._handle_message(raw)
                        except Exception as e:
                            logger.error(f"[DEEPGRAM] Message handling error: {e}")

                recv_task = asyncio.ensure_future(_recv_loop())

                await self._pump_audio(ws, loop)

                # Graceful shutdown: ask Deepgram to flush remaining finals.
                try:
                    await ws.send(json.dumps({"type": "CloseStream"}))
                except Exception as e:
                    logger.debug(f"[DEEPGRAM] CloseStream send: {e}")
                try:
                    await asyncio.wait_for(recv_task, timeout=3.0)
                except (asyncio.TimeoutError, Exception):
                    recv_task.cancel()

                self._flush_commit()  # emit any trailing accumulated utterance
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[DEEPGRAM] Session error: {e}")

    async def _pump_audio(self, ws, loop):
        """Drain captured PCM frames and forward them to Deepgram."""
        while not self._stop_event.is_set():
            frame = await loop.run_in_executor(None, self._next_frame)
            if frame is None:
                continue
            try:
                await ws.send(frame)
            except Exception as e:
                logger.error(f"[DEEPGRAM] send audio failed: {e}")
                break

    def _next_frame(self):
        try:
            return self._audio_queue.get(timeout=0.2)
        except queue.Empty:
            return None


def _create_streaming(api_key, config, broadcast_fn, stats, options):
    options = options or {}
    return DeepgramStreamingPipeline(
        api_key=api_key, config=config, broadcast_fn=broadcast_fn, stats=stats,
        model=options.get("deepgram_model", DEFAULT_MODEL),
        endpointing_ms=options.get("deepgram_endpointing_ms"),
    )


common.register_engine(
    _ENGINE_KEY,
    requires_model=False,
    create_streaming=_create_streaming,
)
