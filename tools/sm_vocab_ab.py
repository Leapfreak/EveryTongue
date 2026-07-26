# Speechmatics additional_vocab A/B on the Joni segment of today's sermon.
# Vocab list = what Marina's notes + room template would supply automatically.
import asyncio, io, json, os, re, sys, time, wave
from collections import Counter
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

from speechmatics.rt import (AsyncClient, AudioEncoding, AudioFormat,
                             ServerMessageType, TranscriptionConfig, TranscriptResult)

cfg = json.load(open(os.environ["APPDATA"] + r"/EveryTongue/config.json", encoding="utf-8"))
KEY = cfg["SttApiKeys"]["speechmatics"]
URL = "wss://eu2.rt.speechmatics.com/v2"

w = wave.open(os.environ["TEMP"] + r"/joni.wav", "rb")
pcm = w.readframes(w.getnframes())
dur = w.getnframes() / w.getframerate()
print(f"audio: {dur:.0f}s")

VOCAB = [{"content": c} for c in
         ["Joni", "Eareckson", "Tada", "Joni Eareckson Tada", "Marina", "Mateu", "Filipencs", "Satanàs"]]

async def run(vocab):
    texts = []
    async with AsyncClient(api_key=KEY, url=URL) as client:
        @client.on(ServerMessageType.ADD_TRANSCRIPT)
        def _c(m):
            try:
                t = TranscriptResult.from_message(m).metadata.transcript or ""
            except Exception:
                t = (m.get("metadata", {}) or {}).get("transcript", "") or ""
            if t:
                texts.append(t)
        kw = dict(language="ca", enable_partials=False, max_delay=1.0)
        try:
            from speechmatics.rt import OperatingPoint
            kw["operating_point"] = OperatingPoint.ENHANCED
        except Exception:
            pass
        if vocab:
            kw["additional_vocab"] = vocab
        await client.transcribe(
            io.BytesIO(pcm),
            transcription_config=TranscriptionConfig(**kw),
            audio_format=AudioFormat(encoding=AudioEncoding.PCM_S16LE,
                                     sample_rate=16000, chunk_size=3200),
            timeout=dur * 2 + 30)
    return "".join(texts)

def score(label, text):
    low = text.lower()
    hits = {n: len(re.findall(n, low)) for n in
            ["joni", "johnny", "jonny", "jenny", "tada", "tata", "eareckson", "erikson"]}
    print(f"\n== {label} ==  {dict((k, v) for k, v in hits.items() if v)}")
    for m in re.finditer(r"(?:joni|johnny|jonny|jenny|tada|tata|eareckson|erikson)", low):
        s = max(0, m.start() - 45)
        print("  …" + text[s:m.end() + 45].replace("\n", " ") + "…")

t0 = time.time()
off = asyncio.run(run(None))
print(f"[vocab OFF done {time.time()-t0:.0f}s, {len(off)} chars]")
t0 = time.time()
on = asyncio.run(run(VOCAB))
print(f"[vocab ON done {time.time()-t0:.0f}s, {len(on)} chars]")
score("VOCAB OFF", off)
score("VOCAB ON ", on)
