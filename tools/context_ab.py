# Context A/B: translate today's sermon commits (Marina, 11:26+) through NLLB
# plain (== what the service received this morning; same model+compute, temp-0)
# vs concatenation-context (prev 2 commits prepended, trim-by-sentence-count,
# fallback to plain on count mismatch). Writes CSV + prints summary/canaries.
import json, re, sys, time, urllib.request

PORT = 5085
SRC = r"C:/Users/Jeremy/AppData/Local/Temp/marina_src.txt"
OUT = r"C:/Users/Jeremy/AppData/Local/Temp/context_ab.csv"

def post(path, payload, timeout=60):
    req = urllib.request.Request(
        f"http://127.0.0.1:{PORT}{path}",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read().decode("utf-8"))

def translate(text):
    r = post("/translate", {"text": text, "source_lang": "cat_Latn",
                            "target_langs": ["eng_Latn"], "no_cache": True})
    return (r.get("translations") or {}).get("eng_Latn", "")

SENT = re.compile(r"[^.!?…]+[.!?…]*")
def sents(text):
    return [s.strip() for s in SENT.findall(text) if s.strip()]

def main():
    # load model (server started with --model-path/--compute-type args)
    for i in range(60):
        try:
            st = post("/load", {"device": "cuda"}, timeout=300)
            print("load:", st, flush=True)
            break
        except Exception as e:
            time.sleep(2)
    lines = [l.strip() for l in open(SRC, encoding="utf-8") if l.strip()]
    rows, diffs, fallbacks = [], 0, 0
    t0 = time.time()
    for i, line in enumerate(lines):
        plain = translate(line)
        ctx_used, ctx_out = False, plain
        if i >= 1:
            prev = " ".join(lines[max(0, i-2):i])
            blob = prev + " " + line
            full = translate(blob)
            n_new = len(sents(line))
            n_all = len(sents(blob))
            out_s = sents(full)
            if len(out_s) == n_all and n_new >= 1:
                ctx_out = " ".join(out_s[-n_new:])
                ctx_used = True
            else:
                fallbacks += 1
        if ctx_out.strip().lower() != plain.strip().lower():
            diffs += 1
        rows.append((line, plain, ctx_used, ctx_out))
        if (i + 1) % 25 == 0:
            print(f"{i+1}/{len(lines)} ({time.time()-t0:.0f}s)", flush=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("src\tplain\tctx_used\tctx_out\n")
        for r in rows:
            f.write("\t".join([r[0], r[1], str(r[2]), r[3]]).replace("\n", " ") + "\n")
    print(f"DONE {len(rows)} lines | differing: {diffs} | fallbacks: {fallbacks}", flush=True)
    print("== canaries (Marina) ==", flush=True)
    for r in rows:
        if "marina" in r[0].lower():
            print("SRC:", r[0], flush=True)
            print("  PLAIN:", r[1], flush=True)
            print("  CTX  :", r[3], flush=True)

main()
