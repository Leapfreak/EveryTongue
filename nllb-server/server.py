"""
NLLB-200 Translation Sidecar for Transcription Tools.
FastAPI REST server wrapping CTranslate2 for real-time multi-target translation.
"""

import argparse
import asyncio
import json
import logging
import os
import re
import time
from collections import OrderedDict
from threading import Lock

import ctranslate2
import sentencepiece as spm
from fastapi import FastAPI
from pydantic import BaseModel

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
logger = logging.getLogger("nllb-server")
logger.propagate = False  # Don't duplicate to stderr — file handler only

# Pipeline debug log — shared with live-server via --log-dir
_debug_logger = logging.getLogger("nllb-debug")
_debug_logger.setLevel(logging.DEBUG)
_debug_logger.propagate = False


class _SharedFileHandler(logging.Handler):
    """File handler that opens/closes per write so other processes can share the file."""
    def __init__(self, path):
        super().__init__()
        self._path = path

    def emit(self, record):
        try:
            with open(self._path, "a", encoding="utf-8") as f:
                f.write(self.format(record) + "\n")
        except Exception:
            pass


def setup_logging(log_dir: str):
    """Set up file logging to YYYYMMDD_pipeline-debug.log in the given directory."""
    log_path = os.path.join(log_dir, time.strftime("%Y%m%d") + "_pipeline-debug.log")
    handler = _SharedFileHandler(log_path)
    handler.setFormatter(logging.Formatter("%(asctime)s [NLLB] %(message)s", datefmt="%Y-%m-%d %H:%M:%S"))
    handler.setLevel(logging.DEBUG)
    _debug_logger.addHandler(handler)

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
try:
    with open(_profanity_path, "r", encoding="utf-8") as f:
        _profanity_words = json.load(f)
    for _lang, _words in _profanity_words.items():
        _pattern = r"\b(" + "|".join(re.escape(w) for w in _words) + r")\b"
        _profanity_patterns[_lang] = re.compile(_pattern, re.IGNORECASE)
    logger.info("Profanity filter loaded: %s", ", ".join(f"{k} ({len(v)} words)" for k, v in _profanity_words.items()))
except FileNotFoundError:
    logger.info("No profanity.json found — profanity filter disabled")
except Exception as _e:
    logger.warning("Failed to load profanity.json: %s", _e)


def _filter_profanity(text: str, target_lang: str) -> str:
    """Replace profane words with [...] if a filter exists for the target language."""
    pattern = _profanity_patterns.get(target_lang)
    if not pattern:
        return text
    return pattern.sub("[...]", text)


# ---------------------------------------------------------------------------
# Glossary: source-aware post-translation fixups
# ---------------------------------------------------------------------------
class Glossary:
    """
    Each entry has:
      - trigger: substring to look for in the source text (case-insensitive)
      - source_langs: list of NLLB source language codes this applies to
      - fixes: {target_lang: {wrong_word: right_word, ...}}

    When source text contains the trigger (for the given source lang),
    the fixes are applied as whole-word replacements on the translation output.
    """

    def __init__(self):
        self.entries: list[dict] = []

    def load(self, path: str) -> int:
        """Load glossary from JSON file. Returns number of entries loaded."""
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
            self.entries = [e for e in data if "trigger" in e]
            logger.info("Glossary loaded: %d entries from %s", len(self.entries), path)
            return len(self.entries)
        except FileNotFoundError:
            logger.info("No glossary file at %s", path)
            self.entries = []
            return 0
        except Exception as e:
            logger.warning("Failed to load glossary from %s: %s", path, e)
            self.entries = []
            return 0

    def apply(self, source_text: str, source_lang: str,
              target_lang: str, translated: str) -> str:
        """Apply glossary fixes to a translated string."""
        if not self.entries:
            return translated

        source_lower = source_text.lower()
        result = translated

        for entry in self.entries:
            trigger = entry.get("trigger", "")
            if not trigger:
                continue

            # Check if source language matches
            source_langs = entry.get("source_langs", [])
            if source_langs and source_lang not in source_langs:
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


class TranslateResponse(BaseModel):
    translations: dict[str, str]


class LoadRequest(BaseModel):
    device: str = "cuda"


class StatusResponse(BaseModel):
    status: str
    model_loaded: bool = False
    device: str = ""


# ---------------------------------------------------------------------------
# Translation helpers
# ---------------------------------------------------------------------------
def _translate_single(text: str, source_lang: str, target_lang: str) -> str:
    """Translate text from source_lang to target_lang using the loaded model."""
    _debug_logger.debug("─" * 60)
    _debug_logger.debug("[TRANSLATE] %s -> %s", source_lang, target_lang)
    _debug_logger.debug("[INPUT] %r", text)

    cleaned = text.strip()

    cache_key = (cleaned, source_lang, target_lang)
    cached = cache.get(cache_key)
    if cached is not None:
        _debug_logger.debug("[CACHE HIT] %r", cached)
        return cached

    # Tokenize with SentencePiece
    t_tok = time.perf_counter()
    sp_model.set_encode_extra_options("")
    tokens = sp_model.encode(cleaned, out_type=str)
    tok_ms = (time.perf_counter() - t_tok) * 1000
    _debug_logger.debug("[TOKENS] %d tokens (%.1fms): %s", len(tokens), tok_ms, " ".join(tokens[:30]))

    # Prepend source language token and append EOS (required by NLLB)
    tokens = [source_lang] + tokens + ["</s>"]

    # Translate
    t_nllb = time.perf_counter()
    results = translator.translate_batch(
        [tokens],
        target_prefix=[[target_lang]],
        beam_size=4,
        max_decoding_length=256,
    )
    nllb_ms = (time.perf_counter() - t_nllb) * 1000

    # Decode: skip the target language token
    output_tokens = results[0].hypotheses[0]
    if output_tokens and output_tokens[0] == target_lang:
        output_tokens = output_tokens[1:]

    translated = sp_model.decode(output_tokens)
    _debug_logger.debug("[OUTPUT RAW] %r  (nllb: %.1fms)", translated, nllb_ms)
    cache.put(cache_key, translated)
    return translated


def _reload_on_cpu():
    """Reload model on CPU after a CUDA runtime failure."""
    global translator, device_in_use
    logger.warning("CUDA runtime error during translation, reloading model on CPU...")
    try:
        translator = ctranslate2.Translator(
            model_path_global, device="cpu", compute_type="float32"
        )
        device_in_use = "cpu"
        logger.info("Model reloaded on CPU successfully")
    except Exception as e2:
        logger.error("CPU reload also failed: %s", e2)
        translator = None


def _translate_to_targets(text: str, source_lang: str, target_langs: list[str]) -> dict[str, str]:
    """Translate to all target languages, then apply glossary fixes."""
    global translator
    t_total = time.perf_counter()
    results = {}
    for tl in target_langs:
        try:
            translated = _translate_single(text, source_lang, tl)
            after_glossary = glossary.apply(text, source_lang, tl, translated)
            if after_glossary != translated:
                _debug_logger.debug("[GLOSSARY] %s: %r -> %r", tl, translated, after_glossary)
            filtered = _filter_profanity(after_glossary, tl)
            if filtered != after_glossary:
                _debug_logger.debug("[PROFANITY] %s: %r -> %r", tl, after_glossary, filtered)
            _debug_logger.debug("[FINAL] %s: %r", tl, filtered)
            results[tl] = filtered
        except Exception as e:
            err_msg = str(e)
            if "cublas" in err_msg.lower() or "cuda" in err_msg.lower():
                _reload_on_cpu()
                if translator is not None:
                    try:
                        translated = _translate_single(text, source_lang, tl)
                        results[tl] = _filter_profanity(glossary.apply(text, source_lang, tl, translated), tl)
                        continue
                    except Exception as e2:
                        logger.warning("Translation to %s failed after CPU reload: %s", tl, e2)
                        continue
            logger.warning("Translation to %s failed: %s", tl, e)
    total_ms = (time.perf_counter() - t_total) * 1000
    _debug_logger.debug("[TOTAL] %d targets in %.1fms", len(target_langs), total_ms)
    _debug_logger.debug("=" * 60)
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
                None, _translate_to_targets, req.text, req.source_lang, req.target_langs
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
            try:
                translator = ctranslate2.Translator(
                    model_path_global, device=device, compute_type="float16"
                )
                device_in_use = device
            except Exception:
                if device != "cpu":
                    logger.warning("CUDA failed, falling back to CPU")
                    translator = ctranslate2.Translator(
                        model_path_global, device="cpu", compute_type="float32"
                    )
                    device_in_use = "cpu"
                else:
                    raise

            # Load SentencePiece model
            sp_path = model_path_global.rstrip("/\\") + "/sentencepiece.bpe.model"
            sp_model = spm.SentencePieceProcessor()
            sp_model.load(sp_path)

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


@app.post("/glossary/reload")
async def reload_glossary():
    count = glossary.load(glossary_path_global)
    return {"status": "ok", "entries": count}


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
    parser = argparse.ArgumentParser(description="NLLB-200 Translation Server")
    parser.add_argument("--port", type=int, default=5090)
    parser.add_argument("--model-path", type=str, required=True)
    parser.add_argument("--device", type=str, default="cuda")
    parser.add_argument("--glossary", type=str, default="",
                        help="Path to glossary.json for post-translation fixes")
    parser.add_argument("--log-dir", type=str, default="",
                        help="Directory for pipeline-debug.log (shared log)")
    args = parser.parse_args()

    model_path_global = args.model_path

    # Set up logging to shared pipeline-debug.log
    log_dir = args.log_dir or os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
    setup_logging(log_dir)
    _debug_logger.debug(f"NLLB server started")

    # Load glossary: explicit path, or default next to server.py
    glossary_path_global = args.glossary
    if not glossary_path_global:
        glossary_path_global = os.path.join(os.path.dirname(os.path.abspath(__file__)), "glossary.json")
    glossary.load(glossary_path_global)

    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=args.port, log_level="info")
