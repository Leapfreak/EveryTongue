"""Audio buffers for the VAD pipeline.

PrerollBuffer — fixed-size circular buffer for pre-roll audio capture.
UtteranceBuffer — growable buffer for the current utterance (single-writer: VAD thread).
"""
import numpy as np

SAMPLE_RATE = 16000


class PrerollBuffer:
    """Fixed-size circular buffer for pre-roll audio.

    Continuously filled by the sounddevice callback regardless of VAD state.
    When transitioning IDLE->SPEAKING, the VAD thread snapshots the contents
    into the utterance buffer to capture speech onset.

    Thread safety: single writer (audio callback), single reader (VAD thread
    on transition). Worst case of concurrent access is slightly stale pre-roll,
    which is acceptable for a 400ms window.
    """

    def __init__(self, duration_ms=400, sample_rate=SAMPLE_RATE):
        self._size = int(duration_ms / 1000 * sample_rate)
        self._buf = np.zeros(self._size, dtype=np.float32)
        self._pos = 0
        self._filled = False

    def write(self, samples):
        """Append samples to the ring buffer (called from audio callback)."""
        n = len(samples)
        if n >= self._size:
            # Frame larger than buffer -- just keep the tail
            self._buf[:] = samples[-self._size:]
            self._pos = 0
            self._filled = True
            return
        end = self._pos + n
        if end <= self._size:
            self._buf[self._pos:end] = samples
        else:
            first = self._size - self._pos
            self._buf[self._pos:] = samples[:first]
            self._buf[:n - first] = samples[first:]
            self._filled = True
        self._pos = end % self._size
        if end >= self._size:
            self._filled = True

    def read(self):
        """Return the full pre-roll contents in chronological order (called from VAD thread)."""
        if not self._filled:
            return self._buf[:self._pos].copy()
        return np.concatenate([self._buf[self._pos:], self._buf[:self._pos]]).copy()


class UtteranceBuffer:
    """Growable buffer for the current utterance's audio.

    Single-writer (VAD thread only). Initialized with pre-roll snapshot when
    entering SPEAKING state. Audio appended frame-by-frame by the VAD thread.
    """

    def __init__(self, sample_rate=SAMPLE_RATE):
        self._chunks = []
        self._sample_rate = sample_rate

    def start(self, preroll):
        """Begin a new utterance with pre-roll audio."""
        self._chunks = [preroll.copy()]

    def append(self, samples):
        """Append a frame of audio (VAD thread only)."""
        self._chunks.append(samples)

    def get_audio(self):
        """Return the full utterance audio as a contiguous array."""
        if self._chunks:
            return np.concatenate(self._chunks)
        return np.array([], dtype=np.float32)

    def duration_s(self):
        """Return the current utterance duration in seconds."""
        return sum(len(c) for c in self._chunks) / self._sample_rate

    def clear(self):
        """Clear the buffer."""
        self._chunks.clear()
