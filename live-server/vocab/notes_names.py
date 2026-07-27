#!/usr/bin/env python3
"""Extract people/place name candidates from a sermon-notes file.

Usage: python notes_names.py <notes-file>
Prints JSON: {"names": ["Joni Eareckson Tada", ...]} or {"error": "..."}.

Supported formats: .pdf (pypdf), .docx/.odt/.pages (zip+XML, stdlib only),
.rtf (control-word strip), .xml (tag strip), .json (string values),
.txt/.md and anything else (plain text).

The candidate heuristic is LANGUAGE-NEUTRAL and derived from the document
itself (no static word lists, per project rules):
  - runs of capitalized words (allowing short lowercase connectors like
    "de"/"of" inside a run) become multi-word candidates;
  - single capitalized words qualify only if their lowercase form never
    appears as an ordinary word in the same document (self-derived
    common-word test, same idea as the Bible book-alias ambiguity check)
    AND they appear at least once mid-sentence (not only sentence-initial).
"""
import io
import json
import re
import sys
import zipfile
import zlib

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

MAX_NAMES = 40


def _pdf_text(path):
    try:
        from pypdf import PdfReader
        reader = PdfReader(path)
        return " ".join((page.extract_text() or "") for page in reader.pages)
    except ImportError:
        # Raw fallback: inflate content streams and pull the (...) text ops.
        raw = open(path, "rb").read()
        chunks = []
        for m in re.finditer(rb"stream\r?\n(.*?)endstream", raw, re.S):
            try:
                chunks.append(zlib.decompress(m.group(1)).decode("latin-1", "replace"))
            except Exception:
                pass
        blob = " ".join(chunks)
        parts = re.findall(r"\((?:[^()\\]|\\.)*\)", blob)
        joined = " ".join(p[1:-1] for p in parts)
        return joined.replace("\\(", "(").replace("\\)", ")").replace("\\'", "'")


def _xml_zip_text(path, member_patterns):
    """Text from zip-archived XML formats (docx/odt/pages)."""
    with zipfile.ZipFile(path) as z:
        names = z.namelist()
        for pat in member_patterns:
            hits = [n for n in names if re.fullmatch(pat, n)]
            if hits:
                xml = z.read(hits[0]).decode("utf-8", "replace")
                # Block-level tags become sentence-ish breaks so headings don't
                # glue onto the next line as fake mid-sentence evidence.
                xml = re.sub(r"</(?:w:p|text:p|text:h|sf:p)>", ". ", xml)
                return re.sub(r"<[^>]+>", " ", xml)
    return None


def _pages_text(path):
    # Newer .pages bundles carry a QuickLook PDF; older ones an index XML.
    with zipfile.ZipFile(path) as z:
        pdf = next((n for n in z.namelist()
                    if n.lower().endswith(".pdf") and "quicklook" in n.lower()), None)
        if pdf:
            tmp = io.BytesIO(z.read(pdf))
            try:
                from pypdf import PdfReader
                return " ".join((p.extract_text() or "") for p in PdfReader(tmp).pages)
            except ImportError:
                pass
    return _xml_zip_text(path, [r"index\.xml"])


def _rtf_text(raw):
    text = raw.decode("latin-1", "replace")
    # \'xx hex escapes → chars, then drop control words/groups.
    text = re.sub(r"\\'([0-9a-fA-F]{2})", lambda m: chr(int(m.group(1), 16)), text)
    text = re.sub(r"\\par[d]?", ". ", text)
    text = re.sub(r"\\[a-zA-Z]+-?\d*\s?", " ", text)
    return text.replace("{", " ").replace("}", " ")


def _plain_text(path):
    raw = open(path, "rb").read()
    for enc in ("utf-8-sig", "utf-8", "cp1252", "latin-1"):
        try:
            return raw.decode(enc)
        except UnicodeDecodeError:
            continue
    return raw.decode("latin-1", "replace")


def _xml_text(path):
    xml = _plain_text(path)
    # Closing tags act as sentence breaks so sibling elements don't glue into
    # one fake capitalized run; then strip all remaining markup.
    xml = re.sub(r"</[^>]+>", ". ", xml)
    return re.sub(r"<[^>]+>", " ", xml)


def _json_text(path):
    # Every string value becomes its own "sentence" — keys are skipped
    # (they're identifiers, not prose).
    data = json.loads(_plain_text(path))
    parts = []

    def walk(node):
        if isinstance(node, str):
            parts.append(node)
        elif isinstance(node, dict):
            for v in node.values():
                walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)

    walk(data)
    return ". ".join(parts)


def extract_text(path):
    ext = path.lower().rsplit(".", 1)[-1] if "." in path else ""
    if ext == "pdf":
        return _pdf_text(path)
    if ext == "docx":
        return _xml_zip_text(path, [r"word/document\.xml"])
    if ext == "odt":
        return _xml_zip_text(path, [r"content\.xml"])
    if ext == "pages":
        return _pages_text(path)
    if ext == "rtf":
        return _rtf_text(open(path, "rb").read())
    if ext == "xml":
        return _xml_text(path)
    if ext == "json":
        return _json_text(path)
    return _plain_text(path)


# No "." in the trailing class — a sentence-final dot must stay OUTSIDE the
# token or the boundary detector never sees it.
WORD = re.compile(r"[^\W\d_][\w'’-]*", re.UNICODE)


def candidates(text):
    tokens = []  # (word, starts_sentence)
    boundary = True
    for m in WORD.finditer(text):
        between = text[tokens[-1][2]:m.start()] if tokens else ""
        if tokens:
            boundary = bool(re.search(r"[.!?…:;\n\r]", between))
        tokens.append((m.group(0), boundary, m.end()))
        boundary = False

    lowercase_forms = {w.lower() for w, _, _ in tokens if w[:1].islower()}

    def is_cap(w):
        return w[:1].isupper() and len(w) > 1

    # Runs of capitalized words; short lowercase connectors allowed INSIDE.
    runs = []
    i = 0
    while i < len(tokens):
        w, starts, _ = tokens[i]
        if is_cap(w):
            run = [w]
            run_start = starts
            j = i + 1
            while j < len(tokens) and not tokens[j][1]:
                nw = tokens[j][0]
                if is_cap(nw):
                    run.append(nw)
                    j += 1
                elif nw.islower() and len(nw) <= 3 and j + 1 < len(tokens) \
                        and not tokens[j + 1][1] and is_cap(tokens[j + 1][0]):
                    run.append(nw)
                    run.append(tokens[j + 1][0])
                    j += 2
                else:
                    break
            runs.append((run, run_start))
            i = j
        else:
            i += 1

    counts = {}
    mid_sentence = set()
    for run, starts in runs:
        # Strip leading short words whose lowercase form occurs as an ordinary
        # word in this document — capitalized articles glued onto a name
        # ("La Joni", "The Lord") without any static per-language article list.
        while len(run) > 1 and len(run[0]) <= 3 and run[0].lower() in lowercase_forms:
            run = run[1:]
        if len(run) > 1:
            key = " ".join(run)
        else:
            w = run[0].strip(".'’-")
            if len(w) < 3 or w.lower() in lowercase_forms:
                continue
            key = w
        counts[key] = counts.get(key, 0) + 1
        if not starts:
            mid_sentence.add(key)

    keep = [k for k in counts if k in mid_sentence or counts[k] >= 2]
    keep.sort(key=lambda k: (-counts[k], k))
    # Drop single words that are also part of a kept multi-word name.
    multi = [k for k in keep if " " in k]
    keep = [k for k in keep
            if " " in k or not any(k in m.split() for m in multi)]
    return keep[:MAX_NAMES]


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "usage: notes_names.py <file>"}))
        return 2
    try:
        text = extract_text(sys.argv[1])
        if not text or not text.strip():
            print(json.dumps({"error": "no text could be extracted"}))
            return 1
        print(json.dumps({"names": candidates(text)}, ensure_ascii=False))
        return 0
    except Exception as e:
        print(json.dumps({"error": str(e)}))
        return 1


if __name__ == "__main__":
    sys.exit(main())
