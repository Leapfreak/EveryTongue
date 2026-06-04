"""Transcript segment model and session statistics.

TranscriptSegment -- dataclass for a single committed utterance.
SessionStats -- session-level telemetry for the /stats endpoint.
"""
import dataclasses
import time


@dataclasses.dataclass
class TranscriptSegment:
    """A single committed utterance with metadata."""
    utterance_id: int
    start_time: float       # wall-clock seconds since session start
    end_time: float
    text: str
    language: str
    commit_type: str        # "SOFT-COMMIT" | "COMMIT" | "FORCE-COMMIT"
    inference_duration_s: float
    audio_duration_s: float


class SessionStats:
    """Session statistics for the VAD pipeline.

    Tracks commit counts by type, speaking rate, silence gaps, and other
    telemetry. Accessible via the /stats endpoint.
    """

    def __init__(self):
        self.start_time = time.time()
        self.commits = []          # list of {type, duration, chars, words, lang, time, sentences}
        self.hallucinations = 0
        self.silence_gaps = []     # seconds between end of one commit and start of next
        self.dropped_count = 0     # utterances dropped due to full transcribe_queue
        self.merge_strip_count = 0 # times boundary merger stripped words (force-commits)
        self._last_commit_time = None

    def record_commit(self, commit_type, duration, text, lang, sentence_count=1):
        """Record a committed utterance."""
        now = time.time()
        words = len(text.split())
        entry = {
            "type": commit_type,
            "duration": duration,
            "chars": len(text),
            "words": words,
            "wps": words / duration if duration > 0.5 else 0,
            "lang": lang,
            "time": now,
            "sentences": sentence_count,
        }
        self.commits.append(entry)
        if self._last_commit_time is not None:
            gap = now - self._last_commit_time
            if gap < 30:  # ignore long pauses (breaks, not speech gaps)
                self.silence_gaps.append(gap)
        self._last_commit_time = now

    def record_hallucination(self):
        self.hallucinations += 1

    def record_drop(self):
        self.dropped_count += 1

    def record_merge_strip(self):
        self.merge_strip_count += 1

    def to_dict(self):
        """Return stats as a JSON-serializable dict for the /stats endpoint."""
        if not self.commits:
            return {"elapsed": time.time() - self.start_time, "commits": 0}

        n = len(self.commits)
        durations = [c["duration"] for c in self.commits]
        word_counts = [c["words"] for c in self.commits]
        wps_vals = [c["wps"] for c in self.commits if c["wps"] > 0]
        char_counts = [c["chars"] for c in self.commits]

        types = {}
        for c in self.commits:
            types[c["type"]] = types.get(c["type"], 0) + 1

        langs = {}
        for c in self.commits:
            lang = c["lang"] or "unknown"
            langs[lang] = langs.get(lang, 0) + 1

        total_sentences = sum(c.get("sentences", 1) for c in self.commits)
        multi_sentence = sum(1 for c in self.commits if c.get("sentences", 1) >= 2)

        result = {
            "elapsed": time.time() - self.start_time,
            "commits": n,
            "hallucinations": self.hallucinations,
            "dropped": self.dropped_count,
            "merge_strips": self.merge_strip_count,
            "commit_types": types,
            "languages": langs,
            "total_sentences": total_sentences,
            "multi_sentence_utterances": multi_sentence,
            "duration": {
                "min": round(min(durations), 1),
                "max": round(max(durations), 1),
                "avg": round(sum(durations) / n, 1),
                "median": round(sorted(durations)[n // 2], 1),
            },
            "words": {
                "min": min(word_counts),
                "max": max(word_counts),
                "avg": round(sum(word_counts) / n),
                "median": sorted(word_counts)[n // 2],
            },
            "chars": {
                "min": min(char_counts),
                "max": max(char_counts),
                "avg": round(sum(char_counts) / n),
            },
        }
        if wps_vals:
            result["wps"] = {
                "min": round(min(wps_vals), 1),
                "max": round(max(wps_vals), 1),
                "avg": round(sum(wps_vals) / len(wps_vals), 1),
            }
        if self.silence_gaps:
            gaps = self.silence_gaps
            gn = len(gaps)
            result["silence_gaps"] = {
                "min": round(min(gaps), 1),
                "max": round(max(gaps), 1),
                "avg": round(sum(gaps) / gn, 1),
                "median": round(sorted(gaps)[gn // 2], 1),
            }
        short_count = sum(1 for d in durations if d < 3)
        result["short_segment_ratio"] = round(short_count / n, 2)
        force_count = types.get("force-commit", 0)
        result["force_commit_ratio"] = round(force_count / n, 2)

        return result

    def summary(self):
        """Return a human-readable session summary for logging."""
        if not self.commits:
            return "SESSION STATS: no commits recorded"

        elapsed = time.time() - self.start_time
        n = len(self.commits)
        durations = [c["duration"] for c in self.commits]
        word_counts = [c["words"] for c in self.commits]
        wps_vals = [c["wps"] for c in self.commits if c["wps"] > 0]
        char_counts = [c["chars"] for c in self.commits]

        types = {}
        for c in self.commits:
            types[c["type"]] = types.get(c["type"], 0) + 1

        langs = {}
        for c in self.commits:
            lang = c["lang"] or "unknown"
            langs[lang] = langs.get(lang, 0) + 1

        lines = [
            "=" * 60,
            f"SESSION STATS  ({elapsed/60:.0f}m {elapsed%60:.0f}s total)",
            f"  Commits: {n}  |  Hallucinations: {self.hallucinations}  |  Dropped: {self.dropped_count}",
            f"  Commit types: {', '.join(f'{k}={v}' for k, v in sorted(types.items()))}",
            f"  Languages: {', '.join(f'{k}={v}' for k, v in sorted(langs.items()))}",
            "",
            "  Segment duration (audio seconds per commit):",
            f"    min={min(durations):.1f}s  max={max(durations):.1f}s  "
            f"avg={sum(durations)/n:.1f}s  median={sorted(durations)[n//2]:.1f}s",
            "",
            "  Words per commit:",
            f"    min={min(word_counts)}  max={max(word_counts)}  "
            f"avg={sum(word_counts)/n:.0f}  median={sorted(word_counts)[n//2]}",
            "",
            "  Characters per commit:",
            f"    min={min(char_counts)}  max={max(char_counts)}  "
            f"avg={sum(char_counts)/n:.0f}",
        ]

        if wps_vals:
            lines += [
                "",
                "  Speaking rate (words/sec):",
                f"    min={min(wps_vals):.1f}  max={max(wps_vals):.1f}  "
                f"avg={sum(wps_vals)/len(wps_vals):.1f}",
            ]

        if self.silence_gaps:
            gaps = self.silence_gaps
            gn = len(gaps)
            lines += [
                "",
                "  Silence gaps between commits:",
                f"    min={min(gaps):.1f}s  max={max(gaps):.1f}s  "
                f"avg={sum(gaps)/gn:.1f}s  median={sorted(gaps)[gn//2]:.1f}s",
            ]

        buckets = {"<2s": 0, "2-5s": 0, "5-10s": 0, "10-20s": 0, ">20s": 0}
        for d in durations:
            if d < 2:
                buckets["<2s"] += 1
            elif d < 5:
                buckets["2-5s"] += 1
            elif d < 10:
                buckets["5-10s"] += 1
            elif d < 20:
                buckets["10-20s"] += 1
            else:
                buckets[">20s"] += 1
        lines += [
            "",
            "  Duration distribution:",
            f"    {', '.join(f'{k}={v}' for k, v in buckets.items())}",
            "=" * 60,
        ]

        return "\n".join(lines)
