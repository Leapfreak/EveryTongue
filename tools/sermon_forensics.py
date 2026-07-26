# Sermon forensics 2026-07-26: align live pipeline output vs batch-whisper
# reference (SRT t=0 == log 11:26:11), both languages. Stats + mistake ledger.
import csv, difflib, io, os, re, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

LOGDIR = r"C:/Users/Jeremy/Desktop/Source/EveryTongue/EveryTongue/bin/Publish/logs/20260726_103054"
T0 = 11*3600 + 26*60 + 11          # SRT zero in seconds-of-day
SPAN = 23*60 + 40                   # audio length

def parse_srt(path):
    segs, txt = [], open(path, encoding="utf-8-sig").read()
    for m in re.finditer(r"(\d\d):(\d\d):(\d\d)[,.](\d+)\s*-->.*?\n(.*?)(?:\n\n|\Z)", txt, re.S):
        h, mn, s = int(m.group(1)), int(m.group(2)), int(m.group(3))
        segs.append((h*3600+mn*60+s, m.group(5).replace("\n", " ").strip()))
    return segs

def toks(text):
    clean = re.sub(r"[^\w'À-ɏ]+", " ", text.lower())
    return clean.split()

def cap_tokens(text):
    out = []
    for seg_start, t in text if isinstance(text, list) else [(0, text)]:
        words = t.split()
        for i, w in enumerate(words):
            wl = re.sub(r"[^\wÀ-ɏ']", "", w)
            if len(wl) > 2 and wl[0].isupper() and i > 0 and not words[i-1].rstrip().endswith((".", "!", "?")):
                out.append(wl)
    return out

# live catalan commits in window
live_ca = []
for line in open(os.path.join(LOGDIR, "session.log"), encoding="utf-8", errors="replace"):
    if "[5005]" not in line or "text=" not in line: continue
    ts = line[11:19]
    try: sod = int(ts[0:2])*3600 + int(ts[3:5])*60 + int(ts[6:8])
    except: continue
    if T0 <= sod <= T0 + SPAN:
        m = re.search(r'text="(.*)"', line)
        if m: live_ca.append((sod - T0, m.group(1)))

wca = parse_srt(os.path.join(LOGDIR, "Catalan.srt"))
wen = parse_srt(os.path.join(LOGDIR, "English.srt"))

# live english = tonight's plain NLLB regeneration keyed by source text
plain = {}
for row in csv.reader(open(os.environ["TEMP"] + r"/context_ab.csv", encoding="utf-8"), delimiter="\t"):
    if len(row) >= 2 and row[0] != "src": plain[row[0]] = row[1]
live_en = [(t, plain[s]) for t, s in live_ca if s in plain]

def align(name, hyp_pairs, ref_pairs):
    hyp = [w for _, t in hyp_pairs for w in toks(t)]
    ref = [w for _, t in ref_pairs for w in toks(t)]
    sm = difflib.SequenceMatcher(None, hyp, ref, autojunk=False)
    match = sum(b.size for b in sm.get_matching_blocks())
    diffs = []
    for op, i1, i2, j1, j2 in sm.get_opcodes():
        if op != "equal":
            diffs.append((max(i2-i1, j2-j1), " ".join(hyp[i1:i2]) or "-", " ".join(ref[j1:j2]) or "-"))
    print(f"\n════ {name} ════")
    print(f"live words: {len(hyp)}  whisper words: {len(ref)}  agreement: {100*match/max(1,len(ref)):.1f}%"
          f"  divergence-vs-whisper: {100*(1-match/max(1,len(ref))):.1f}%  diff-chunks: {len(diffs)}")
    print("top disagreements (LIVE << >> WHISPER):")
    for sz, h, r in sorted(diffs, reverse=True)[:18]:
        print(f"  [{sz}] {h[:90]}  <<>>  {r[:90]}")
    return diffs

print(f"corpus: live-ca commits {len(live_ca)} | whisper-ca segs {len(wca)} | live-en {len(live_en)} | whisper-en segs {len(wen)}")
lw = [len(toks(t)) for _, t in live_ca]; ww = [len(toks(t)) for _, t in wca]
print(f"fragmentation: live {len(live_ca)} units (avg {sum(lw)/len(lw):.1f} words) vs whisper {len(wca)} units (avg {sum(ww)/len(ww):.1f} words) — ratio {len(live_ca)/len(wca):.2f} live units per whisper segment")

d1 = align("STT LAYER — live Speechmatics-ca vs whisper-ca", live_ca, wca)
d2 = align("END-TO-END — live NLLB-en vs whisper-en", live_en, wen)

print("\n════ NAME HARVEST (whisper, mid-sentence capitalized) ════")
from collections import Counter
names = Counter(cap_tokens(wca) + cap_tokens(wen))
print(", ".join(f"{n}×{c}" for n, c in names.most_common(25)))

print("\n════ NAME DISAGREEMENTS (STT layer) ════")
for sz, h, r in d1:
    if re.search(r"joni|johnny|marina|jesu|d[eé]u|satan|mateu", h + " " + r):
        print(f"  LIVE: {h[:80]}  <<>>  WHISPER: {r[:80]}")
