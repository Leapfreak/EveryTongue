"""Shared speaker-pace tuner — the ONE brain behind every EOU auto-tune.

Policy (extracted from engines/speechmatics.py, 2026-07-29): keep a rolling
window of the speaker's pause lengths, take the 85th percentile, map it to a
threshold bucket, and only change buckets when the p85 CLEARS the boundary by
an asymmetric hysteresis margin (going longer is easy, going shorter is hard —
a borderline speaker settles on the longer/safer side) and a cooldown has
elapsed.

What differs per engine is only the ears and the hands, so those stay with the
caller:
- engines/speechmatics.py hears pauses via word timestamps and applies a bucket
  change with a WS reconnect (hence its long cooldown).
- vad/state_machine.py (whisper family) hears pauses via VAD silence runs and
  applies changes in place (plain numbers read per-frame).

Buckets are a list of (upper_bound_p85_ms, payload) with the last bound None
(open-ended). The payload is opaque to the tuner — a float EOU for
Speechmatics, a (soft_ms, silence_ms) tuple for whisper.
"""
import collections


def percentile(values, p):
    """Linear-interpolated p-th percentile of an iterable of numbers (0 if empty)."""
    s = sorted(values)
    if not s:
        return 0.0
    k = (len(s) - 1) * (p / 100.0)
    f = int(k)
    c = min(f + 1, len(s) - 1)
    return s[f] + (s[c] - s[f]) * (k - f)


class PaceTuner:
    def __init__(self, buckets, *, window=200, min_samples=40,
                 check_interval_s=10.0, cooldown_s=45.0,
                 hyst_up=0.10, hyst_down=0.25):
        self._buckets = list(buckets)
        self._gaps = collections.deque(maxlen=window)
        self._min_samples = min_samples
        self._check_interval = check_interval_s
        self._cooldown = cooldown_s
        self._hyst_up = hyst_up
        self._hyst_down = hyst_down
        self._bucket = None       # current bucket index; None = not yet settled
        self._last_check = 0.0
        self._last_retune = 0.0

    @staticmethod
    def _payload_matches(a, b):
        # Numeric payloads get a small tolerance (config baselines are floats,
        # e.g. an EOU of 1.0 must match the 1.0 bucket despite representation).
        if isinstance(a, (int, float)) and isinstance(b, (int, float)):
            return abs(a - b) < 0.05
        return a == b

    def seed(self, payload):
        """Adopt the bucket whose payload matches the caller's starting value, so
        the tuner doesn't announce a 'change' to what is already in effect. A
        custom starting value matching no bucket leaves the tuner unseeded: the
        first confident measurement then applies without hysteresis (there is no
        boundary to defend) — same behaviour the Speechmatics tuner always had."""
        for i, (_, p) in enumerate(self._buckets):
            if self._payload_matches(p, payload):
                self._bucket = i
                return

    def record(self, gap_ms):
        if gap_ms > 0:
            self._gaps.append(gap_ms)

    def reset(self):
        """Pace re-measure (host pause, language change, new speaker): clear the
        window, forget the settled bucket, lift the cooldown."""
        self._gaps.clear()
        self._bucket = None
        self._last_retune = 0.0

    @property
    def sample_count(self):
        return len(self._gaps)

    def evaluate(self, now):
        """Return (payload, p85_ms, samples) when the thresholds should change,
        else None. Self-limits to one real check per check_interval."""
        if (now - self._last_check) < self._check_interval:
            return None
        self._last_check = now
        if len(self._gaps) < self._min_samples:
            return None
        p85 = percentile(self._gaps, 85)
        target = len(self._buckets) - 1
        for i, (bound, _) in enumerate(self._buckets):
            if bound is not None and p85 < bound:
                target = i
                break
        if target == self._bucket:
            return None
        if (now - self._last_retune) < self._cooldown:
            return None
        # Boundary hysteresis (2026-07-12, Speechmatics field finding): a p85
        # sitting ON a boundary ping-pongs buckets every window. Require it to
        # CLEAR the boundary adjacent to the target — 10% to go longer, 25% to
        # go shorter, so borderline speakers settle on the longer (safer) side.
        if self._bucket is not None:
            if target > self._bucket:
                boundary = self._buckets[target - 1][0]
                if p85 <= boundary * (1 + self._hyst_up):
                    return None
            else:
                boundary = self._buckets[target][0]
                if p85 >= boundary * (1 - self._hyst_down):
                    return None
        self._bucket = target
        self._last_retune = now
        return (self._buckets[target][1], p85, len(self._gaps))
