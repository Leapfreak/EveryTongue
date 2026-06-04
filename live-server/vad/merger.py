"""Boundary merger and sentence splitter for the VAD pipeline.

BoundaryMerger -- deduplicates text overlap at FORCE-COMMIT boundaries only.
split_sentences -- splits transcribed text into sentences for per-sentence commits.
"""
import logging
import re
from difflib import SequenceMatcher

logger = logging.getLogger("live-server")


def split_sentences(text):
    """Split transcribed text into sentences for per-sentence commit.

    Only splits on strong sentence boundaries: '. ', '? ', '! ' followed by
    uppercase letter. Does NOT split on abbreviations, ellipsis, or mid-sentence
    periods (e.g. "Dr. Smith", "U.S.A.", "3.14").

    For non-Latin scripts (Chinese, Japanese, Arabic, etc.) where there's no
    uppercase, the regex won't match and the full text is returned as one item.
    This is acceptable -- sentence splitting is best-effort, not a correctness
    requirement.
    """
    if not text or not text.strip():
        return []
    parts = re.split(r'(?<=[.!?])\s+(?=[A-Z])', text.strip())
    return [p.strip() for p in parts if p.strip()]


class BoundaryMerger:
    """Deduplicates overlap between adjacent utterances at the boundary only.

    ONLY active for FORCE-COMMIT utterances (where ~400ms audio overlap exists
    from the pre-roll of the seamless re-entry). For SOFT-COMMIT and COMMIT
    utterances (both silence-based), no audio overlap exists, so the merger is
    skipped entirely to preserve intentional spoken repetitions like "thank you,
    thank you".

    Uses fuzzy matching (SequenceMatcher) because Whisper can transcribe
    identical audio differently across inference runs.
    """

    # 400ms of audio at ~2.5 words/sec = 1-2 words of actual overlap.
    # Window of 3 gives margin for Whisper adding/removing filler words.
    FORCE_COMMIT_MAX_WORDS = 3

    def __init__(self, similarity_threshold=0.75):
        self._sim_threshold = similarity_threshold
        self.last_commit_text = ""

    def merge(self, new_text, commit_type):
        """Remove overlapping words from the start of new_text that
        fuzzy-match the end of the previous commit.

        Only runs for FORCE-COMMIT (audio overlap exists).
        For SOFT-COMMIT and COMMIT (silence-based, no overlap), returns
        new_text unchanged.

        Returns (merged_text, did_strip) tuple.
        """
        if commit_type != "FORCE-COMMIT":
            return new_text, False

        if not self.last_commit_text or not new_text:
            return new_text, False

        prev_words = self.last_commit_text.split()
        new_words = new_text.split()
        if not prev_words or not new_words:
            return new_text, False

        # Tight window -- only strip words proportional to actual audio overlap
        search_n = min(self.FORCE_COMMIT_MAX_WORDS, len(prev_words), len(new_words))

        for n in range(search_n, 0, -1):
            prev_tail = " ".join(prev_words[-n:]).lower()
            new_head = " ".join(new_words[:n]).lower()

            # Strip punctuation for comparison
            prev_clean = re.sub(r"[^\w\s]", "", prev_tail)
            new_clean = re.sub(r"[^\w\s]", "", new_head)

            ratio = SequenceMatcher(None, prev_clean, new_clean).ratio()
            if ratio >= self._sim_threshold:
                # For single-word overlap, require 4+ chars to avoid false matches
                if n == 1 and len(prev_clean.strip()) < 4:
                    continue
                remaining = new_words[n:]
                if not remaining:
                    continue
                stripped = " ".join(remaining)
                logger.debug(
                    f"[MERGE] FORCE-COMMIT: stripped {n} words (sim={ratio:.2f}): "
                    f"{' '.join(new_words[:n])}"
                )
                return stripped, True

        return new_text, False

    def record_commit(self, text):
        """Record committed text for next merge comparison."""
        self.last_commit_text = text[-300:] if text else ""
