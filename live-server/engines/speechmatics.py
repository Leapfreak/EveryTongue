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
import collections
import json
import logging
import os
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

# max_delay (seconds): finalization lookahead. Higher = fuller, better-punctuated,
# more accurate finals (partials still stream live). An audio A/B showed EOU is the
# main fragmentation lever, but more max_delay improves accuracy/fullness.
DEFAULT_MAX_DELAY_S = 2.0   # matches AppConfig.SpeechmaticsMaxDelayMs (2000)

# ── EOU auto-tune (Phase 1) ──────────────────────────────────────────────────
# Measure the speaker's inter-word pauses and pick the EOU silence trigger to match:
# a slow pauser wants a high EOU (so mid-thought pauses don't split); a fast reader
# wants a low one (or they balloon). Buckets calibrated from a 3-speaker audio A/B
# (fast p85≈30ms→0.7s; moderate ≈240ms→1.0s; slow ≈650ms→1.35s).
PACE_WINDOW = 200            # rolling inter-word gaps kept (samples)
PACE_MIN_SAMPLES = 40        # need at least this many before tuning
PACE_CHECK_INTERVAL_S = 10   # recompute the target EOU this often
RETUNE_COOLDOWN_S = 45       # min seconds between EOU changes (each = a WS reconnect)
EOU_HYST_UP = 0.10           # p85 must exceed the bucket boundary by 10% to go LONGER
EOU_HYST_DOWN = 0.25         # ...and undercut it by 25% to go SHORTER (err long)

# Auto-reconnect on an unexpected Speechmatics drop (network blip, server-side idle
# close during a long silence). The audio queue keeps buffering during the gap so
# no audio is lost; give up only after this many consecutive failed reconnects.
RECONNECT_MAX_ATTEMPTS = 8
RECONNECT_BACKOFF_S = 2.0
RECONNECT_MAX_BACKOFF_S = 15.0


def _load_biblical_vocab(lang):
    """Load the generated biblical proper-noun list for `lang` (any language with a
    list in ..\\vocab) as Speechmatics additional_vocab so STT stops garbling names
    ("Elies"→"aliens"). Returns [] when there's no list for the language."""
    code = (lang or "").split("-")[0].split("_")[0].lower()
    if not code:
        return []
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        "..", "vocab", f"biblical-vocab-{code}.json")
    if not os.path.exists(path):
        return []   # no list for this language — normal, not an error
    try:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        return [e for e in data if isinstance(e, dict) and e.get("content")]
    except Exception as e:
        logging.getLogger("live-server.speechmatics").warning(
            "biblical vocab load failed for %s (%s) — skipping", code, e)
        return []


def _eou_for_pace(p85_ms):
    """Map the speaker's 85th-percentile inter-word pause (ms) to an EOU trigger (s)."""
    if p85_ms < 100:
        return 0.7
    if p85_ms < 400:
        return 1.0
    return 1.35


def _percentile(values, p):
    """Linear-interpolated p-th percentile of an iterable of numbers (0 if empty)."""
    s = sorted(values)
    if not s:
        return 0.0
    k = (len(s) - 1) * (p / 100.0)
    f = int(k)
    c = min(f + 1, len(s) - 1)
    return s[f] + (s[c] - s[f]) * (k - f)

# Sentence-terminating punctuation (Latin + CJK + Arabic) used as an early
# commit trigger when a final segment already closes a sentence.
_SENTENCE_END = ".?!…。？！۔؟"

# 100ms chunks of 16-bit mono PCM.
_CHUNK_SAMPLES = int(SAMPLE_RATE * 0.1)


def _apply_operating_point(kwargs, transcription_config_cls, tier):
    """Set the accuracy tier on TranscriptionConfig kwargs, handling the SDK's
    `operating_point` -> `model` deprecation (same tier names). The SDK is
    unpinned in the Lite image, so a future build may drop the old field —
    prefer `model` when the installed SDK exposes it, fall back otherwise.
    Shared by the streaming pipeline and the one-shot /transcribe path."""
    import inspect
    if "model" in inspect.signature(transcription_config_cls.__init__).parameters:
        kwargs["model"] = str(tier)
    else:
        from speechmatics.rt import OperatingPoint
        try:
            kwargs["operating_point"] = OperatingPoint(str(tier))
        except Exception:
            kwargs["operating_point"] = OperatingPoint.ENHANCED
    return kwargs


class SpeechmaticsStreamingPipeline:
    """Streams audio to Speechmatics real-time API via an AsyncClient on a
    background event loop."""

    def __init__(self, api_key, config, broadcast_fn, stats,
                 region=DEFAULT_REGION, operating_point=DEFAULT_OPERATING_POINT,
                 translation_targets=None, eou_silence_s=None, max_delay_s=None,
                 auto_tune_eou=True, biblical_vocab=False, audio_source="local"):
        self._api_key = api_key
        self._config = config
        # Who fills _audio_queue: "local" = sounddevice capture on this machine,
        # "web" = frames pushed by the app via feed() (browser mic relayed through
        # /audio-in). Everything downstream of the queue is source-agnostic.
        self.audio_source = audio_source if audio_source in ("local", "web") else "local"
        self._last_feed_monotonic = 0.0
        self._broadcast_fn = broadcast_fn
        self._stats = stats
        self._region = region if region in REGION_URLS else DEFAULT_REGION
        self._operating_point = operating_point or DEFAULT_OPERATING_POINT
        # Speechmatics real-time needs an explicit language (no auto-detect).
        lang = (config.language or "en").strip()
        self._language = "en" if lang in ("", "auto") else lang
        # Biblical proper-noun additional_vocab (auto-selected by session language).
        self._biblical_vocab_requested = bool(biblical_vocab)
        self._additional_vocab = _load_biblical_vocab(self._language) if biblical_vocab else []
        self._eou_silence = (eou_silence_s if eou_silence_s and eou_silence_s > 0
                             else DEFAULT_EOU_SILENCE_S)
        self._max_delay = (max_delay_s if max_delay_s and max_delay_s > 0
                           else DEFAULT_MAX_DELAY_S)
        # EOU auto-tune: self._eou_silence is the live value (starts at the config
        # baseline, then the pace tracker nudges it). _pace_gaps persists across
        # reconnects (same speaker); _last_word_end resets each session (timings restart).
        self._auto_tune = bool(auto_tune_eou)
        self._pace_gaps = collections.deque(maxlen=PACE_WINDOW)
        self._last_word_end = None
        self._last_retune = 0.0   # time.monotonic() of the last EOU change
        # Phase 2 (diarization): a speaker change re-measures pace (clears _pace_gaps) but
        # KEEPS the current EOU — only a genuine pace difference then moves it. EOU resets
        # to this baseline on a HOST PAUSE or LANGUAGE CHANGE (via reset_pace), not on a
        # speaker change. _current_speaker tracks the diarization voice.
        self._eou_baseline = self._eou_silence
        self._current_speaker = None
        self._reset_pending = False   # set by host pause / language change → _pump_audio reconnects
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
            f"[SPEECHMATICS] Starting: source={self.audio_source} device={cfg.device_index} "
            f"lang={self._language} region={self._region} operating_point={self._operating_point} "
            f"eou_silence={self._eou_silence}s max_delay={self._max_delay}s "
            f"translation_targets={self._translation_targets}")

        if self.audio_source == "local":
            self._stream = sd.InputStream(
                samplerate=SAMPLE_RATE, channels=1, dtype="int16",
                blocksize=_CHUNK_SAMPLES, device=cfg.device_index,
                callback=self._audio_callback,
            )

        self._thread = threading.Thread(
            target=self._thread_main, name="speechmatics-stream", daemon=True)
        self._thread.start()

        if self._stream is not None:
            self._stream.start()
            logger.info("[SPEECHMATICS] Audio capture started")
        else:
            logger.info("[SPEECHMATICS] Waiting for web-mic frames via /audio-in")

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
        # Reset trigger: host pause / deliberate context change. Lightweight — clear the
        # speaker's pace and drop EOU back to baseline via a WS reconnect (no full restart).
        if kwargs.get("reset_pace"):
            self._pace_gaps.clear()
            self._last_word_end = None
            self._current_speaker = None
            self._last_retune = 0.0
            if abs(self._eou_silence - self._eou_baseline) >= 0.05:
                self._eou_silence = self._eou_baseline
                self._reset_pending = True
            logger.info("[SPEECHMATICS] pace reset (host pause / context change)")
            return

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
            # Reset trigger: a manual language/target change is a deliberate context
            # switch — start the pace fresh at the baseline EOU, don't carry it over.
            self._pace_gaps.clear()
            self._last_word_end = None
            self._current_speaker = None
            self._last_retune = 0.0
            self._eou_silence = self._eou_baseline
            logger.info(
                f"[SPEECHMATICS] config change → lang={self._language} "
                f"targets={self._translation_targets}; restarting session (pace reset)")
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

    def feed(self, frame_bytes):
        """Web-mic path: push a 16kHz mono s16 PCM frame into the queue —
        the exact hand-off the sounddevice callback does for the local mic,
        so everything downstream (sender, EOU tune, reconnect) is untouched."""
        try:
            self._audio_queue.put_nowait(frame_bytes)
        except queue.Full:
            pass  # drop frame rather than block (matches local behaviour)
        self._audio_callback_count += 1
        self._last_feed_monotonic = time.monotonic()

    def web_feed_recent(self, window_s=5.0):
        """True when web-mic frames arrived within the window — the honest
        'capturing' signal for /health when audio_source == 'web'."""
        return (self._last_feed_monotonic > 0
                and (time.monotonic() - self._last_feed_monotonic) < window_s)

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
                ServerMessageType, TranscriptionConfig,
                TranscriptResult, TranslationConfig,
            )
        except ImportError as e:
            self._thread_error = f"Missing package: {e}"
            logger.error(
                f"[SPEECHMATICS] speechmatics-rt not installed. "
                f"Run: pip install speechmatics-rt\n{e}")
            return

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

        def _feed_pace(message):
            # Feed inter-word gaps to the pace tracker, AND reset per speaker (Phase 2):
            # when diarization reports a different voice, log it (for noise auditing),
            # start that speaker's pace fresh, and drop the EOU back to baseline so the
            # new speaker never inherits the previous one's tuned value.
            if not self._auto_tune:
                return
            try:
                results = message.get("results", []) if isinstance(message, dict) else []
            except Exception:
                return
            for r in results or []:
                try:
                    alt = (r.get("alternatives") or [{}])[0]
                    st = r.get("start_time")
                    et = r.get("end_time")
                    spk = alt.get("speaker")
                    word = alt.get("content", "")
                except Exception:
                    continue
                if spk and spk != self._current_speaker:
                    if self._current_speaker is not None:
                        tail = self._pending.strip()[-50:]
                        logger.info(
                            f"[SPEECHMATICS] SPEAKER CHANGE {self._current_speaker}->{spk} "
                            f"| prev: \"...{tail}\" | new: \"{word}\"")
                        # Re-measure pace for the new speaker, but KEEP the current EOU
                        # (do NOT reset to baseline). The old reset-then-reclimb caused
                        # two wasteful reconnects per speaker change — and a 1-word
                        # diarization blip churned for nothing. If the new speaker's pace
                        # genuinely differs, _maybe_retune moves the EOU (one reconnect,
                        # only when needed); if it's the same pace, no reconnect at all.
                        self._pace_gaps.clear()
                        self._last_word_end = None
                        self._last_retune = 0.0
                    self._current_speaker = spk
                if st is None or et is None:
                    continue
                if self._last_word_end is not None:
                    gap = (float(st) - self._last_word_end) * 1000.0
                    if 0 <= gap <= 6000:
                        self._pace_gaps.append(gap)
                self._last_word_end = float(et)

        # Reconnect loop: the audio-capture thread keeps filling _audio_queue
        # independently, so when the EOU auto-tune reconnects with a new value — OR
        # the connection drops unexpectedly (network blip / idle close on a long
        # silence) — the buffered audio flows to the fresh session; no audio is lost.
        reconnect_failures = 0
        while not self._stop_event.is_set():
            self._last_word_end = None       # word timings restart with each session
            self._current_speaker = None     # diarization voice labels restart too
            self._reset_pending = False
            retune = False
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
                        _feed_pace(message)
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

                    tc_kwargs = dict(
                        language=self._language,
                        enable_partials=True,
                        max_delay=self._max_delay,
                        # Biblical proper-noun boost (auto-selected by language) so STT
                        # stops garbling names; None when no list for this language.
                        additional_vocab=(self._additional_vocab or None),
                        # Diarization drives the per-speaker pace reset (only needed when
                        # auto-tuning). Speaker label arrives in results[].alternatives[0].speaker.
                        diarization=("speaker" if self._auto_tune else None),
                        conversation_config=ConversationConfig(
                            end_of_utterance_silence_trigger=self._eou_silence),
                    )
                    _apply_operating_point(tc_kwargs, TranscriptionConfig, self._operating_point)
                    transcription_config = TranscriptionConfig(**tc_kwargs)
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
                    logger.info(
                        f"[SPEECHMATICS] Session started "
                        f"(eou={self._eou_silence}s max_delay={self._max_delay}s "
                        f"auto_tune={self._auto_tune} vocab={len(self._additional_vocab)})")
                    if self._additional_vocab:
                        sample = ", ".join(str(e.get("content", "")) for e in self._additional_vocab[:10])
                        logger.info(
                            f"[SPEECHMATICS] additional_vocab passed to engine: "
                            f"{len(self._additional_vocab)} biblical terms for lang={self._language} "
                            f"(e.g. {sample})")
                    elif self._biblical_vocab_requested:
                        logger.info(
                            f"[SPEECHMATICS] additional_vocab requested but no list for lang="
                            f"{self._language} — run Download Manager → Biblical Vocabulary")

                    retune = await self._pump_audio(client, loop)

                    _flush_commit()  # emit any trailing accumulated utterance
                    try:
                        await client.stop_session()
                    except Exception as e:
                        logger.debug(f"[SPEECHMATICS] stop_session: {e}")
                reconnect_failures = 0   # session ran cleanly → reset the drop counter
            except Exception as e:
                if self._stop_event.is_set():
                    break   # intentional stop during teardown — not a failure
                reconnect_failures += 1
                if reconnect_failures >= RECONNECT_MAX_ATTEMPTS:
                    self._thread_error = str(e)
                    logger.error(f"[SPEECHMATICS] connection lost — giving up after "
                                 f"{reconnect_failures} reconnect attempts: {e}")
                    break
                backoff = min(RECONNECT_BACKOFF_S * reconnect_failures, RECONNECT_MAX_BACKOFF_S)
                logger.warning(f"[SPEECHMATICS] connection lost ({e}); reconnecting in "
                               f"{backoff:.0f}s (attempt {reconnect_failures}/{RECONNECT_MAX_ATTEMPTS})")
                await asyncio.sleep(backoff)
                continue   # audio kept buffering → reconnect with no loss

            if not retune or self._stop_event.is_set():
                break
            logger.info(f"[SPEECHMATICS] EOU auto-tune: reconnecting with EOU={self._eou_silence}s")

    async def _pump_audio(self, client, loop):
        """Drain captured PCM frames and forward them to Speechmatics. Returns True if
        an EOU retune was requested (the caller reconnects with the new self._eou_silence);
        False on normal stop / send failure."""
        last_check = time.monotonic()
        while not self._stop_event.is_set():
            frame = await loop.run_in_executor(None, self._next_frame)
            if frame is not None:
                try:
                    await client.send_audio(frame)
                except Exception as e:
                    if self._stop_event.is_set():
                        return False   # sending during teardown — a normal stop
                    # Connection dropped: propagate so _run's reconnect loop retries
                    # (the audio queue keeps buffering during the gap → no loss).
                    logger.warning(f"[SPEECHMATICS] send_audio failed (connection dropped): {e}")
                    raise
            if self._auto_tune:
                if self._reset_pending:
                    self._reset_pending = False
                    return True   # speaker change → reconnect at the baseline EOU
                now = time.monotonic()
                if (now - last_check) >= PACE_CHECK_INTERVAL_S:
                    last_check = now
                    if self._maybe_retune(now):
                        return True
        return False

    def _maybe_retune(self, now):
        """Recompute the EOU target from the speaker's pace; if it crosses a bucket and
        the cooldown has elapsed, update self._eou_silence and signal a reconnect.

        BOUNDARY HYSTERESIS (2026-07-12): a speaker whose p85 sits ON a bucket
        boundary (observed: 313-484ms straddling the 400ms line) ping-pongs between
        buckets every window, and every flip is a full session reconnect. A bucket
        change now requires p85 to CLEAR the crossed boundary by a margin — and the
        margins are asymmetric (going shorter needs 25%, longer only 10%) so a
        borderline speaker settles on the LONGER EOU, which is the safer side
        (fewer mid-thought cuts; slightly later commits).
        """
        if len(self._pace_gaps) < PACE_MIN_SAMPLES:
            return False
        p85 = _percentile(self._pace_gaps, 85)
        target = _eou_for_pace(p85)
        if abs(target - self._eou_silence) < 0.05:
            return False   # same bucket, nothing to do
        if (now - self._last_retune) < RETUNE_COOLDOWN_S:
            return False   # too soon since the last change
        # Hysteresis: p85 must clear the boundary between the buckets by a margin.
        if target > self._eou_silence:
            boundary = 400.0 if target >= 1.35 else 100.0
            if p85 <= boundary * (1 + EOU_HYST_UP):
                return False   # not clearly past the line — stay put
        else:
            boundary = 100.0 if target <= 0.7 else 400.0
            if p85 >= boundary * (1 - EOU_HYST_DOWN):
                return False   # borderline — prefer staying on the longer EOU
        old = self._eou_silence
        self._eou_silence = target
        self._last_retune = now
        logger.info(
            f"[SPEECHMATICS] EOU auto-tune {old}s -> {target}s "
            f"(p85 pause {int(p85)}ms over {len(self._pace_gaps)} words)")
        return True

    def _next_frame(self):
        try:
            return self._audio_queue.get(timeout=0.2)
        except queue.Empty:
            return None


def _transcribe_speechmatics(audio_array, language=None,
                             beam_size=5, best_of=1, initial_prompt=""):
    """One-shot transcription via a short-lived Speechmatics RT session.
    Serves conversation-room PTT on servers with no offline whisper (Lite).
    Billed by audio duration, same as any session. beam/best_of/prompt are
    whisper-isms — accepted and ignored. Returns (segments, info) or (None, None)."""
    api_key = common.get_api_key(_ENGINE_KEY)
    if not api_key:
        logger.error("[SPEECHMATICS] /transcribe: no API key configured")
        return None, None

    lang = (language or "").strip().lower()
    if not lang or lang == "auto":
        lang = "en"   # RT needs an explicit language; PTT callers pass the speaker's

    import numpy as np
    a = audio_array
    if a.dtype != np.int16:
        a = (np.clip(a, -1.0, 1.0) * 32767.0).astype(np.int16)
    pcm = a.tobytes()
    duration_s = len(a) / float(SAMPLE_RATE)

    result = {"texts": [], "error": None}

    def _worker():
        # Own thread + own event loop: /transcribe may be called from an async
        # context where asyncio.run() would be illegal.
        async def _run_once():
            import io as _io
            from speechmatics.rt import (
                AsyncClient, AudioEncoding, AudioFormat,
                ServerMessageType, TranscriptionConfig, TranscriptResult,
            )
            kwargs = dict(language=lang, enable_partials=False, max_delay=1.0)
            _apply_operating_point(kwargs, TranscriptionConfig, DEFAULT_OPERATING_POINT)
            async with AsyncClient(api_key=api_key,
                                   url=REGION_URLS[DEFAULT_REGION]) as client:
                @client.on(ServerMessageType.ADD_TRANSCRIPT)
                def _collect(message):
                    try:
                        t = TranscriptResult.from_message(message).metadata.transcript or ""
                    except Exception:
                        t = (message.get("metadata", {}) or {}).get("transcript", "") or ""
                    if t:
                        result["texts"].append(t)
                # transcribe() streams the buffer, waits for finals, returns on EOS.
                await client.transcribe(
                    _io.BytesIO(pcm),
                    transcription_config=TranscriptionConfig(**kwargs),
                    audio_format=AudioFormat(
                        encoding=AudioEncoding.PCM_S16LE, sample_rate=SAMPLE_RATE,
                        chunk_size=_CHUNK_SAMPLES * 2),
                    timeout=max(20.0, duration_s * 2 + 15.0),
                )
        try:
            asyncio.run(_run_once())
        except Exception as e:
            result["error"] = f"{type(e).__name__}: {e}"

    t0 = time.time()
    worker = threading.Thread(target=_worker, name="sm-oneshot", daemon=True)
    worker.start()
    worker.join(timeout=max(30.0, duration_s * 2 + 20.0))
    if worker.is_alive():
        logger.error("[SPEECHMATICS] /transcribe: session timed out")
        return None, None
    if result["error"]:
        logger.error(f"[SPEECHMATICS] /transcribe failed: {result['error']}")
        return None, None

    text = "".join(result["texts"]).strip()
    logger.info(f"[SPEECHMATICS] one-shot: {duration_s:.1f}s audio -> "
                f"{time.time() - t0:.1f}s, {len(text)} chars, lang={lang}")
    segments = []
    if text:
        segments.append(common.SegmentInfo(
            start=0.0, end=duration_s, text=text,
            no_speech_prob=0.0, avg_logprob=0.0, words=[]))
    return segments, common.TranscribeInfo(language=lang)


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
        max_delay_s=options.get("speechmatics_max_delay_s"),
        auto_tune_eou=options.get("speechmatics_auto_tune_eou", True),
        biblical_vocab=options.get("speechmatics_biblical_vocab", False),
        audio_source=options.get("audio_source", "local"),
    )


common.register_engine(
    _ENGINE_KEY,
    requires_model=False,
    create_streaming=_create_streaming,
    transcribe_fn=_transcribe_speechmatics,
)
