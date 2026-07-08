"""Build Speechmatics biblical additional_vocab lists from the INSTALLED Bibles.

Generated on-device (no hosted asset) so the wordlists always match the user's
own Bibles. Scans every <bibles_dir>/<iso3>/*.sqlite3 (MyBible format:
verses(text)) and writes <out_dir>/biblical-vocab-<iso1>.json for each language
that has a Bible (iso3→iso1 via the app's language-codes.json).

Heuristic (from tools/extract_bible_names.py): a proper noun is a token that
appears Capitalized many times and essentially never lowercase (<=2%). Handles
ca/es contractions (l'Elies, d'Egipte). Names are merged across every Bible
installed for the language, then the top 1000 by frequency are written.

Usage:  python build_vocab.py <bibles_dir> <out_dir>
Prints one status line per language:  VOCAB <lang> OK <n>  |  VOCAB <lang> SKIP no-bible
"""
import sqlite3, re, collections, sys, os, json, glob

WORD = re.compile(r"[A-Za-zÀ-ſ][A-Za-zÀ-ſ'·\-]*")
TAG = re.compile(r"<[^>]+>")
CONTR = ("l'", "d'", "s'", "n'", "m'", "t'")
STOP = set("""
y o u e a i de la el los las un una en con por para que se su sus del al lo le
les els un uns una unes i o a de la el els les que es se son com per amb sense
""".split())

# Bibles/ subdirs are ISO 639-3 (spa/cat/eng/deu/...); the STT session language is
# ISO 639-1 (es/ca/en/de/...). The mapping is read from the app's canonical
# language-codes.json (single source of truth) — no codes hardcoded here.


def _load_iso3_to_1(explicit_path=None):
    """Build an ISO 639-3 -> 639-1 map from the app's language-codes.json."""
    path = explicit_path
    if not path:
        here = os.path.dirname(os.path.abspath(__file__))
        path = os.path.join(here, "..", "..", "wwwroot", "data", "language-codes.json")
    try:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        entries = data.values() if isinstance(data, dict) else data
        m = {}
        for e in entries:
            if isinstance(e, dict) and e.get("iso3") and e.get("iso1"):
                m[e["iso3"].lower()] = e["iso1"].lower()
        return m
    except Exception as e:
        print(f"VOCAB - WARN language-codes.json load failed ({e}); "
              f"only 2-letter Bible dirs will map", file=sys.stderr)
        return {}


def _candidate(token):
    low = token.lower()
    for p in CONTR:
        if low.startswith(p) and len(token) > len(p):
            seg = token[len(p):]
            return seg if seg[:1].isupper() else None
    return token if token[:1].isupper() else None


def _scan_db(db_path, cap, low):
    """Accumulate capitalized / lowercase counts from one MyBible DB."""
    c = sqlite3.connect(db_path)
    try:
        for (text,) in c.execute("SELECT text FROM verses"):
            text = TAG.sub(" ", text or "")
            for m in WORD.finditer(text):
                tok = m.group(0)
                seg = _candidate(tok)
                if seg is not None:
                    cap[seg.lower()] += 1
                else:
                    low[tok.lower().split("'")[-1]] += 1
    finally:
        c.close()


def _candidates(cap, low, min_count=3, max_lower_ratio=0.02):
    out = []
    for key, cnt in cap.items():
        if key in STOP or len(key) == 1:
            continue
        lc = low.get(key, 0)
        if cnt >= min_count and lc <= max(1, cnt * max_lower_ratio):
            out.append((cnt, key))
    out.sort(reverse=True)
    return out


def _clean_and_merge(cands, cap=1000):
    """Merge hyphen-split variants (Is-rael->Israel) and drop marker words."""
    DROP = {"pausa"}  # Selah rendered as a section marker, not a name
    by = {}
    for cnt, key in cands:
        if key in DROP:
            continue
        dk = key.replace("-", "")
        by.setdefault(dk, []).append((cnt, key))
    merged = []
    for dk, members in by.items():
        total = sum(c for c, _ in members)
        nohyph = [m for m in members if "-" not in m[1]]
        canon = max(nohyph or members, key=lambda m: m[0])[1]
        merged.append((total, canon))
    merged.sort(reverse=True)
    return merged[:cap]


def _iso1_for(subdir, iso3_to_1):
    """Map a Bibles/ subdir name to an ISO 639-1 code, or None if unknown."""
    key = subdir.lower()
    if key in iso3_to_1:
        return iso3_to_1[key]
    if len(key) == 2 and key.isalpha():
        return key   # already a 2-letter code
    return None


def build(bibles_dir, out_dir, lang_codes_path=None):
    """Generate a vocab list for EVERY language that has an installed Bible."""
    os.makedirs(out_dir, exist_ok=True)
    iso3_to_1 = _load_iso3_to_1(lang_codes_path)
    disp = lambda k: k[:1].upper() + k[1:]
    if not os.path.isdir(bibles_dir):
        print(f"VOCAB - SKIP no-bibles-dir {bibles_dir}")
        return
    for subdir in sorted(os.listdir(bibles_dir)):
        d = os.path.join(bibles_dir, subdir)
        if not os.path.isdir(d):
            continue
        dbs = []
        for ext in ("*.sqlite3", "*.sqlite", "*.db"):
            dbs += glob.glob(os.path.join(d, ext))
        seen, uniq = set(), []
        for x in dbs:
            if x.lower() not in seen:
                seen.add(x.lower())
                uniq.append(x)
        if not uniq:
            continue
        lang = _iso1_for(subdir, iso3_to_1)
        if not lang:
            print(f"VOCAB {subdir} SKIP unknown-language")
            continue
        cap, low = collections.Counter(), collections.Counter()
        for db in uniq:
            try:
                _scan_db(db, cap, low)
            except Exception as e:
                print(f"VOCAB {lang} WARN {os.path.basename(db)}: {e}")
        final = _clean_and_merge(_candidates(cap, low))
        entries = [{"content": disp(k)} for _, k in final]
        out_path = os.path.join(out_dir, f"biblical-vocab-{lang}.json")
        with open(out_path, "w", encoding="utf-8") as f:
            json.dump(entries, f, ensure_ascii=False, indent=1)
        print(f"VOCAB {lang} OK {len(entries)}")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("usage: build_vocab.py <bibles_dir> <out_dir> [language-codes.json]", file=sys.stderr)
        sys.exit(2)
    build(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
