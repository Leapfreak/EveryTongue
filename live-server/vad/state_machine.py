"""Utterance state machine for the VAD pipeline.

Two states: IDLE and SPEAKING. Three commit types:
- SOFT-COMMIT: 400ms silence mid-speech, stays SPEAKING (low-latency sentence delivery)
- COMMIT: 750ms silence, transitions to IDLE (definitive pause)
- FORCE-COMMIT: max duration hit, seamless re-entry to SPEAKING

The VAD thread is the sole writer to the utterance buffer, eliminating
cross-thread race conditions.
"""
import enum
import logging
import queue
import time

import numpy as np

from .buffers import PrerollBuffer, UtteranceBuffer

logger = logging.getLogger("live-server")


class State(enum.Enum):
    IDLE = "idle"
    SPEAKING = "speaking"


class UtteranceStateMachine:
    """Two-state machine driven by the VAD thread. Sole writer to utterance buffer.

    Evaluation order for silence checks:
    1. Force-commit (max duration) -- always fires regardless of silence state
    2. Hard commit (750ms) -- takes priority, goes directly to IDLE
    3. Soft commit (400ms) -- only fires in the 400-749ms window
    4. Interim update -- only when no commit is happening
    """

    def __init__(self, preroll, utterance, commit_callback,
                 soft_commit_ms=400, silence_commit_ms=750,
                 max_utterance_s=25,
                 interim_queue=None, interim_interval_s=3.0):
        self.state = State.IDLE
        self._preroll = preroll
        self._utterance = utterance
        self._commit_cb = commit_callback       # (audio, commit_type) -> None
        self._interim_queue = interim_queue      # queue.Queue or None
        self._soft_commit_s = soft_commit_ms / 1000.0
        self._silence_commit_s = silence_commit_ms / 1000.0
        self._max_utterance_s = max_utterance_s
        self._interim_interval_s = interim_interval_s
        self._last_speech_time = 0.0
        self._utterance_start_time = 0.0
        self._last_interim_time = 0.0
        self._has_speech_since_commit = False    # tracks speech after soft commit

    def feed(self, prob, is_speech, frame):
        """Called from VAD thread for every audio frame."""
        now = time.time()

        if self.state == State.IDLE:
            if is_speech:
                # Transition IDLE -> SPEAKING: grab pre-roll
                self._utterance.start(self._preroll.read())
                self._utterance.append(frame)
                self._last_speech_time = now
                self._utterance_start_time = now
                self._last_interim_time = now
                self._has_speech_since_commit = True
                self.state = State.SPEAKING
                logger.debug(f"[STATE] IDLE -> SPEAKING (prob={prob:.2f})")

        elif self.state == State.SPEAKING:
            # VAD thread is the sole writer -- always append
            self._utterance.append(frame)

            if is_speech:
                self._last_speech_time = now
                self._has_speech_since_commit = True

            silence_duration = now - self._last_speech_time
            utterance_duration = self._utterance.duration_s()

            # 1. Force commit on max duration -- seamless re-entry
            if utterance_duration >= self._max_utterance_s:
                logger.debug(f"[STATE] FORCE-COMMIT ({utterance_duration:.1f}s)")
                self._force_commit()
                return

            # 2. Hard commit -> IDLE (definitive pause)
            if silence_duration >= self._silence_commit_s:
                if self._has_speech_since_commit:
                    logger.debug(
                        f"[STATE] SPEAKING -> IDLE "
                        f"(silence={silence_duration:.2f}s, duration={utterance_duration:.1f}s)"
                    )
                    audio = self._utterance.get_audio()
                    self._utterance.clear()
                    self._commit_cb(audio, "COMMIT")
                else:
                    # No speech since last soft commit -- just discard silence buffer
                    logger.debug("[STATE] SPEAKING -> IDLE (silence-only, discarded)")
                    self._utterance.clear()
                self.state = State.IDLE
                self._has_speech_since_commit = False
                return

            # 3. Soft commit -- natural sentence pause (stay SPEAKING)
            if (silence_duration >= self._soft_commit_s
                    and self._has_speech_since_commit
                    and utterance_duration >= 1.0):
                logger.debug(
                    f"[STATE] SOFT-COMMIT "
                    f"(silence={silence_duration:.2f}s, duration={utterance_duration:.1f}s)"
                )
                audio = self._utterance.get_audio()
                self._utterance.clear()
                self._commit_cb(audio, "SOFT-COMMIT")
                # Stay SPEAKING -- start fresh with pre-roll
                self._utterance.start(self._preroll.read())
                self._utterance_start_time = now
                self._last_interim_time = now
                self._has_speech_since_commit = False
                return

            # 4. Interim update -- queue audio snapshot, don't block
            if (self._interim_queue is not None
                    and utterance_duration >= 2.0
                    and (now - self._last_interim_time) >= self._interim_interval_s):
                try:
                    self._interim_queue.put_nowait(self._utterance.get_audio())
                except queue.Full:
                    pass  # skip interim rather than block VAD thread
                self._last_interim_time = now

    def _force_commit(self):
        """Force-commit without losing speech continuity."""
        audio = self._utterance.get_audio()
        self._utterance.clear()
        self._commit_cb(audio, "FORCE-COMMIT")
        # Immediately start new utterance with fresh pre-roll
        self._utterance.start(self._preroll.read())
        self._utterance_start_time = time.time()
        self._last_interim_time = time.time()
        self._has_speech_since_commit = True  # speaker is still talking
        logger.debug("[STATE] FORCE-COMMIT -> SPEAKING (seamless re-entry)")

    def update_thresholds(self, soft_commit_ms=None, silence_commit_ms=None,
                          max_utterance_s=None, interim_interval_s=None):
        """Update tunable parameters at runtime (e.g. from /config endpoint)."""
        if soft_commit_ms is not None:
            self._soft_commit_s = soft_commit_ms / 1000.0
        if silence_commit_ms is not None:
            self._silence_commit_s = silence_commit_ms / 1000.0
        if max_utterance_s is not None:
            self._max_utterance_s = max_utterance_s
        if interim_interval_s is not None:
            self._interim_interval_s = interim_interval_s
