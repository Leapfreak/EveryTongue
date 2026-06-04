"""VadPipeline orchestrator.

Owns all queues, threads, and internal wiring for the VAD capture pipeline.
server.py creates a VadPipeline instance, passes callbacks, and calls
start()/stop().

Thread architecture:
    Audio callback (OS thread) -> preroll.write + vad_queue.put
    VAD thread -> reads vad_queue, runs FrameVAD, feeds state machine
    Transcription worker -> reads transcribe_queue, runs Whisper, broadcasts
    Interim worker (optional) -> reads interim_queue, runs Whisper, broadcasts
"""
import logging
import queue
import threading
import traceback
import time

import numpy as np
import sounddevice as sd

from .buffers import PrerollBuffer, UtteranceBuffer
from .frame_vad import FrameVAD
from .state_machine import UtteranceStateMachine
from .merger import BoundaryMerger, split_sentences

logger = logging.getLogger("live-server")

SAMPLE_RATE = 16000


class VadPipeline:
    """Orchestrates the VAD capture pipeline: audio -> VAD -> Whisper -> broadcast."""

    def __init__(self, silero_model, config, transcribe_fn, broadcast_fn,
                 hallucination_fn, stats):
        """
        Args:
            silero_model: loaded Silero VAD model
            config: VadConfig with all tuning parameters
            transcribe_fn: (audio, language, beam_size, initial_prompt) -> (segments, info)
            broadcast_fn: (event_type, text, lang) -> None
            hallucination_fn: (segments, last_text, lang, recent_langs) -> bool
            stats: SessionStats object for telemetry
        """
        self._model = silero_model
        self._config = config
        self._transcribe_fn = transcribe_fn
        self._broadcast_fn = broadcast_fn
        self._hallucination_fn = hallucination_fn
        self._stats = stats
        self._stop_event = threading.Event()
        self._threads = []
        self._stream = None
        self._preroll = None
        self._sm = None
        self._vad = None
        self._vad_queue = None
        self._transcribe_queue = None
        self._interim_queue = None
        self._merger = None
        self._recent_langs = []
        # Debugging / health monitoring
        self._audio_callback_count = 0
        self._audio_callback_errors = 0
        self._thread_errors = {}  # thread_name -> error message

    @property
    def stats(self):
        return self._stats

    def start(self):
        """Start all pipeline threads and begin processing audio.

        Opens the sounddevice stream, creates all internal components,
        and starts worker threads. Raises on audio device errors.
        """
        cfg = self._config
        logger.debug("[PIPELINE] Creating buffers...")

        # Create buffers
        preroll = PrerollBuffer(duration_ms=cfg.vad_preroll_ms)
        utterance = UtteranceBuffer()
        self._preroll = preroll
        logger.debug(
            f"[PIPELINE] Buffers OK: preroll={cfg.vad_preroll_ms}ms "
            f"({preroll._size} samples)"
        )

        # Create queues
        self._vad_queue = queue.Queue(maxsize=500)
        self._transcribe_queue = queue.Queue(maxsize=10)
        self._interim_queue = (
            queue.Queue(maxsize=2) if cfg.enable_interim else None
        )
        logger.debug("[PIPELINE] Queues OK: vad=500, transcribe=10, interim=2")

        # Create frame-level VAD with hysteresis
        logger.debug("[PIPELINE] Creating FrameVAD...")
        vad = FrameVAD(
            self._model,
            speech_threshold=cfg.vad_speech_threshold,
            silence_threshold=cfg.vad_silence_threshold,
            speech_confirm_frames=cfg.vad_speech_confirm_frames,
        )
        vad.reset()
        self._vad = vad
        logger.debug(
            f"[PIPELINE] FrameVAD OK: speech_thresh={cfg.vad_speech_threshold} "
            f"silence_thresh={cfg.vad_silence_threshold} "
            f"confirm_frames={cfg.vad_speech_confirm_frames}"
        )

        # Create boundary merger
        self._merger = BoundaryMerger(cfg.merge_similarity_threshold)

        # Non-blocking commit callback for the state machine
        def on_commit(audio, commit_type):
            try:
                self._transcribe_queue.put_nowait((audio, commit_type))
            except queue.Full:
                logger.warning("[STATE] transcribe_queue full -- dropping utterance")
                self._stats.record_drop()

        # Create state machine
        sm = UtteranceStateMachine(
            preroll, utterance, on_commit,
            soft_commit_ms=cfg.soft_commit_ms,
            silence_commit_ms=cfg.vad_silence_ms,
            max_utterance_s=cfg.vad_max_segment_s,
            max_soft_utterance_s=cfg.vad_max_soft_segment_s,
            interim_queue=self._interim_queue,
            interim_interval_s=cfg.interim_interval_s,
        )
        self._sm = sm
        logger.debug(
            f"[PIPELINE] StateMachine OK: soft={cfg.soft_commit_ms}ms "
            f"silence={cfg.vad_silence_ms}ms max={cfg.vad_max_segment_s}s "
            f"max_soft={cfg.vad_max_soft_segment_s}s"
        )

        # Open audio stream (blocksize=1536 ensures each callback = one Silero frame)
        logger.debug(
            f"[PIPELINE] Opening audio stream: device={cfg.device_index} "
            f"rate={SAMPLE_RATE} blocksize={FrameVAD.SILERO_FRAME_SAMPLES}"
        )
        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE,
            channels=1,
            dtype="float32",
            blocksize=FrameVAD.SILERO_FRAME_SAMPLES,
            device=cfg.device_index,
            callback=self._audio_callback,
        )
        logger.debug("[PIPELINE] Audio stream created OK")

        # Start worker threads
        vad_thread = threading.Thread(
            target=self._vad_thread, name="vad-thread", daemon=True
        )
        transcribe_thread = threading.Thread(
            target=self._transcription_worker, name="transcribe-worker", daemon=True
        )
        self._threads = [vad_thread, transcribe_thread]

        if self._interim_queue is not None:
            interim_thread = threading.Thread(
                target=self._interim_worker, name="interim-worker", daemon=True
            )
            self._threads.append(interim_thread)

        for t in self._threads:
            t.start()
            logger.debug(f"[PIPELINE] Thread '{t.name}' started")

        self._stream.start()

        logger.debug(
            f"[PIPELINE] READY: device={cfg.device_index} lang={cfg.language} "
            f"soft_commit={cfg.soft_commit_ms}ms silence={cfg.vad_silence_ms}ms "
            f"max_seg={cfg.vad_max_segment_s}s preroll={cfg.vad_preroll_ms}ms "
            f"beam={cfg.beam_size} interim={cfg.enable_interim}"
        )

    def stop(self):
        """Signal all threads to stop and wait for them to finish."""
        logger.debug("[PIPELINE] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[PIPELINE] Error closing stream: {e}")
            self._stream = None
        for t in self._threads:
            t.join(timeout=5.0)
            if t.is_alive():
                logger.warning(f"[PIPELINE] Thread '{t.name}' did not stop within 5s")
        self._threads.clear()
        logger.debug(
            f"[PIPELINE] Stopped. audio_callbacks={self._audio_callback_count} "
            f"callback_errors={self._audio_callback_errors} "
            f"thread_errors={self._thread_errors}"
        )
        if self._stats:
            logger.debug("\n" + self._stats.summary())

    def is_alive(self):
        """Check if the pipeline is healthy (all threads running, audio flowing)."""
        if not self._threads:
            return False, "no threads"
        dead = [t.name for t in self._threads if not t.is_alive()]
        if dead:
            errors = {n: self._thread_errors.get(n, "unknown") for n in dead}
            return False, f"dead threads: {errors}"
        if self._thread_errors:
            return False, f"thread errors: {self._thread_errors}"
        if self._audio_callback_count == 0:
            return True, "waiting for first audio callback"
        return True, f"ok (callbacks={self._audio_callback_count})"

    def update_config(self, **kwargs):
        """Update tunable parameters at runtime (e.g. from /config endpoint)."""
        if self._sm:
            self._sm.update_thresholds(
                soft_commit_ms=kwargs.get("soft_commit_ms"),
                silence_commit_ms=kwargs.get("vad_min_silence_ms"),
                max_utterance_s=kwargs.get("vad_max_segment_s"),
                max_soft_utterance_s=kwargs.get("vad_max_soft_segment_s"),
            )
        if self._vad:
            if "vad_speech_threshold" in kwargs:
                self._vad._speech_thresh = kwargs["vad_speech_threshold"]
            if "vad_silence_threshold" in kwargs:
                self._vad._silence_thresh = kwargs["vad_silence_threshold"]
        if "language" in kwargs:
            self._config.language = kwargs["language"]

    # ------------------------------------------------------------------
    # Audio callback (runs on OS audio thread)
    # ------------------------------------------------------------------
    def _audio_callback(self, indata, frames, time_info, status):
        """Minimal audio callback: write to pre-roll and enqueue for VAD.

        NEVER touches the utterance buffer or state machine -- those are
        owned exclusively by the VAD thread.

        IMPORTANT: sounddevice silently swallows exceptions in callbacks.
        Any unhandled exception here = silent failure, audio stops flowing
        to the pipeline with no error in logs. Wrap EVERYTHING in try/except.
        """
        try:
            if status:
                logger.warning(f"[AUDIO] callback status: {status}")
            samples = indata[:, 0].astype(np.float32)
            self._preroll.write(samples)
            try:
                self._vad_queue.put_nowait(samples)
            except queue.Full:
                pass  # drop frame rather than block audio thread
            self._audio_callback_count += 1
            # Log first callback only; heartbeat every ~5 min (10000 callbacks at 32ms)
            if self._audio_callback_count == 1:
                logger.debug(
                    f"[AUDIO] First callback: {len(samples)} samples, "
                    f"dtype={samples.dtype}, range=[{samples.min():.4f}, {samples.max():.4f}]"
                )
            elif self._audio_callback_count % 10000 == 0:
                logger.debug(
                    f"[AUDIO] Heartbeat: {self._audio_callback_count} callbacks "
                    f"({self._audio_callback_count * 512 / 16000 / 60:.0f}min)"
                )
        except Exception as e:
            self._audio_callback_errors += 1
            if self._audio_callback_errors <= 5:
                # Can't use logger here safely (might be in wrong thread context)
                # but logger is thread-safe in CPython, so it's fine
                logger.error(f"[AUDIO] callback EXCEPTION #{self._audio_callback_errors}: {e}")

    # ------------------------------------------------------------------
    # VAD thread (sole owner of utterance buffer + state machine)
    # ------------------------------------------------------------------
    def _vad_thread(self):
        """VAD processing thread -- reads frames, runs Silero, feeds state machine."""
        thread_name = "vad-thread"
        logger.debug(f"[{thread_name}] Started, waiting for audio frames...")
        frame_count = 0
        try:
            while not self._stop_event.is_set():
                try:
                    frame = self._vad_queue.get(timeout=0.5)
                except queue.Empty:
                    continue
                frame_count += 1
                if frame_count == 1:
                    logger.debug(f"[{thread_name}] Processing first frame ({len(frame)} samples)")
                prob, is_speech = self._vad.process_frame(frame)
                self._sm.feed(prob, is_speech, frame)
        except Exception as e:
            self._thread_errors[thread_name] = f"{type(e).__name__}: {e}"
            logger.error(
                f"[{thread_name}] CRASHED after {frame_count} frames: {e}\n"
                f"{traceback.format_exc()}"
            )
        logger.debug(f"[{thread_name}] Exited ({frame_count} frames processed)")

    # ------------------------------------------------------------------
    # Transcription worker thread
    # ------------------------------------------------------------------
    def _transcription_worker(self):
        """Dedicated thread for Whisper inference on committed utterances."""
        thread_name = "transcribe-worker"
        logger.debug(f"[{thread_name}] Started, waiting for committed utterances...")
        utterance_id = 0
        try:
            while not self._stop_event.is_set():
                try:
                    item = self._transcribe_queue.get(timeout=1.0)
                except queue.Empty:
                    continue

                audio, commit_type = item
                utterance_id += 1
                speech_dur = len(audio) / SAMPLE_RATE
                logger.debug(
                    f"[WHISPER] utterance #{utterance_id}: {speech_dur:.1f}s audio, "
                    f"type={commit_type}, queue_depth={self._transcribe_queue.qsize()}"
                )

                language = self._config.language
                if language == "auto":
                    language = None

                t0 = time.time()
                try:
                    segments, info = self._transcribe_fn(
                        audio, language=language,
                        beam_size=self._config.beam_size,
                        initial_prompt=self._config.initial_prompt,
                    )
                except Exception as e:
                    logger.error(
                        f"[WHISPER] utterance #{utterance_id}: transcribe_fn error: {e}\n"
                        f"{traceback.format_exc()}"
                    )
                    continue
                inference_dur = time.time() - t0

                if segments is None:
                    logger.debug(
                        f"[WHISPER] utterance #{utterance_id}: inference returned None "
                        f"(whisper-server error?) after {inference_dur:.1f}s"
                    )
                    continue

                detected_lang = info.language if info else ""
                full_text = " ".join(
                    seg.text.strip() for seg in segments if seg.text.strip()
                )
                if not full_text:
                    logger.debug(
                        f"[WHISPER] utterance #{utterance_id}: empty after transcription "
                        f"({len(segments)} segments, {inference_dur:.1f}s inference)"
                    )
                    continue

                if self._hallucination_fn(
                    segments, self._merger.last_commit_text,
                    detected_lang, self._recent_langs
                ):
                    logger.debug(
                        f"[WHISPER] HALLUCINATION #{utterance_id}: \"{full_text}\""
                    )
                    self._stats.record_hallucination()
                    continue

                # Boundary merge (only for FORCE-COMMIT where audio overlap exists)
                merged_text, did_strip = self._merger.merge(full_text, commit_type)
                if did_strip:
                    self._stats.record_merge_strip()

                # Split into sentences and broadcast each individually
                if self._config.enable_sentence_split:
                    sentences = split_sentences(merged_text)
                else:
                    sentences = []
                if not sentences:
                    sentences = [merged_text]  # fallback: emit as-is

                for i, sentence in enumerate(sentences):
                    self._broadcast_fn("commit", sentence, lang=detected_lang)
                    logger.debug(
                        f"[WHISPER] utterance #{utterance_id} "
                        f"sentence {i+1}/{len(sentences)}: \"{sentence}\""
                    )

                logger.debug(
                    f"[WHISPER] utterance #{utterance_id}: {speech_dur:.1f}s audio -> "
                    f"{inference_dur:.1f}s inference -> {len(sentences)} sentence(s) "
                    f"[{commit_type}]"
                )

                # Record full text for boundary merge context
                self._merger.record_commit(merged_text)
                self._stats.record_commit(
                    commit_type.lower(), speech_dur, merged_text, detected_lang,
                    sentence_count=len(sentences),
                )
                if detected_lang:
                    self._recent_langs.append(detected_lang)
                    if len(self._recent_langs) > 10:
                        self._recent_langs.pop(0)

        except Exception as e:
            self._thread_errors[thread_name] = f"{type(e).__name__}: {e}"
            logger.error(
                f"[{thread_name}] CRASHED after {utterance_id} utterances: {e}\n"
                f"{traceback.format_exc()}"
            )
        logger.debug(f"[{thread_name}] Exited ({utterance_id} utterances processed)")

    # ------------------------------------------------------------------
    # Interim worker thread (optional)
    # ------------------------------------------------------------------
    def _interim_worker(self):
        """Dedicated thread for interim (provisional) transcription.

        Drains stale entries before processing -- only the latest audio
        snapshot is transcribed.
        """
        thread_name = "interim-worker"
        logger.debug(f"[{thread_name}] Started, waiting for interim requests...")
        interim_count = 0
        try:
            while not self._stop_event.is_set():
                try:
                    audio = self._interim_queue.get(timeout=1.0)
                except queue.Empty:
                    continue

                # Drain stale entries -- only process the latest
                latest = audio
                drained = 0
                while not self._interim_queue.empty():
                    try:
                        latest = self._interim_queue.get_nowait()
                        drained += 1
                    except queue.Empty:
                        break

                interim_count += 1
                language = self._config.language
                if language == "auto":
                    language = None

                if drained > 0:
                    logger.debug(f"[WHISPER] INTERIM #{interim_count}: drained {drained} stale entries")

                try:
                    segments, info = self._transcribe_fn(
                        latest, language=language,
                        beam_size=self._config.beam_size,
                        initial_prompt=self._config.initial_prompt,
                    )
                except Exception as e:
                    logger.error(f"[WHISPER] INTERIM #{interim_count}: transcribe error: {e}")
                    continue

                if segments is None:
                    continue
                text = " ".join(
                    seg.text.strip() for seg in segments if seg.text.strip()
                )
                if text:
                    self._broadcast_fn("update", text)
                    logger.debug(f"[WHISPER] INTERIM #{interim_count}: \"{text}\"")

        except Exception as e:
            self._thread_errors[thread_name] = f"{type(e).__name__}: {e}"
            logger.error(
                f"[{thread_name}] CRASHED after {interim_count} interims: {e}\n"
                f"{traceback.format_exc()}"
            )
        logger.debug(f"[{thread_name}] Exited ({interim_count} interims processed)")
