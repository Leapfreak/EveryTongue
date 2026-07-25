"""
Translation Quality Estimation sidecar for Every Tongue.
FastAPI REST server wrapping CometKiwi (Unbabel/wmt22-cometkiwi-da) —
REFERENCE-FREE quality scoring: (source, translation) -> ~0..1 score on one
scale that is roughly comparable ACROSS language pairs (unlike chrF, which
needs a reference and has a different ruler per pair).

Model is gated on HuggingFace (free, non-commercial license) — download
happens at INSTALL time via the Download Manager with the user's HF token;
this server only loads the local cache and never needs the token itself.
"""

import argparse
import logging
import logging.handlers
import os
import queue as _queue_mod
import threading

MODEL_ID = "Unbabel/wmt22-cometkiwi-da"

logger = logging.getLogger("qe")
logger.setLevel(logging.INFO)
logger.propagate = False

# QueueHandler pattern (same as mms-tts-server): logging never blocks the
# caller; a background thread drains to a rotating file. Sidecars must log to
# FILES, never pipes (PythonSidecarHost tails the file).
_log_queue = _queue_mod.Queue(maxsize=5000)
_queue_handler = logging.handlers.QueueHandler(_log_queue)
logger.addHandler(_queue_handler)

_active_handler = logging.StreamHandler()
_active_handler.setFormatter(logging.Formatter("[QE] %(message)s"))


def _setup_file_logging(log_dir: str):
    global _active_handler
    os.makedirs(log_dir, exist_ok=True)
    handler = logging.handlers.RotatingFileHandler(
        os.path.join(log_dir, "qe-server.log"),
        maxBytes=2 * 1024 * 1024, backupCount=2, encoding="utf-8")
    handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(message)s"))
    _active_handler = handler


def _log_writer_thread():
    while True:
        try:
            record = _log_queue.get(timeout=1.0)
            _active_handler.emit(record)
        except _queue_mod.Empty:
            continue
        except Exception:
            pass


threading.Thread(target=_log_writer_thread, daemon=True).start()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="CometKiwi QE sidecar server")
    parser.add_argument("--port", type=int, default=5096)
    parser.add_argument("--cache-dir", type=str, default="",
                        help="HuggingFace cache directory holding the downloaded model")
    parser.add_argument("--log-dir", type=str, default="")
    args = parser.parse_args()

    if args.log_dir:
        _setup_file_logging(args.log_dir)
    if args.cache_dir:
        os.environ["HF_HOME"] = args.cache_dir
        # NOT offline: the checkpoint's encoder tokenizer (a separate, UNGATED
        # HF repo) is fetched lazily on first model load. The installer
        # prefetches it, but if the cache predates that (or was pruned) the
        # load can still self-heal here — it lands in the same app cache.

    logger.info(f"QE server starting on port {args.port} (cache: {args.cache_dir or 'default'})")

    from fastapi import FastAPI
    from pydantic import BaseModel

    app = FastAPI()
    _model = None
    _model_lock = threading.Lock()
    _model_error = ""

    def _load_model():
        global _model, _model_error
        with _model_lock:
            if _model is not None or _model_error:
                return
            try:
                logger.info(f"Loading {MODEL_ID} (first load takes ~1 min on CPU)...")
                from comet import download_model, load_from_checkpoint
                path = download_model(MODEL_ID)  # offline: resolves the local cache
                _model = load_from_checkpoint(path)
                _model.eval()
                logger.info("Model loaded")
            except Exception as e:
                _model_error = str(e)
                logger.error(f"Model load failed: {e}")

    class ScoreRequest(BaseModel):
        # Parallel arrays: sources[i] was translated as translations[i]
        sources: list[str]
        translations: list[str]

    @app.get("/health")
    def health():
        return {
            "status": "ok",
            "model_loaded": _model is not None,
            "model_error": _model_error,
        }

    @app.post("/load")
    def load():
        _load_model()
        return {"model_loaded": _model is not None, "model_error": _model_error}

    @app.post("/score")
    def score(req: ScoreRequest):
        _load_model()
        if _model is None:
            return {"error": _model_error or "model not loaded"}
        data = [{"src": s, "mt": t} for s, t in zip(req.sources, req.translations)]
        # CPU inference; small batch keeps memory flat. gpus=0 = CPU.
        out = _model.predict(data, batch_size=8, gpus=0, progress_bar=False)
        logger.info(f"Scored {len(data)} pairs, system score {out.system_score:.4f}")
        return {"scores": [float(s) for s in out.scores],
                "system_score": float(out.system_score)}

    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=args.port, log_level="warning")
