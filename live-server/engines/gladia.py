"""Gladia real-time Speech-to-Text engine (online) — fully self-contained.

Gladia's v2 live API is a two-step handshake:

  1. POST https://api.gladia.io/v2/live (header ``x-gladia-key``) with the
     session config (sample rate / encoding / language hints). The response
     contains a per-session websocket ``url`` (token embedded in the URL).
  2. Connect to that URL and stream raw PCM as binary frames; Gladia sends
     back JSON messages with partial/final utterance transcripts.

Gladia endpoints server-side, so there is no local VAD: partial transcripts
are emitted as ``update`` events and final utterances as ``commit`` events.
Unknown message types are ignored defensively; errors are logged and surfaced
through is_alive() like the other online engines.

Uses the ``websockets`` package already bundled for speechmatics-rt and the
stdlib http.client for the one-shot session POST — no new dependencies.

Registered at import time via engines.common.register_engine("gladia").
"""
import asyncio
import http.client
import json
import logging
import queue
import ssl
import threading

import sounddevice as sd

from . import common
from .common import SAMPLE_RATE

logger = logging.getLogger("live-server")

_ENGINE_KEY = "gladia"

API_HOST = "api.gladia.io"
INIT_PATH = "/v2/live"

# 100ms chunks of 16-bit mono PCM.
_CHUNK_SAMPLES = int(SAMPLE_RATE * 0.1)


class GladiaStreamingPipeline:
    """Streams audio to the Gladia v2 live API via a websocket on a background
    event loop (same threading shape as the Speechmatics engine)."""

    def __init__(self, api_key, config, broadcast_fn, stats, endpointing_s=None):
        self._api_key = api_key
        self._config = config
        self._broadcast_fn = broadcast_fn
        self._stats = stats
        # Whisper ISO 639-1 code, or "auto" → language detection (see _init_session).
        self._language = (config.language or "auto").strip() or "auto"
        # Endpointing silence in seconds; None/0 = Gladia default.
        try:
            self._endpointing_s = float(endpointing_s) if endpointing_s else 0.0
        except (TypeError, ValueError):
            self._endpointing_s = 0.0
        self._stop_event = threading.Event()
        self._audio_queue = queue.Queue(maxsize=1000)
        self._stream = None
        self._thread = None
        self._audio_callback_count = 0
        self._thread_error = None

    @property
    def stats(self):
        return self._stats

    # ------------------------------------------------------------------ setup
    def _init_session(self):
        """POST the session config to Gladia; returns the session websocket URL.
        Raises on any HTTP/parse failure (caught by the streaming thread)."""
        body = {
            "encoding": "wav/pcm",
            "sample_rate": SAMPLE_RATE,
            "bit_depth": 16,
            "channels": 1,
        }
        if self._language in ("", "auto"):
            # Empty language list = all languages; code_switching lets Gladia
            # re-detect the language on each utterance.
            body["language_config"] = {"languages": [], "code_switching": True}
        else:
            body["language_config"] = {"languages": [self._language],
                                       "code_switching": False}
        if self._endpointing_s > 0:
            body["endpointing"] = self._endpointing_s

        payload = json.dumps(body).encode("utf-8")
        headers = {
            "Content-Type": "application/json",
            "x-gladia-key": self._api_key,
        }
        conn = http.client.HTTPSConnection(
            API_HOST, timeout=15, context=ssl.create_default_context())
        try:
            conn.request("POST", INIT_PATH, body=payload, headers=headers)
            resp = conn.getresponse()
            data = resp.read()
        finally:
            try:
                conn.close()
            except Exception:
                pass

        if resp.status not in (200, 201):
            detail = data.decode("utf-8", errors="replace")[:300]
            raise RuntimeError(f"session init HTTP {resp.status}: {detail}")
        info = json.loads(data)
        url = (info.get("url") or "") if isinstance(info, dict) else ""
        if not url:
            raise RuntimeError(f"session init: no websocket url in response: {data[:300]}")
        logger.info(f"[GLADIA] Session created (id={info.get('id', '?')})")
        return url

    # ---------------------------------------------------------------- control
    def start(self):
        cfg = self._config
        self._stop_event.clear()  # allow restart (stop() sets the event)
        logger.info(
            f"[GLADIA] Starting: device={cfg.device_index} lang={self._language} "
            f"endpointing_s={self._endpointing_s or 'default'}")

        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE, channels=1, dtype="int16",
            blocksize=_CHUNK_SAMPLES, device=cfg.device_index,
            callback=self._audio_callback,
        )

        self._thread = threading.Thread(
            target=self._thread_main, name="gladia-stream", daemon=True)
        self._thread.start()

        self._stream.start()
        logger.info("[GLADIA] Audio capture started")

    def stop(self):
        logger.info("[GLADIA] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[GLADIA] Error closing stream: {e}")
            self._stream = None
        if self._thread:
            self._thread.join(timeout=5.0)
            self._thread = None
        logger.info("[GLADIA] Stopped")

    def is_alive(self):
        if self._thread is None:
            return False, "no thread"
        if not self._thread.is_alive():
            return False, f"thread dead ({self._thread_error or 'exited'})"
        return True, f"ok (callbacks={self._audio_callback_count})"

    def update_config(self, **kwargs):
        # Gladia fixes the language config at session creation, so a language
        # change requires restarting the session (stop + start) — same approach
        # as the Speechmatics engine.
        if "language" in kwargs:
            new_lang = (kwargs["language"] or "auto").strip() or "auto"
            if new_lang != self._language:
                self._language = new_lang
                logger.info(f"[GLADIA] language change → {new_lang}; restarting session")
                try:
                    self.stop()
                    self.start()
                except Exception as e:
                    logger.error(f"[GLADIA] restart after config change failed: {e}")

    # ------------------------------------------------------------------ audio
    def _audio_callback(self, indata, frames, time_info, status):
        try:
            if status:
                logger.warning(f"[GLADIA] Audio status: {status}")
            try:
                self._audio_queue.put_nowait(indata.tobytes())
            except queue.Full:
                pass  # drop frame rather than block
            self._audio_callback_count += 1
        except Exception as e:
            if self._audio_callback_count < 5:
                logger.error(f"[GLADIA] Audio callback error: {e}")

    # -------------------------------------------------------------- streaming
    def _thread_main(self):
        try:
            url = self._init_session()
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[GLADIA] Session init failed: {e}")
            return
        try:
            asyncio.run(self._run(url))
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[GLADIA] Streaming thread error: {e}")

    def _commit_lang(self, detected):
        """Language code attached to commits (ISO 639-1)."""
        if detected:
            return str(detected).split("-")[0]
        if self._language not in ("", "auto"):
            return self._language
        return "en"

    def _handle_message(self, raw):
        try:
            msg = json.loads(raw)
        except Exception:
            return
        if not isinstance(msg, dict):
            return
        mtype = msg.get("type", "")
        if mtype == "transcript":
            data = msg.get("data") or {}
            utt = data.get("utterance") or {}
            text = (utt.get("text") or "").strip()
            if not text:
                return
            if data.get("is_final"):
                lang = self._commit_lang(utt.get("language"))
                self._broadcast_fn("commit", text, lang=lang)
                logger.info(f"[GLADIA] COMMIT: \"{text}\"")
                if self._stats:
                    self._stats.record_commit(
                        "gladia-final", 0.0, text, lang, sentence_count=1)
            else:
                self._broadcast_fn("update", text)
        elif mtype == "error":
            logger.error(f"[GLADIA] Server error: {msg}")
        # audio_chunk acks / speech_start / speech_end / lifecycle /
        # post-processing messages and unknown types: ignore.

    async def _run(self, url):
        try:
            from websockets.asyncio.client import connect
        except ImportError as e:
            self._thread_error = f"Missing package: {e}"
            logger.error(f"[GLADIA] websockets package not installed: {e}")
            return

        loop = asyncio.get_running_loop()
        try:
            async with connect(url, max_size=2 ** 23) as ws:
                logger.info("[GLADIA] Session started")

                async def _recv_loop():
                    async for raw in ws:
                        try:
                            self._handle_message(raw)
                        except Exception as e:
                            logger.error(f"[GLADIA] Message handling error: {e}")

                recv_task = asyncio.ensure_future(_recv_loop())

                await self._pump_audio(ws, loop)

                # Graceful shutdown: ask Gladia to flush remaining finals.
                try:
                    await ws.send(json.dumps({"type": "stop_recording"}))
                except Exception as e:
                    logger.debug(f"[GLADIA] stop_recording send: {e}")
                try:
                    await asyncio.wait_for(recv_task, timeout=3.0)
                except (asyncio.TimeoutError, Exception):
                    recv_task.cancel()
        except Exception as e:
            self._thread_error = str(e)
            logger.error(f"[GLADIA] Session error: {e}")

    async def _pump_audio(self, ws, loop):
        """Drain captured PCM frames and forward them to Gladia as binary."""
        while not self._stop_event.is_set():
            frame = await loop.run_in_executor(None, self._next_frame)
            if frame is None:
                continue
            try:
                await ws.send(frame)
            except Exception as e:
                logger.error(f"[GLADIA] send audio failed: {e}")
                break

    def _next_frame(self):
        try:
            return self._audio_queue.get(timeout=0.2)
        except queue.Empty:
            return None


def _create_streaming(api_key, config, broadcast_fn, stats, options):
    options = options or {}
    return GladiaStreamingPipeline(
        api_key=api_key, config=config, broadcast_fn=broadcast_fn, stats=stats,
        endpointing_s=options.get("gladia_endpointing_s"),
    )


common.register_engine(
    _ENGINE_KEY,
    requires_model=False,
    create_streaming=_create_streaming,
)
