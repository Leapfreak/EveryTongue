"""Utterance state machine for the VAD pipeline.

Two states: IDLE and SPEAKING. Four commit types:
- SOFT-COMMIT: 400ms silence mid-speech, stays SPEAKING (low-latency sentence delivery)
- DURATION-COMMIT: max soft segment (8s) hit during continuous speech, stays SPEAKING
- COMMIT: 750ms silence, transitions to IDLE (definitive pause)
- FORCE-COMMIT: max duration (25s) hit, seamless re-entry to SPEAKING

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

# ── EOU auto-tune (whisper twin of the Speechmatics pace tuner) ──────────────
# The tuning POLICY (rolling window → p85 → bucket, hysteresis, cooldown) lives
# once in pace_tuner.PaceTuner, shared with engines/speechmatics.py. Only the
# ears and hands are ours: pauses are measured from VAD silence runs that ended
# with speech resuming, and bucket changes apply IN PLACE (the thresholds are
# plain numbers this class reads per-frame — no reconnect, so a short cooldown).
#
# The signal differs from Speechmatics' word-timestamp gaps: Silero VAD only
# resolves pauses down to ~2 frames (64ms) and smooths shorter dips, so the
# bucket boundaries are calibrated for VAD-granularity pauses, not word gaps.
PAUSE_FLOOR_S = 0.064        # ignore sub-2-frame dips (VAD jitter, plosives)
IDLE_RESUME_CAP_S = 3.0      # IDLE→speech gaps above this are real utterance breaks
RETUNE_COOLDOWN_S = 20       # in-place retunes are free; cooldown only stops flapping

# (p85 upper bound ms, (soft_commit_ms, silence_commit_ms))
PACE_BUCKETS = [
    (200,  (300,  650)),   # fast reader: barely pauses — commit sooner
    (450,  (400,  800)),   # moderate: the shipped defaults
    (None, (650, 1200)),   # slow, deliberate pauser: don't rip mid-thought
]


class State(enum.Enum):
    IDLE = "idle"
    SPEAKING = "speaking"


class UtteranceStateMachine:
    """Two-state machine driven by the VAD thread. Sole writer to utterance buffer.

    Evaluation order for silence checks:
    1. Force-commit (max duration) -- always fires regardless of silence state
    2. Duration-commit (max soft segment) -- continuous speech too long, stays SPEAKING
    3. Hard commit (750ms) -- takes priority, goes directly to IDLE
    4. Soft commit (400ms) -- only fires in the 400-749ms window
    5. Interim update -- only when no commit is happening
    """

    def __init__(self, preroll, utterance, commit_callback,
                 soft_commit_ms=400, silence_commit_ms=750,
                 max_utterance_s=25, max_soft_utterance_s=8,
                 interim_queue=None, interim_interval_s=3.0,
                 auto_tune=False):
        self.state = State.IDLE
        self._preroll = preroll
        self._utterance = utterance
        self._commit_cb = commit_callback       # (audio, commit_type) -> None
        self._interim_queue = interim_queue      # queue.Queue or None
        self._soft_commit_s = soft_commit_ms / 1000.0
        self._silence_commit_s = silence_commit_ms / 1000.0
        self._max_utterance_s = max_utterance_s
        self._max_soft_utterance_s = max_soft_utterance_s
        self._interim_interval_s = interim_interval_s
        self._last_speech_time = 0.0
        self._utterance_start_time = 0.0
        self._last_interim_time = 0.0
        self._has_speech_since_commit = False    # tracks speech after soft commit
        # EOU auto-tune (see module constants above). Seeded with the configured
        # dials so the tuner stays quiet until the pace genuinely differs.
        self._tuner = None
        if auto_tune:
            from pace_tuner import PaceTuner
            self._tuner = PaceTuner(PACE_BUCKETS, cooldown_s=RETUNE_COOLDOWN_S)
            self._tuner.seed((soft_commit_ms, silence_commit_ms))

    def feed(self, prob, is_speech, frame):
        """Called from VAD thread for every audio frame."""
        now = time.time()

        # EOU auto-tune: measure RESUMED pauses (silence runs that ended with
        # speech coming back — the speaker's own rhythm), and apply a bucket
        # change in place when the tuner calls one.
        if self._tuner is not None:
            if is_speech and self._last_speech_time > 0:
                pause = now - self._last_speech_time
                if self.state == State.SPEAKING:
                    if pause >= PAUSE_FLOOR_S:
                        self._tuner.record(pause * 1000.0)
                elif pause <= IDLE_RESUME_CAP_S:
                    # Short IDLE→speech gap: the hard commit fired but the speaker
                    # was mid-thought — exactly the signal for a longer threshold.
                    self._tuner.record(pause * 1000.0)
            decision = self._tuner.evaluate(now)
            if decision is not None:
                (soft_ms, silence_ms), p85, n = decision
                logger.info(
                    f"[EOU-TUNE] {int(self._soft_commit_s * 1000)}/"
                    f"{int(self._silence_commit_s * 1000)}ms -> "
                    f"{soft_ms}/{silence_ms}ms (soft/commit; p85 pause "
                    f"{int(p85)}ms over {n} resumed pauses)")
                self._soft_commit_s = soft_ms / 1000.0
                self._silence_commit_s = silence_ms / 1000.0

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

            # 2. Duration commit -- continuous speech exceeded max soft segment
            #    Prevents 10+ second utterances from bulk-committing many sentences.
            #    Distinct type: this cut happens WITHOUT a pause (unlike case 4's
            #    SOFT-COMMIT) — the sat_hold clause treatment glues these, and
            #    only pause-backed commits flush the clause.
            if (utterance_duration >= self._max_soft_utterance_s
                    and self._has_speech_since_commit):
                logger.debug(
                    f"[STATE] DURATION-COMMIT "
                    f"({utterance_duration:.1f}s > {self._max_soft_utterance_s}s)"
                )
                audio = self._utterance.get_audio()
                self._utterance.clear()
                self._commit_cb(audio, "SOFT-MAX")
                # Stay SPEAKING -- start fresh with pre-roll
                self._utterance.start(self._preroll.read())
                self._utterance_start_time = now
                self._last_interim_time = now
                self._has_speech_since_commit = False
                return

            # 3. Hard commit -> IDLE (definitive pause)
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

            # 4. Soft commit -- natural sentence pause (stay SPEAKING)
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

            # 5. Interim update -- queue audio snapshot, don't block
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
                          max_utterance_s=None, max_soft_utterance_s=None,
                          interim_interval_s=None):
        """Update tunable parameters at runtime (e.g. from /config endpoint)."""
        if soft_commit_ms is not None:
            self._soft_commit_s = soft_commit_ms / 1000.0
        if silence_commit_ms is not None:
            self._silence_commit_s = silence_commit_ms / 1000.0
        if self._tuner is not None and (soft_commit_ms is not None
                                        or silence_commit_ms is not None):
            # Operator override: re-learn from the new baseline instead of
            # immediately retuning back over it.
            self._tuner.reset()
            self._tuner.seed((int(self._soft_commit_s * 1000),
                              int(self._silence_commit_s * 1000)))
        if max_utterance_s is not None:
            self._max_utterance_s = max_utterance_s
        if max_soft_utterance_s is not None:
            self._max_soft_utterance_s = max_soft_utterance_s
        if interim_interval_s is not None:
            self._interim_interval_s = interim_interval_s
