"""Frame-level Silero VAD with hysteresis.

Processes one 512-sample audio frame at a time (32ms at 16kHz) and returns
a speech probability and a boolean is_speech flag with hysteresis.
"""
import logging

import numpy as np
import torch

logger = logging.getLogger("live-server")


class FrameVAD:
    """Runs Silero VAD frame-by-frame with hysteresis.

    Hysteresis design:
    - Speech onset: probability >= speech_threshold for speech_confirm_frames
      consecutive frames (~64ms at 32ms/frame) before is_speech becomes True.
    - Speech offset: probability < silence_threshold sets is_speech to False
      immediately. The state machine has its own time-based thresholds (400ms /
      750ms) for commit decisions, so no frame-count confirmation is needed here.
    - Hysteresis band (silence_threshold..speech_threshold): maintain current
      is_speech state, prevents rapid toggling on borderline audio.
    """

    SILERO_FRAME_SAMPLES = 512  # 32ms at 16kHz -- required by Silero v5

    def __init__(self, model, sample_rate=16000,
                 speech_threshold=0.6, silence_threshold=0.4,
                 speech_confirm_frames=2):
        self._model = model
        self._sr = sample_rate
        self._speech_thresh = speech_threshold
        self._silence_thresh = silence_threshold
        self._confirm_frames = speech_confirm_frames
        self._consec_speech = 0
        self._consec_silence = 0
        self._is_speech = False

    def process_frame(self, frame):
        """Process one 512-sample audio frame. Returns (probability, is_speech).

        Frame MUST be exactly 512 samples (32ms at 16kHz) for Silero v5.
        If the frame size is wrong, returns (0.0, current_is_speech) and logs a warning.
        """
        if len(frame) != self.SILERO_FRAME_SAMPLES:
            logger.warning(
                f"[VAD] Unexpected frame size {len(frame)}, "
                f"expected {self.SILERO_FRAME_SAMPLES} -- skipping"
            )
            return 0.0, self._is_speech

        tensor = torch.from_numpy(frame)
        prob = self._model(tensor, self._sr).item()

        if prob >= self._speech_thresh:
            self._consec_speech += 1
            self._consec_silence = 0
            if self._consec_speech >= self._confirm_frames:
                self._is_speech = True
        elif prob < self._silence_thresh:
            self._consec_silence += 1
            self._consec_speech = 0
            # Immediate offset -- state machine has time-based thresholds for commits
            self._is_speech = False
        else:
            # In hysteresis band -- maintain current state, reset counters
            self._consec_speech = 0
            self._consec_silence = 0

        return prob, self._is_speech

    def reset(self):
        """Reset state for a new session."""
        self._consec_speech = 0
        self._consec_silence = 0
        self._is_speech = False
        self._model.reset_states()
