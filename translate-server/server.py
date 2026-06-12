"""
Translation Sidecar for Every Tongue.
FastAPI REST server wrapping CTranslate2 for real-time multi-target translation.
Uses NLLB-200 models.
"""

import argparse
import asyncio
import json
import logging
import logging.handlers
import os
import queue as _queue_mod
import re
import threading
import time
from collections import OrderedDict
from threading import Lock

import ctranslate2
import sentencepiece as spm
from fastapi import FastAPI
from pydantic import BaseModel

# ---------------------------------------------------------------------------
# Logging — file-based via RotatingFileHandler
# ---------------------------------------------------------------------------
# Logs go to a file in --log-dir (passed by .NET). The file is tailed by
# PythonSidecarHost which feeds lines to AppLogger/UI. This completely avoids
# the 4KB Windows pipe buffer bottleneck that caused cascading deadlocks.
#
# QueueHandler ensures logger.info/debug() never blocks the caller.
# ---------------------------------------------------------------------------

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")

_log_queue = _queue_mod.Queue(maxsize=5000)

# Main logger
logger = logging.getLogger("translate-server")
logger.setLevel(logging.DEBUG)
logger.propagate = False
_queue_handler = logging.handlers.QueueHandler(_log_queue)
_queue_handler.setLevel(logging.DEBUG)
logger.addHandler(_queue_handler)

# Debug logger (verbose translation details)
_debug_logger = logging.getLogger("translate-debug")
_debug_logger.setLevel(logging.DEBUG)
_debug_logger.propagate = False
_debug_queue_handler = logging.handlers.QueueHandler(_log_queue)
_debug_queue_handler.setLevel(logging.DEBUG)
_debug_logger.addHandler(_debug_queue_handler)

# File handler — configured in __main__ once --log-dir is known.
# Until then, a fallback stderr handler catches early messages.
_file_handler = None
_active_handler = logging.StreamHandler()
_active_handler.setLevel(logging.INFO)
_active_handler.setFormatter(logging.Formatter("[TRANSLATE] %(message)s"))


def _setup_file_logging(log_dir: str):
    """Switch from stderr fallback to RotatingFileHandler."""
    global _file_handler, _active_handler
    os.makedirs(log_dir, exist_ok=True)
    log_path = os.path.join(log_dir, "translate-server.log")
    _file_handler = logging.handlers.RotatingFileHandler(
        log_path, maxBytes=2 * 1024 * 1024, backupCount=2, encoding="utf-8")
    _file_handler.setLevel(logging.DEBUG)
    _file_handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(message)s"))
    _active_handler = _file_handler


def _log_writer_thread():
    """Drain log queue to file (or stderr fallback). Only this thread does I/O."""
    while True:
        try:
            record = _log_queue.get(timeout=1.0)
            handler = _active_handler
            if handler.level <= record.levelno:
                try:
                    handler.emit(record)
                except Exception:
                    pass
        except _queue_mod.Empty:
            continue
        except Exception:
            break

_log_writer = threading.Thread(target=_log_writer_thread, daemon=True, name="log-writer")
_log_writer.start()


def _apply_log_level(level_name: str):
    """Set handler level from config: minimal/normal/verbose."""
    level_map = {"minimal": logging.WARNING, "normal": logging.INFO, "verbose": logging.DEBUG}
    level = level_map.get(level_name.lower(), logging.INFO)
    if _file_handler:
        _file_handler.setLevel(level)
    else:
        _active_handler.setLevel(level)


app = FastAPI()

# ---------------------------------------------------------------------------
# Global state
# ---------------------------------------------------------------------------
translator = None
sp_model = None
device_in_use = "cpu"
model_path_global = ""
glossary_path_global = ""
_lock = Lock()


# ---------------------------------------------------------------------------
# LRU translation cache
# ---------------------------------------------------------------------------
class LRUCache:
    def __init__(self, capacity: int = 5000):
        self._cache: OrderedDict = OrderedDict()
        self._capacity = capacity
        self._lock = Lock()

    def get(self, key):
        with self._lock:
            if key in self._cache:
                self._cache.move_to_end(key)
                return self._cache[key]
        return None

    def put(self, key, value):
        with self._lock:
            if key in self._cache:
                self._cache.move_to_end(key)
            self._cache[key] = value
            while len(self._cache) > self._capacity:
                self._cache.popitem(last=False)


cache = LRUCache(5000)


# ---------------------------------------------------------------------------
# Profanity filter: scrub hallucinated profanity from translation output
# ---------------------------------------------------------------------------
_profanity_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "profanity.json")
_profanity_patterns: dict[str, re.Pattern] = {}


def _compile_profanity_file(path):
    """Compile a profanity.json file into {lang: compiled regex}. Raises on I/O errors.
    Supports both legacy format (plain string arrays) and new format
    (objects with "word" and "enabled" fields). Disabled items are skipped.
    """
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    patterns = {}
    for lang, items in data.items():
        words = []
        for item in items:
            if isinstance(item, str):
                # Legacy format: plain string (always enabled)
                words.append(item)
            elif isinstance(item, dict):
                # New format: {"word": "...", "enabled": true/false}
                if item.get("enabled", True):
                    words.append(item.get("word", ""))
        words = [w for w in words if w]  # filter empty
        if words:
            pattern = r"\b(" + "|".join(re.escape(w) for w in words) + r")\b"
            patterns[lang] = re.compile(pattern, re.IGNORECASE)
    return patterns


def _load_profanity():
    """Load the GLOBAL profanity word lists from profanity.json. Returns language count."""
    global _profanity_patterns
    try:
        _profanity_patterns = _compile_profanity_file(_profanity_path)
        logger.info("Profanity filter loaded: %s", ", ".join(f"{k}" for k in _profanity_patterns))
        return len(_profanity_patterns)
    except FileNotFoundError:
        logger.info("No profanity.json found — profanity filter disabled")
        _profanity_patterns = {}
        return 0
    except Exception as e:
        logger.warning("Failed to load profanity.json: %s", e)
        _profanity_patterns = {}
        return 0


_load_profanity()


# ---------------------------------------------------------------------------
# Per-request filter sets: named glossary/profanity files selected per session.
# Cached by path, invalidated when the file's mtime changes. Missing/broken
# files fall back to the global set (logged once per load attempt).
# ---------------------------------------------------------------------------
_filter_set_cache: dict = {}  # path -> (mtime, loaded object)


def _load_cached_filter(path, loader):
    try:
        mtime = os.path.getmtime(path)
    except OSError:
        return None
    entry = _filter_set_cache.get(path)
    if entry is not None and entry[0] == mtime:
        return entry[1]
    try:
        obj = loader(path)
    except Exception as e:
        logger.warning("Filter set load failed for %s: %s", path, e)
        return None
    _filter_set_cache[path] = (mtime, obj)
    logger.info("Filter set loaded: %s", path)
    return obj


def _get_glossary(path):
    """The glossary for a request: the named set at `path`, else the global one."""
    if not path:
        return glossary

    def loader(p):
        g = Glossary()
        g.load(p)
        return g

    return _load_cached_filter(path, loader) or glossary


def _get_profanity_patterns(path):
    """The profanity patterns for a request: the named set at `path`, else global."""
    if not path:
        return _profanity_patterns
    pats = _load_cached_filter(path, _compile_profanity_file)
    return pats if pats is not None else _profanity_patterns


def _filter_profanity(text: str, target_lang: str, patterns=None) -> str:
    """Replace profane words with [...] if a filter exists for the target language."""
    if patterns is None:
        patterns = _profanity_patterns
    pattern = patterns.get(target_lang)
    if not pattern:
        return text
    return pattern.sub("[...]", text)


# ---------------------------------------------------------------------------
# Glossary: source-aware post-translation fixups
# ---------------------------------------------------------------------------
class Glossary:
    """
    Glossary keyed by source language (FLORES code).
    Each source language maps to a list of entries with:
      - trigger: substring to look for in the source text (case-insensitive)
      - fixes: {target_lang: {wrong_word: right_word, ...}}

    Format: {"eng_Latn": [{"trigger": "...", "fixes": {...}}, ...], ...}
    """

    def __init__(self):
        self.data: dict[str, list[dict]] = {}

    def load(self, path: str) -> int:
        """Load glossary from JSON file. Returns number of entries loaded.
        Entries with "enabled": false are skipped.
        """
        try:
            with open(path, "r", encoding="utf-8") as f:
                raw = json.load(f)

            self.data = {}
            total = 0
            for lang, entries in raw.items():
                filtered = [e for e in entries if "trigger" in e and e.get("enabled", True)]
                if filtered:
                    self.data[lang] = filtered
                    total += len(filtered)

            logger.info("Glossary loaded: %d entries across %d languages from %s",
                        total, len(self.data), path)
            return total
        except FileNotFoundError:
            logger.info("No glossary file at %s", path)
            self.data = {}
            return 0
        except Exception as e:
            logger.warning("Failed to load glossary from %s: %s", path, e)
            self.data = {}
            return 0

    def apply(self, source_text: str, source_lang: str,
              target_lang: str, translated: str) -> str:
        """Apply glossary fixes to a translated string."""
        entries = self.data.get(source_lang, [])
        if not entries:
            return translated

        source_lower = source_text.lower()
        result = translated

        for entry in entries:
            trigger = entry.get("trigger", "")
            if not trigger:
                continue

            # Check if trigger is in source text
            if trigger.lower() not in source_lower:
                continue

            # Apply fixes for this target language
            fixes = entry.get("fixes", {}).get(target_lang, {})
            for wrong, right in fixes.items():
                # If the pattern contains non-word chars (punctuation, hyphens),
                # use plain string replacement — \b won't match correctly.
                if re.search(r"[^\w\s]", wrong):
                    if wrong in result:
                        result = result.replace(wrong, right)
                    elif wrong.lower() in result.lower():
                        # Case-insensitive plain replacement
                        pattern = re.escape(wrong)
                        result = re.sub(pattern, right, result, flags=re.IGNORECASE)
                else:
                    # Word-boundary replacement, case-insensitive
                    pattern = r"\b" + re.escape(wrong) + r"\b"
                    result = re.sub(pattern, right, result, flags=re.IGNORECASE)

        return result


glossary = Glossary()


# ---------------------------------------------------------------------------
# Request / Response models
# ---------------------------------------------------------------------------
class TranslateRequest(BaseModel):
    text: str
    source_lang: str
    target_langs: list[str]
    no_cache: bool = False
    # Optional per-session filter set ("" = the global files). Safe with the
    # translation cache: glossary/profanity are applied AFTER the cached raw
    # translation, so the cache key needs no filter component.
    glossary_path: str = ""
    profanity_path: str = ""


class TranslateResponse(BaseModel):
    translations: dict[str, str]


class GlossaryApplyRequest(BaseModel):
    # Apply glossary + profanity fixups to already-translated text (no NLLB).
    # Used for translations produced inline by the STT engine (e.g. Speechmatics).
    source_text: str
    source_lang: str                 # FLORES code, e.g. "cat_Latn"
    translations: dict[str, str]     # {target FLORES code: translated text}
    glossary_path: str = ""          # optional per-session filter set
    profanity_path: str = ""


class LoadRequest(BaseModel):
    device: str = "cuda"


class StatusResponse(BaseModel):
    status: str
    model_loaded: bool = False
    device: str = ""


# ---------------------------------------------------------------------------
# Translation helpers
# ---------------------------------------------------------------------------
def _translate_single(text: str, source_lang: str, target_lang: str, no_cache: bool = False) -> str:
    """Translate text from source_lang to target_lang using the loaded NLLB model."""
    _debug_logger.debug("─" * 60)
    _debug_logger.debug("[TRANSLATE] %s -> %s", source_lang, target_lang)
    _debug_logger.debug("[INPUT] %r", text)

    cleaned = text.strip()

    cache_key = (cleaned, source_lang, target_lang)
    if not no_cache:
        cached = cache.get(cache_key)
        if cached is not None:
            _debug_logger.debug("[CACHE HIT] %r", cached)
            return cached

    # Tokenize: prepend source language token, append EOS, use target_prefix
    t_tok = time.perf_counter()
    sp_model.set_encode_extra_options("")
    tokens = sp_model.encode(cleaned, out_type=str)
    tok_ms = (time.perf_counter() - t_tok) * 1000
    _debug_logger.debug("[TOKENS] %d tokens (%.1fms): %s",
                        len(tokens), tok_ms, " ".join(tokens[:30]))

    tokens = [source_lang] + tokens + ["</s>"]

    t_xlate = time.perf_counter()
    results = translator.translate_batch(
        [tokens],
        target_prefix=[[target_lang]],
        beam_size=4,
        max_decoding_length=256,
    )
    xlate_ms = (time.perf_counter() - t_xlate) * 1000

    # Decode: skip the target language token
    output_tokens = results[0].hypotheses[0]
    if output_tokens and output_tokens[0] == target_lang:
        output_tokens = output_tokens[1:]

    translated = sp_model.decode(output_tokens)
    _debug_logger.debug("[OUTPUT RAW] %r  (translate: %.1fms)", translated, xlate_ms)
    cache.put(cache_key, translated)
    return translated


def _reload_on_cpu():
    """Reload model on CPU after a CUDA runtime failure."""
    global translator, device_in_use
    logger.warning("CUDA runtime error during translation (device_in_use=%s), reloading model on CPU...", device_in_use)
    try:
        translator = ctranslate2.Translator(
            model_path_global, device="cpu", compute_type="float32"
        )
        device_in_use = "cpu"
        logger.info("Model reloaded on CPU successfully")
    except Exception as e2:
        logger.error("CPU reload also failed: %s", e2, exc_info=True)
        translator = None


def _translate_to_targets(text: str, source_lang: str, target_langs: list[str], no_cache: bool = False,
                          glossary_path: str = "", profanity_path: str = "") -> dict[str, str]:
    """Translate to all target languages, then apply glossary fixes."""
    global translator
    t_total = time.perf_counter()
    logger.info("[TRANSLATE] %s -> %s: %r", source_lang, target_langs, text)
    gl = _get_glossary(glossary_path)
    prof = _get_profanity_patterns(profanity_path)
    results = {}
    for tl in target_langs:
        try:
            translated = _translate_single(text, source_lang, tl, no_cache=no_cache)
            after_glossary = gl.apply(text, source_lang, tl, translated)
            if after_glossary != translated:
                logger.info("[GLOSSARY] %s: %r -> %r", tl, translated, after_glossary)
            filtered = _filter_profanity(after_glossary, tl, prof)
            if filtered != after_glossary:
                logger.info("[PROFANITY] %s: %r -> %r", tl, after_glossary, filtered)
            _debug_logger.debug("[FINAL] %s: %r", tl, filtered)
            results[tl] = filtered
        except Exception as e:
            err_msg = str(e)
            logger.error("Translation to %s failed: [%s] %s", tl, type(e).__name__, err_msg, exc_info=True)
            if "cublas" in err_msg.lower() or "cuda" in err_msg.lower():
                logger.error("CUDA error detected during translation — full exception type: %s, device_in_use: %s", type(e).__name__, device_in_use)
                _reload_on_cpu()
                if translator is not None:
                    try:
                        translated = _translate_single(text, source_lang, tl, no_cache=no_cache)
                        after_glossary = gl.apply(text, source_lang, tl, translated)
                        if after_glossary != translated:
                            logger.info("[GLOSSARY] %s: %r -> %r", tl, translated, after_glossary)
                        filtered = _filter_profanity(after_glossary, tl, prof)
                        if filtered != after_glossary:
                            logger.info("[PROFANITY] %s: %r -> %r", tl, after_glossary, filtered)
                        results[tl] = filtered
                        continue
                    except Exception as e2:
                        logger.warning("Translation to %s failed after CPU reload: %s", tl, e2)
                        continue
            # Non-CUDA error already logged with exc_info above
    total_ms = (time.perf_counter() - t_total) * 1000
    for tl, tr in results.items():
        logger.info("[RESULT] %s: %r", tl, tr)
    logger.info("[TOTAL] %d targets in %.1fms", len(target_langs), total_ms)
    return results


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------
@app.post("/translate", response_model=TranslateResponse)
async def translate(req: TranslateRequest):
    if translator is None:
        return TranslateResponse(translations={})

    loop = asyncio.get_event_loop()
    try:
        result = await asyncio.wait_for(
            loop.run_in_executor(
                None, _translate_to_targets, req.text, req.source_lang, req.target_langs, req.no_cache,
                req.glossary_path, req.profanity_path
            ),
            timeout=10.0,
        )
    except asyncio.TimeoutError:
        logger.warning("Translation timed out for: %s", req.text[:80])
        return TranslateResponse(translations={})

    return TranslateResponse(translations=result)


@app.post("/load", response_model=StatusResponse)
async def load_model(req: LoadRequest):
    global translator, sp_model, device_in_use

    device = req.device
    with _lock:
        try:
            logger.info("Loading model from %s on %s...", model_path_global, device)

            # Try CUDA, fall back to CPU
            # Use compute_type="auto" so quantized models (int8, int8_float16)
            # run with their native precision instead of being dequantized.
            try:
                translator = ctranslate2.Translator(
                    model_path_global, device=device, compute_type="auto"
                )
                device_in_use = device
            except Exception as cuda_err:
                if device != "cpu":
                    logger.warning("CUDA failed during model load: [%s] %s", type(cuda_err).__name__, cuda_err, exc_info=True)
                    logger.warning("Falling back to CPU...")
                    translator = ctranslate2.Translator(
                        model_path_global, device="cpu", compute_type="auto"
                    )
                    device_in_use = "cpu"
                else:
                    raise

            # Load SentencePiece model
            model_dir = model_path_global.rstrip("/\\")
            sp_path = os.path.join(model_dir, "sentencepiece.bpe.model")
            if not os.path.exists(sp_path):
                sp_path = os.path.join(model_dir, "spiece.model")
            sp_model = spm.SentencePieceProcessor()
            sp_model.load(sp_path)
            logger.info("Loaded tokenizer from %s", sp_path)

            logger.info("Model loaded successfully on %s", device_in_use)
            return StatusResponse(status="ok", model_loaded=True, device=device_in_use)
        except Exception as e:
            logger.error("Failed to load model: %s", e)
            translator = None
            sp_model = None
            return StatusResponse(status=f"error: {e}", model_loaded=False)


@app.post("/unload", response_model=StatusResponse)
async def unload_model():
    global translator, sp_model
    with _lock:
        translator = None
        sp_model = None
        import gc
        gc.collect()
        logger.info("Model unloaded")
    return StatusResponse(status="ok")


@app.post("/glossary/apply", response_model=TranslateResponse)
async def apply_glossary(req: GlossaryApplyRequest):
    """Apply glossary + profanity fixups to already-translated text without running
    NLLB. For inline STT-engine translations (Speechmatics) that bypass /translate."""
    gl = _get_glossary(req.glossary_path)
    prof = _get_profanity_patterns(req.profanity_path)
    result: dict[str, str] = {}
    for target_lang, translated in req.translations.items():
        fixed = gl.apply(req.source_text, req.source_lang, target_lang, translated)
        fixed = _filter_profanity(fixed, target_lang, prof)
        result[target_lang] = fixed
    return TranslateResponse(translations=result)


@app.post("/glossary/reload")
async def reload_glossary():
    count = glossary.load(glossary_path_global)
    return {"status": "ok", "entries": count}


@app.post("/profanity/reload")
async def reload_profanity():
    count = _load_profanity()
    return {"status": "ok", "languages": count}


@app.get("/health", response_model=StatusResponse)
async def health():
    loaded = translator is not None
    return StatusResponse(
        status="ready" if loaded else "idle",
        model_loaded=loaded,
        device=device_in_use if loaded else "",
    )


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="NLLB Translation Server")
    parser.add_argument("--port", type=int, default=5090)
    parser.add_argument("--model-path", type=str, required=True)
    parser.add_argument("--device", type=str, default="cuda")
    parser.add_argument("--model-type", type=str, default="nllb",
                        help="Model architecture (kept for backward compatibility)")
    parser.add_argument("--log-level", type=str, default="normal",
                        choices=["minimal", "normal", "verbose"],
                        help="Log verbosity: minimal (errors only), normal (default), verbose (all debug)")
    parser.add_argument("--glossary", type=str, default="",
                        help="Path to glossary.json for post-translation fixes")
    parser.add_argument("--log-dir", type=str, default="",
                        help="Directory for log files (RotatingFileHandler)")
    args = parser.parse_args()

    if args.log_dir:
        _setup_file_logging(args.log_dir)

    model_path_global = args.model_path
    _apply_log_level(args.log_level)
    _debug_logger.info("Translation server started")

    # Load glossary: explicit path, or default next to server.py
    glossary_path_global = args.glossary
    if not glossary_path_global:
        glossary_path_global = os.path.join(os.path.dirname(os.path.abspath(__file__)), "glossary.json")
    glossary.load(glossary_path_global)

    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=args.port, log_level="info")
