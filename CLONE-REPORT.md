# Duplicate Code Report

*Generated 2026-07-30 from `audit-clones` findings (25 raw blocks), each block read
and grouped by hand. The raw tool output overstates the problem — overlapping
detection windows count the same duplication several times. The truth is **six
distinct duplications**, listed here from biggest to smallest.*

**How to read the "Risk" column:** it's the risk of *doing the extraction*, not
of leaving it. Leaving any of these costs the same thing: a bug fixed in one
copy silently survives in the others.

---

## Group 1 — PipelineRunner: the same pipeline written three times
**Where:** `EveryTongue.Core/Pipeline/PipelineRunner.vb` (accounts for ~11 of the 25 raw findings)

The batch Transcribe workspace supports three input modes (YouTube URL, local
video file, audio file). Each mode has its own Run method, and each re-implements
the same steps by copy-paste:

- the "check every tool exists" ladder (ffmpeg, ffprobe, whisper-cli, model) — 3 copies
- the download / trim / extract-audio steps with their SKIP-if-exists logic — 3 copies
- the ffmpeg chunking loop (5-minute WAV chunks at 16 kHz) — 2 copies

**Why it matters:** this is the file where a chunking bug fixed for audio-file
mode would silently stay broken for YouTube mode. Three parallel copies of
process-spawning code is also three places to keep the pipe-drain rule right.

**The fix:** extract step helpers — `ValidateTools()`, `DownloadStep()`,
`TrimStep()`, `ChunkStep()` — and have the three Run methods compose them.

**Effort:** medium (half a day incl. testing all three modes with a local file).
**Risk:** low-medium — the Transcribe workspace is offline/batch, NOT part of the
Sunday live path, and every mode is testable on the dev machine.

---

## Group 2 — The whisper backend twins
**Where:** `FasterWhisperBackend.vb` ↔ `WhisperCppBackend.vb`, partly shared with
`CloudStreamingSttBackend.vb` (~6 raw findings)

All three STT backends are thin adapters around the same `LiveStreamRunner`, and
the adapter boilerplate is copy-pasted: the delegating properties (IsRunning,
Transcript), the four events, the Start() forwarding — including the
`SatHold`/`EouAutoTune` lines added this very week, which went into BOTH files
because the pattern demanded it — plus Segment, UpdateConfigAsync, and the
device-list parsing.

**Why it matters:** this is the duplication that actively grows. Every new
whisper capability (this week alone: web-mic, clause hold, pace tuner) gets
pasted twice. The capability matrix exists precisely because this pattern makes
gaps invisible.

**The fix:** a shared base class (e.g. `RunnerBackedSttBackend`) owning the
runner and all forwarding; each backend keeps only its engine-specific config
assembly (~30 lines each).

**Effort:** small-medium (mechanical, but touches the hot path).
**Risk:** the highest here — this IS the Sunday whisper path. **Do after Sunday.**

---

## Group 3 — ServerOptions ↔ ConversationAudioHandler option mirror
**Where:** `Server/ServerOptions.vb:68-101` ↔ `Services/Rooms/ConversationAudioHandler.vb:43-76`

The same ~12 whisper/server settings (model path, compute type, server path/port,
beam, best-of, API key, Silero path…) are declared as properties in BOTH classes
— one is the Kestrel DI options object, the other keeps its own copy.

**Why it matters:** this is *config drift by design*: adding one new whisper
option means remembering two declaration sites plus the plumbing between them.
Exactly the "was this updated everywhere?" class of problem.

**The fix:** ConversationAudioHandler consumes `ServerOptions` (or a shared
`WhisperSessionOptions` block) instead of mirroring it.

**Effort:** small. **Risk:** medium — conversation rooms path; verify with a
room session. Post-Sunday.

---

## Group 4 — Concurrency runners' statistics math
**Where:** `SttConcurrencyRunner.vb` ↔ `TranslationConcurrencyRunner.vb` ↔ `TtsConcurrencyRunner.vb` (3-way)

All three benchmark runners end with the identical latency-aggregation block
(sort, average, min/max, P50, P95, throughput) AND each carries its own private
copy of the same `Percentile()` function.

**The fix:** one `LatencyStats` helper in `Services/Testing` that all three call.
**Effort:** small (an hour). **Risk:** negligible — operator test tooling, no
live path. Could be done any time.

---

## Group 5 — Benchmark CSV export
**Where:** `FormTranslationBenchmark.vb:722` ↔ `:1515`

The stage-summary CSV writer (headers + row formatting + queue-metrics section)
exists twice — once in the STT-concurrency export, once in the TTS one.

**The fix:** a `WriteStageCsv(sb, stage)` helper. **Effort:** trivial.
**Risk:** negligible.

---

## Group 6 — TranscribeController's twin catch ladders
**Where:** `TranscribeController.vb:308` ↔ `:373`

The two pipeline-launch handlers end in identical
Cancelled / PipelineException / Exception catch ladders (status label +
unified log + notify), duplicated verbatim.

**The fix:** wrap the pipeline call in one `RunPipelineGuarded(work)` helper
owning the ladder. **Effort:** trivial. **Risk:** low (desktop UI).

---

## Bonus (seen by eye, below the detector's window size)
The Python **log-writer thread** block is copy-pasted across all four sidecars
(`live-server`, `translate-server`, `mms-tts-server`, `qe-server`). A shared
module is awkward there (each sidecar ships as its own folder), so the realistic
options are: a shared `sidecar_logging.py` added to each folder's publish, or
accepting the duplication with a marker comment naming the master copy. Worth a
decision, not urgent.

---

## Recommended order

| # | Group | When | Why this order |
|---|-------|------|----------------|
| 1 | G4 stats + G5 CSV + G6 catch ladders | any time | zero live-path risk, quick wins |
| 2 | G1 PipelineRunner steps | any time | offline feature, fully testable locally |
| 3 | G3 options mirror | after Sunday | rooms path, needs a live-room check |
| 4 | G2 backend base class | after Sunday | the Sunday whisper path itself |
| 5 | Bonus log-writer | with the next sidecar change | needs a packaging decision first |

After each extraction, `node tools/audit-clones.js` should show the group gone —
the auditor is the definition of done.
