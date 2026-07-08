"""Engine-agnostic sentence segmentation via SaT (wtpsplit).

Downstream of ANY STT engine: given a buffered clause that may contain spurious
mid-phrase periods (Speechmatics) or none at all (whisper), split it into proper
sentences so each is translated whole. This REPLACES the per-language
function-word lists — SaT needs no lists and covers every language.

How it's used (see server.py /segment): the .NET clause accumulator buffers
committed text until a real PAUSE, then posts the buffer here. We strip the
sentence-ending punctuation (which is where the spurious periods live), let SaT
re-decide the boundaries from the words, and restore a period per sentence.

Loads lazily and degrades gracefully: if wtpsplit or the model isn't present the
segment() call just returns the text unchanged (feature no-ops, nothing breaks).
Torch/tokenizers/onnxruntime already ship in the embedded Python; only the
`wtpsplit` package (tools/sat-libs) and the model weights (tools/sat-cache) are
extra, and both are auto-discovered by walking up from this file.
"""
import os
import re
import sys
import logging
import threading

logger = logging.getLogger("live-server.sat")

_lock = threading.Lock()
_model = None
_loaded = False          # a load was attempted (success or failure)
_available = False       # the model is ready to use
_model_name = "sat-3l-sm"

# Sentence-ending punctuation we strip before SaT (this is where the spurious
# mid-phrase periods hide). Commas/colons are KEPT — they aid readability and
# give SaT soft boundary hints.
_ENDERS = ".?!…。？！۔؟"
_STRIP_RE = re.compile("[" + re.escape(_ENDERS) + "]")
_WS_RE = re.compile(r"\s+")


def _discover_paths():
    """Find (sat-libs, sat-cache). Priority: env vars → the Download-Manager
    install <app>\\sat\\ (live-server is <app>\\live-server\\, so ..\\sat\\) →
    a repo tools\\sat-* found by walking up (dev machine)."""
    libs = os.environ.get("SAT_LIBS")
    cache = os.environ.get("SAT_CACHE")
    if libs and cache:
        return libs, cache
    here = os.path.dirname(os.path.abspath(__file__))
    # Download-Manager install: sibling 'sat' folder next to live-server's parent.
    sat_dir = os.path.join(os.path.dirname(here), "sat")
    dm_libs = os.path.join(sat_dir, "sat-libs")
    dm_cache = os.path.join(sat_dir, "sat-cache")
    if os.path.isdir(dm_libs):
        return libs or dm_libs, cache or (dm_cache if os.path.isdir(dm_cache) else None)
    # Dev fallback: walk up for repo tools\sat-*.
    d = here
    for _ in range(7):
        cand_libs = os.path.join(d, "tools", "sat-libs")
        cand_cache = os.path.join(d, "tools", "sat-cache")
        if os.path.isdir(cand_libs):
            return libs or cand_libs, cache or (cand_cache if os.path.isdir(cand_cache) else None)
        parent = os.path.dirname(d)
        if parent == d:
            break
        d = parent
    return libs, cache


def load(model_name=None):
    """Idempotent lazy load. Safe to call from a warm-up thread on /start."""
    global _model, _loaded, _available, _model_name
    if model_name:
        # A different model than the one already loaded → force a reload.
        with _lock:
            if model_name != _model_name:
                _model, _loaded, _available = None, False, False
                _model_name = model_name
    if _loaded:
        return _available
    with _lock:
        if _loaded:
            return _available
        _loaded = True
        try:
            libs, cache = _discover_paths()
            if libs and os.path.isdir(libs) and libs not in sys.path:
                # Front-insert so sat-libs' version-matched transformers/tokenizers
                # win. torch/numpy are already imported (by VAD) and cached, so
                # they stay embedded; only the not-yet-loaded SaT deps come from here.
                sys.path.insert(0, libs)
            if cache and os.path.isdir(cache):
                os.environ.setdefault("HF_HOME", cache)
            from wtpsplit import SaT
            _model = SaT(_model_name)
            _available = True
            logger.info("SaT segmenter ready (model=%s, libs=%s)", _model_name, libs)
        except Exception as e:
            _available = False
            logger.warning("SaT segmenter unavailable (%s: %s) — segmentation no-ops",
                           type(e).__name__, e)
        return _available


def segment(text, threshold=0.1, model_name=None):
    """Split text into sentences. Returns [text] unchanged if SaT is unavailable
    or anything goes wrong — never raises into the request path."""
    if not text or not text.strip():
        return []
    if not load(model_name):
        return [text.strip()]
    try:
        stripped = _WS_RE.sub(" ", _STRIP_RE.sub(" ", text)).strip()
        if not stripped:
            return [text.strip()]
        with _lock:
            raw = list(_model.split([stripped], threshold=threshold))[0]
        out = []
        for s in raw:
            s = _WS_RE.sub(" ", s).strip()
            if not s:
                continue
            # Restore a sentence terminator SaT's re-segmentation removed.
            if s[-1] not in _ENDERS and s[-1] not in ",;:":
                s += "."
            out.append(s)
        return out or [text.strip()]
    except Exception as e:
        logger.warning("SaT segment failed (%s: %s) — passing text through", type(e).__name__, e)
        return [text.strip()]
