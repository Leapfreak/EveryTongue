# Extract proper nouns from Marina's sermon-notes PDF (no external deps).
import glob, io, re, sys, zlib
from collections import Counter
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

pdf = glob.glob(r"C:/Users/Jeremy/Desktop/Source/EveryTongue/EveryTongue/bin/Publish/logs/20260726_103054/*.pdf")[0]
raw = open(pdf, "rb").read()
chunks = []
for m in re.finditer(rb"stream\r?\n(.*?)endstream", raw, re.S):
    try:
        chunks.append(zlib.decompress(m.group(1)).decode("latin-1", "replace"))
    except Exception:
        pass
blob = " ".join(chunks)
parts = re.findall(r"\((?:[^()\\]|\\.)*\)", blob)
joined = " ".join(p[1:-1] for p in parts)
joined = joined.replace("\\(", "(").replace("\\)", ")").replace("\\'", "'")
print("pdf:", pdf)
print("sample:", joined[:400])
words = re.findall(r"[A-ZÀ-Ý][\wà-ÿ.'-]+", joined)
print("capitalized:", Counter(w for w in words if len(w) > 2).most_common(35))
