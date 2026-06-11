"""AccumulatingPipeline — v1.4.1-style audio accumulation.

Replicates the exact architecture that scored 84/100 on the Catalan sermon test:
- ALL audio goes into the buffer (no external VAD filtering)
- Full uncommitted audio is sent to Whisper every ~1s
- Commit decisions use Whisper's own segment end times, not external VAD
- For faster-whisper, vad_filter=True lets Whisper's internal Silero handle segmentation
- For whisper.cpp, Whisper handles speech detection natively

Single capture thread + transcription loop, matching v1.4.1's simplicity.
"""
import logging
import re
import threading
import time
import traceback

import numpy as np
import sounddevice as sd

from .merger import BoundaryMerger

logger = logging.getLogger("live-server")

SAMPLE_RATE = 16000


def _find_sentence_boundary(words, min_time, audio_end):
    """Find the last word that ends a sentence (after min_time seconds).
    Only returns a boundary if it's within 2s of the audio end.
    Returns (text_up_to_boundary, audio_end_time) or (None, None)."""
    if not words:
        return None, None

    last_boundary_idx = -1
    for i, w in enumerate(words):
        word_text = w.word.rstrip() if hasattr(w, 'word') else ""
        word_end = w.end if hasattr(w, 'end') else 0
        if not word_text:
            continue
        if word_end >= min_time and word_text[-1:] in ".!?;" and not word_text.endswith("..."):
            if i + 1 < len(words):
                next_word = words[i + 1].word.lstrip() if hasattr(words[i + 1], 'word') else ""
                if next_word and next_word[0].isupper():
                    last_boundary_idx = i
            else:
                last_boundary_idx = i

    if last_boundary_idx < 0:
        return None, None

    boundary_time = words[last_boundary_idx].end
    if audio_end - boundary_time > 2.0:
        return None, None

    text = "".join(w.word for w in words[:last_boundary_idx + 1]).strip()
    if len(text) < 30:
        return None, None
    return text, boundary_time


def _strip_boundary_overlap(new_text, prev_text, max_overlap_words=4):
    """Remove overlapping words from the start of new_text that match the end of prev_text."""
    if not prev_text or not new_text:
        return new_text
    prev_words = prev_text.lower().split()
    new_words = new_text.split()
    if not prev_words or not new_words:
        return new_text
    for n in range(min(max_overlap_words, len(prev_words), len(new_words)), 0, -1):
        prev_tail = [re.sub(r"[^\w]", "", w) for w in prev_words[-n:]]
        new_head = [re.sub(r"[^\w]", "", w.lower()) for w in new_words[:n]]
        if prev_tail == new_head:
            if n == 1 and len(prev_tail[0]) < 4:
                continue
            remaining = new_words[n:]
            if not remaining:
                continue
            stripped = " ".join(remaining)
            logger.debug(f"  BOUNDARY-DEDUP: stripped {n} overlapping words")
            return stripped
    return new_text


class AccumulatingPipeline:
    """v1.4.1-style accumulating pipeline. Drop-in replacement for VadPipeline."""

    def __init__(self, silero_model, config, transcribe_fn, broadcast_fn,
                 hallucination_fn, stats):
        self._model = silero_model  # not used — Whisper handles VAD internally
        self._config = config
        self._transcribe_fn = transcribe_fn
        self._broadcast_fn = broadcast_fn
        self._hallucination_fn = hallucination_fn
        self._stats = stats
        self._stop_event = threading.Event()
        self._threads = []
        self._stream = None

        # Audio buffer — ALL audio, no filtering
        self._audio_buffer = []
        self._buffer_lock = threading.Lock()

        # Health monitoring
        self._audio_callback_count = 0
        self._audio_callback_errors = 0
        self._thread_errors = {}

    @property
    def stats(self):
        return self._stats

    def start(self):
        cfg = self._config
        logger.info("[ACCUM] Starting accumulating pipeline (v1.4.1 architecture)")

        # Open audio stream — 100ms blocks like v1.4.1
        self._stream = sd.InputStream(
            samplerate=SAMPLE_RATE,
            channels=1,
            dtype="float32",
            blocksize=int(SAMPLE_RATE * 0.1),  # 100ms blocks
            device=cfg.device_index,
            callback=self._audio_callback,
        )

        # Single transcription thread — matches v1.4.1's simplicity
        transcribe_thread = threading.Thread(
            target=self._transcription_thread, name="accum-transcribe", daemon=True
        )
        self._threads = [transcribe_thread]
        transcribe_thread.start()

        self._stream.start()
        logger.info(
            f"[ACCUM] READY: device={cfg.device_index} lang={cfg.language} "
            f"beam={cfg.beam_size} best_of={cfg.best_of} "
            f"silence={cfg.vad_silence_ms}ms max_seg={cfg.vad_max_segment_s}s "
            f"interim_interval={cfg.interim_interval_s}s"
        )

    def stop(self):
        logger.info("[ACCUM] Stopping...")
        self._stop_event.set()
        if self._stream:
            try:
                self._stream.stop()
                self._stream.close()
            except Exception as e:
                logger.warning(f"[ACCUM] Error closing stream: {e}")
            self._stream = None
        for t in self._threads:
            t.join(timeout=5.0)
            if t.is_alive():
                logger.warning(f"[ACCUM] Thread '{t.name}' did not stop within 5s")
        self._threads.clear()
        logger.info(
            f"[ACCUM] Stopped. callbacks={self._audio_callback_count} "
            f"errors={self._audio_callback_errors}"
        )
        if self._stats:
            logger.info("\n" + self._stats.summary())

    def is_alive(self):
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
        if "language" in kwargs:
            self._config.language = kwargs["language"]

    # ------------------------------------------------------------------
    # File-based feeding (for benchmark mode — no microphone)
    # ------------------------------------------------------------------
    def start_from_file(self, audio_array, realtime_factor=0.0, done_event=None):
        """Feed audio from a numpy array instead of a microphone.

        Starts the transcription thread without opening an audio device, then
        feeds chunks into the pipeline at the specified speed.

        Args:
            audio_array: float32 numpy array at 16kHz mono
            realtime_factor: 0.0 = max speed, 1.0 = real-time
            done_event: threading.Event to signal when all audio has been processed
        """
        cfg = self._config
        logger.info(f"[ACCUM] start_from_file: {len(audio_array)/SAMPLE_RATE:.1f}s audio, rt_factor={realtime_factor}")

        # Start transcription thread (no audio stream)
        transcribe_thread = threading.Thread(
            target=self._transcription_thread, name="accum-transcribe", daemon=True
        )
        self._threads = [transcribe_thread]
        transcribe_thread.start()

        # File feeder thread — 100ms blocks like the real pipeline
        chunk_samples = int(SAMPLE_RATE * 0.1)  # 1600 samples = 100ms
        chunk_duration = 0.1

        def _feeder():
            offset = 0
            chunks_fed = 0
            while offset < len(audio_array) and not self._stop_event.is_set():
                end = min(offset + chunk_samples, len(audio_array))
                chunk = audio_array[offset:end]
                if len(chunk) < chunk_samples:
                    chunk = np.pad(chunk, (0, chunk_samples - len(chunk)))
                self._audio_callback(chunk.reshape(-1, 1), chunk_samples, None, None)
                offset = end
                chunks_fed += 1
                if realtime_factor > 0:
                    time.sleep(chunk_duration * realtime_factor)
            logger.info(f"[ACCUM] File feeder done: {chunks_fed} chunks, {offset/SAMPLE_RATE:.1f}s")
            # Grace period for pipeline to flush remaining audio
            time.sleep(5.0)
            self._stop_event.set()
            if done_event:
                done_event.set()

        feeder_thread = threading.Thread(target=_feeder, name="file-feeder", daemon=True)
        feeder_thread.start()
        self._threads.append(feeder_thread)

    # ------------------------------------------------------------------
    # Audio callback — appends ALL audio to buffer (no VAD filtering)
    # ------------------------------------------------------------------
    def _audio_callback(self, indata, frames, time_info, status):
        try:
            if status:
                logger.warning(f"[ACCUM-AUDIO] callback status: {status}")
            samples = indata[:, 0].copy()
            with self._buffer_lock:
                self._audio_buffer.append(samples)
            self._audio_callback_count += 1
        except Exception as e:
            self._audio_callback_errors += 1
            if self._audio_callback_errors <= 5:
                logger.error(f"[ACCUM-AUDIO] callback error #{self._audio_callback_errors}: {e}")

    # ------------------------------------------------------------------
    # Transcription thread — v1.4.1 architecture
    # ------------------------------------------------------------------
    def _transcription_thread(self):
        thread_name = "accum-transcribe"
        logger.debug(f"[{thread_name}] Started")
        cfg = self._config

        last_committed_pos = 0  # samples already committed
        last_commit_text = ""
        consec_hallucinations = 0
        recent_langs = []
        merger = BoundaryMerger(cfg.merge_similarity_threshold)
        commit_count = 0

        try:
            while not self._stop_event.is_set():
                time.sleep(cfg.interim_interval_s)
                if self._stop_event.is_set():
                    break

                # Get current audio
                with self._buffer_lock:
                    if not self._audio_buffer:
                        continue
                    current_audio = np.concatenate(self._audio_buffer)

                total_samples = len(current_audio)
                uncommitted_samples = total_samples - last_committed_pos
                uncommitted_duration = uncommitted_samples / SAMPLE_RATE

                if uncommitted_duration < 1.0:
                    continue

                audio_to_process = current_audio[last_committed_pos:]

                language = cfg.language
                if language == "auto":
                    language = None

                # Transcribe full uncommitted audio — Whisper handles VAD internally
                t0 = time.time()
                try:
                    segments, info = self._transcribe_fn(
                        audio_to_process, language=language,
                        beam_size=cfg.beam_size,
                        best_of=cfg.best_of,
                        initial_prompt=cfg.initial_prompt or "",
                    )
                except Exception as e:
                    logger.error(f"[{thread_name}] transcribe error: {e}\n{traceback.format_exc()}")
                    continue
                inference_dur = time.time() - t0

                if segments is None:
                    logger.debug(f"[{thread_name}] inference returned None after {inference_dur:.1f}s")
                    continue

                if not segments:
                    logger.debug(f"[{thread_name}] no speech in {uncommitted_duration:.1f}s — trimming")
                    with self._buffer_lock:
                        current_full = np.concatenate(self._audio_buffer) if self._audio_buffer else np.array([], dtype=np.float32)
                        keep_samples = int(0.5 * SAMPLE_RATE)
                        remaining = current_full[-keep_samples:] if len(current_full) > keep_samples else current_full
                        self._audio_buffer.clear()
                        self._audio_buffer.append(remaining)
                        last_committed_pos = 0
                    continue

                # Gather words and text
                all_words = []
                for seg in segments:
                    if hasattr(seg, 'words') and seg.words:
                        all_words.extend(seg.words)

                audio_duration = len(audio_to_process) / SAMPLE_RATE
                last_seg_end = segments[-1].end if segments else 0

                # v1.4.1 commit decision: silence at end = audio duration minus last segment end
                silence_at_end = audio_duration - last_seg_end
                vad_silence_s = cfg.vad_silence_ms / 1000.0
                all_final = silence_at_end >= vad_silence_s

                full_text = " ".join(seg.text.strip() for seg in segments if seg.text.strip())
                if not full_text:
                    continue

                detected_lang = info.language if info else ""
                committed_this_loop = False

                logger.debug(
                    f"[{thread_name}] dur={audio_duration:.1f}s segs={len(segments)} "
                    f"last_end={last_seg_end:.1f}s silence_tail={silence_at_end:.2f}s "
                    f"final={all_final} inference={inference_dur:.1f}s"
                )

                if all_final:
                    # Whisper says speech ended — commit everything
                    if not self._hallucination_fn(
                        segments, last_commit_text, detected_lang, recent_langs
                    ):
                        full_text = _strip_boundary_overlap(full_text, last_commit_text)
                        self._broadcast_fn("commit", full_text, lang=detected_lang)
                        logger.info(f"[{thread_name}] COMMIT: \"{full_text}\"")

                        commit_count += 1
                        last_commit_text = full_text[-200:]
                        committed_this_loop = True
                        consec_hallucinations = 0
                        merger.record_commit(full_text)
                        self._stats.record_commit(
                            "vad", uncommitted_duration, full_text, detected_lang,
                            sentence_count=1,
                        )
                        if detected_lang:
                            recent_langs.append(detected_lang)
                            if len(recent_langs) > 10:
                                recent_langs.pop(0)

                        # Cut audio buffer after commit (pad 0.12s past last word)
                        committed_end = last_committed_pos + int((last_seg_end + 0.12) * SAMPLE_RATE)
                        with self._buffer_lock:
                            current_full = np.concatenate(self._audio_buffer) if self._audio_buffer else np.array([], dtype=np.float32)
                            remaining = current_full[committed_end:]
                            self._audio_buffer.clear()
                            if len(remaining) > 0:
                                self._audio_buffer.append(remaining)
                            last_committed_pos = 0
                    else:
                        consec_hallucinations += 1
                        logger.info(
                            f"[{thread_name}] HALLUCINATION #{consec_hallucinations}: "
                            f"\"{full_text[:80]}\""
                        )
                        self._stats.record_hallucination()
                        if consec_hallucinations >= 3:
                            logger.debug(f"[{thread_name}] Cutting buffer after {consec_hallucinations} consecutive hallucinations")
                            with self._buffer_lock:
                                current_full = np.concatenate(self._audio_buffer) if self._audio_buffer else np.array([], dtype=np.float32)
                                committed_end = last_committed_pos + int(last_seg_end * SAMPLE_RATE)
                                remaining = current_full[committed_end:]
                                self._audio_buffer.clear()
                                if len(remaining) > 0:
                                    self._audio_buffer.append(remaining)
                                last_committed_pos = 0
                            consec_hallucinations = 0

                elif uncommitted_duration >= 6.0:
                    # Long audio, speaker still going — try sentence boundary
                    boundary_text, boundary_time = _find_sentence_boundary(
                        all_words, 5.0, audio_duration
                    )
                    if boundary_text and boundary_time:
                        if self._hallucination_fn(
                            segments, last_commit_text, detected_lang, recent_langs
                        ):
                            logger.debug(f"[{thread_name}] SENTENCE-COMMIT blocked (hallucination)")
                            with self._buffer_lock:
                                current_full = np.concatenate(self._audio_buffer) if self._audio_buffer else np.array([], dtype=np.float32)
                                cut_pos = last_committed_pos + int(boundary_time * SAMPLE_RATE)
                                remaining = current_full[cut_pos:]
                                self._audio_buffer.clear()
                                if len(remaining) > 0:
                                    self._audio_buffer.append(remaining)
                                last_committed_pos = 0
                        else:
                            boundary_text = _strip_boundary_overlap(boundary_text, last_commit_text)
                            self._broadcast_fn("commit", boundary_text, lang=detected_lang)
                            logger.info(
                                f"[{thread_name}] SENTENCE-COMMIT @{boundary_time:.1f}s: "
                                f"\"{boundary_text}\""
                            )

                            commit_count += 1
                            last_commit_text = boundary_text[-200:]
                            committed_this_loop = True
                            consec_hallucinations = 0
                            merger.record_commit(boundary_text)
                            self._stats.record_commit(
                                "sentence", boundary_time, boundary_text, detected_lang,
                                sentence_count=1,
                            )
                            if detected_lang:
                                recent_langs.append(detected_lang)
                                if len(recent_langs) > 10:
                                    recent_langs.pop(0)

                            # Cut audio at sentence boundary
                            cut_pos = last_committed_pos + int((boundary_time + 0.12) * SAMPLE_RATE)
                            with self._buffer_lock:
                                current_full = np.concatenate(self._audio_buffer) if self._audio_buffer else np.array([], dtype=np.float32)
                                remaining = current_full[cut_pos:]
                                self._audio_buffer.clear()
                                if len(remaining) > 0:
                                    self._audio_buffer.append(remaining)
                                last_committed_pos = 0
                    else:
                        # No sentence boundary — emit interim update
                        if not self._hallucination_fn(
                            segments, last_commit_text, detected_lang, recent_langs
                        ):
                            self._broadcast_fn("update", full_text)
                            logger.debug(f"[{thread_name}] UPDATE: \"{full_text[:80]}\"")

                else:
                    # Short audio — interim update (skip hallucinations)
                    if not self._hallucination_fn(
                        segments, last_commit_text, detected_lang, recent_langs
                    ):
                        self._broadcast_fn("update", full_text)
                        logger.debug(f"[{thread_name}] UPDATE: \"{full_text[:80]}\"")
                    else:
                        self._stats.record_hallucination()

                # Force-commit on max duration
                if uncommitted_duration >= cfg.vad_max_segment_s and not committed_this_loop:
                    if full_text and not self._hallucination_fn(
                        segments, last_commit_text, detected_lang, recent_langs
                    ):
                        full_text = _strip_boundary_overlap(full_text, last_commit_text)
                        self._broadcast_fn("commit", full_text, lang=detected_lang)
                        logger.info(f"[{thread_name}] FORCE-COMMIT: \"{full_text}\"")

                        commit_count += 1
                        last_commit_text = full_text[-200:]
                        consec_hallucinations = 0
                        merger.record_commit(full_text)
                        self._stats.record_commit(
                            "force", uncommitted_duration, full_text, detected_lang,
                            sentence_count=1,
                        )
                        if detected_lang:
                            recent_langs.append(detected_lang)
                            if len(recent_langs) > 10:
                                recent_langs.pop(0)

                    # Always cut buffer at max segment
                    with self._buffer_lock:
                        current_full = np.concatenate(self._audio_buffer) if self._audio_buffer else np.array([], dtype=np.float32)
                        committed_end = last_committed_pos + int(last_seg_end * SAMPLE_RATE)
                        remaining = current_full[committed_end:]
                        self._audio_buffer.clear()
                        if len(remaining) > 0:
                            self._audio_buffer.append(remaining)
                        last_committed_pos = 0

        except Exception as e:
            self._thread_errors[thread_name] = f"{type(e).__name__}: {e}"
            logger.error(f"[{thread_name}] CRASHED: {e}\n{traceback.format_exc()}")
        logger.debug(f"[{thread_name}] Exited ({commit_count} commits)")
