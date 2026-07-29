#!/usr/bin/env python3
"""Whisper segmentation A/B: would live-whisper benefit from the Speechmatics
treatment (merge-until-pause + SaT re-split)?

Reproduces the live pipeline OFFLINE (no realtime constraint):
  1. Chunk the sermon audio with the REAL FrameVAD (same thresholds) and the
     live chunker rules (800ms silence commit, 15s max, 400ms preroll).
  2. Transcribe each chunk in isolation with whisper-cli (live conditions:
     no cross-chunk context), beam 7.
  3. Arm A (current pipeline): each chunk's text split by the live
     split_sentences() — these are the units translation receives today.
  4. Arm B (Speechmatics treatment): chunks merged while the inter-chunk gap
     < 1400ms (the grace dial), each merged clause re-split by SaT.
  5. Reference: batch-whisper SRT of the same audio (full context), same
     sentence splitter — the "how a human-ish segmenter reads it" yardstick.

Usage:
  python tools/whisper_sat_ab.py <wav16k> <catalan_srt> <max_seconds>
"""
import json
import os
import re
import statistics
import subprocess
import sys
import wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PUB = os.path.join(ROOT, "EveryTongue", "bin", "Publish")
sys.path.insert(0, os.path.join(ROOT, "live-server"))
os.environ.setdefault("SAT_LIBS", os.path.join(PUB, "sat", "sat-libs"))
os.environ.setdefault("SAT_CACHE", os.path.join(PUB, "sat", "sat-cache"))

import numpy as np  # noqa: E402
from silero_vad import load_silero_vad  # noqa: E402
from vad.frame_vad import FrameVAD  # noqa: E402
from vad.merger import split_sentences  # noqa: E402
import sat_segmenter  # noqa: E402

SR = 16000
FRAME = FrameVAD.SILERO_FRAME_SAMPLES  # 512
SILENCE_COMMIT_S = 0.8
MAX_SEG_S = 15.0
PREROLL_S = 0.4
GRACE_S = 1.4  # the Speechmatics clause-hold grace dial


def read_wav(path):
    with wave.open(path, "rb") as w:
        assert w.getframerate() == SR and w.getnchannels() == 1, "need 16k mono"
        data = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16)
    return data.astype(np.float32) / 32768.0


def chunk_audio(samples):
    """Live-chunker reproduction: FrameVAD hysteresis + silence/max commit."""
    vad = FrameVAD(load_silero_vad(), speech_threshold=0.7,
                   silence_threshold=0.45, speech_confirm_frames=2)
    vad.reset()
    chunks = []  # (start_s, end_s, samples)
    cur_start = None
    silence_frames = 0
    silence_needed = int(SILENCE_COMMIT_S * SR / FRAME)
    preroll = int(PREROLL_S * SR)

    for i in range(0, len(samples) - FRAME, FRAME):
        frame = samples[i:i + FRAME]
        speaking = vad.process_frame(frame)
        if speaking:
            if cur_start is None:
                cur_start = max(0, i - preroll)
            silence_frames = 0
            if (i + FRAME - cur_start) / SR >= MAX_SEG_S:
                chunks.append((cur_start / SR, (i + FRAME) / SR,
                               samples[cur_start:i + FRAME]))
                cur_start = None
        elif cur_start is not None:
            silence_frames += 1
            if silence_frames >= silence_needed:
                end = i + FRAME
                chunks.append((cur_start / SR, end / SR, samples[cur_start:end]))
                cur_start = None
                silence_frames = 0
    if cur_start is not None:
        chunks.append((cur_start / SR, len(samples) / SR, samples[cur_start:]))
    return chunks


def transcribe(chunk_samples, idx, workdir):
    path = os.path.join(workdir, f"chunk{idx:03d}.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((chunk_samples * 32767).astype(np.int16).tobytes())
    exe = os.path.join(PUB, "whisper-cli.exe")
    model = os.path.join(PUB, "ggml-large-v3-turbo.bin")
    subprocess.run([exe, "-m", model, "-l", "ca", "-bs", "7", "-oj", "-f", path],
                   capture_output=True, timeout=300, cwd=PUB)
    jpath = path + ".json"
    if not os.path.exists(jpath):
        return ""
    with open(jpath, encoding="utf-8") as f:
        j = json.load(f)
    text = " ".join(s["text"].strip() for s in j.get("transcription", []))
    return re.sub(r"\s+", " ", text).strip()


def parse_srt(path, max_s):
    text = []
    with open(path, encoding="utf-8-sig") as f:
        blocks = f.read().split("\n\n")
    for b in blocks:
        lines = [l for l in b.strip().splitlines() if l.strip()]
        if len(lines) < 3:
            continue
        m = re.match(r"(\d+):(\d+):(\d+)[,.](\d+)", lines[1])
        if not m:
            continue
        start = int(m[1]) * 3600 + int(m[2]) * 60 + int(m[3]) + int(m[4]) / 1000
        if start > max_s:
            break
        text.append(" ".join(lines[2:]))
    return re.sub(r"\s+", " ", " ".join(text)).strip()


def stats(units, label):
    words = [len(u.split()) for u in units]
    tiny = sum(1 for u in units if len(u) < 20)
    print(f"\n{label}: {len(units)} units, "
          f"mean {statistics.mean(words):.1f} words, median {statistics.median(words):.0f}, "
          f"tiny(<20ch) {100*tiny/len(units):.0f}%")
    for u in units[:6]:
        print(f"   | {u[:110]}")
    return len(units), statistics.mean(words)


def main():
    wav, srt, max_s = sys.argv[1], sys.argv[2], float(sys.argv[3])
    workdir = os.path.join(os.path.dirname(wav), "ab_chunks")
    os.makedirs(workdir, exist_ok=True)

    samples = read_wav(wav)[: int(max_s * SR)]
    print(f"audio: {len(samples)/SR:.0f}s")
    chunks = chunk_audio(samples)
    print(f"VAD chunks: {len(chunks)} "
          f"(mean {statistics.mean([(e-s) for s,e,_ in chunks]):.1f}s)")

    texts = []
    for i, (s, e, aud) in enumerate(chunks):
        t = transcribe(aud, i, workdir)
        texts.append((s, e, t))
        print(f"  [{i+1}/{len(chunks)}] {s:6.1f}-{e:6.1f}s: {t[:80]}")

    texts = [(s, e, t) for s, e, t in texts if t]

    # Arm A — current pipeline: per-chunk text through the live sentence splitter
    arm_a = []
    for _, _, t in texts:
        arm_a.extend(split_sentences(t))

    # Arm B — Speechmatics treatment: merge by grace gap, SaT re-split
    print("\nLoading SaT...")
    ok = sat_segmenter.load()
    print(f"SaT available: {ok}")
    clauses = []
    cur, cur_end = "", None
    for s, e, t in texts:
        if cur and s - cur_end < GRACE_S:
            cur = cur + " " + t
        else:
            if cur:
                clauses.append(cur)
            cur = t
        cur_end = e
    if cur:
        clauses.append(cur)
    print(f"merged clauses: {len(clauses)}")
    arm_b = []
    for c in clauses:
        arm_b.extend(sat_segmenter.segment(c, 0.10))

    ref_text = parse_srt(srt, max_s)
    ref = split_sentences(ref_text)

    print("\n" + "=" * 70)
    na, wa = stats(arm_a, "ARM A — current whisper pipeline (per-chunk sentences)")
    nb, wb = stats(arm_b, "ARM B — Speechmatics treatment (merge-to-pause + SaT)")
    nr, wr = stats(ref, "REFERENCE — batch whisper (full context)")
    print("\n" + "=" * 70)
    print(f"unit-count ratio vs reference:  A={na/nr:.2f}   B={nb/nr:.2f}   (1.00 = ideal)")
    print(f"mean-words ratio vs reference:  A={wa/wr:.2f}   B={wb/wr:.2f}   (1.00 = ideal)")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
