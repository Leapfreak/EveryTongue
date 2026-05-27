"""
MMS-TTS Sidecar for Every Tongue.
FastAPI REST server wrapping Meta's MMS-TTS (VITS) for offline speech synthesis.
Models auto-download from HuggingFace on first use per language (~100 MB each).
"""

import argparse
import io
import logging
import os
import struct
from threading import Lock

import numpy as np
import torch
from fastapi import FastAPI, Response
from pydantic import BaseModel
from transformers import VitsModel, AutoTokenizer

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
logger = logging.getLogger("mms-tts")
logger.setLevel(logging.INFO)
logger.propagate = False
_stderr_handler = logging.StreamHandler()
_stderr_handler.setLevel(logging.INFO)
_stderr_handler.setFormatter(logging.Formatter("%(message)s"))
logger.addHandler(_stderr_handler)

app = FastAPI()

# Thread-safe model cache: lang_code -> (model, tokenizer)
_models = {}
_models_lock = Lock()


class SynthesiseRequest(BaseModel):
    text: str
    language: str = "eng"


def _build_wav(pcm_bytes: bytes, sample_rate: int, num_channels: int = 1, bits: int = 16) -> bytes:
    """Build a WAV file in memory from raw PCM bytes."""
    data_size = len(pcm_bytes)
    buf = io.BytesIO()
    buf.write(b"RIFF")
    buf.write(struct.pack("<I", 36 + data_size))
    buf.write(b"WAVE")
    buf.write(b"fmt ")
    block_align = num_channels * (bits // 8)
    buf.write(struct.pack("<IHHIIHH", 16, 1, num_channels, sample_rate,
                          sample_rate * block_align, block_align, bits))
    buf.write(b"data")
    buf.write(struct.pack("<I", data_size))
    buf.write(pcm_bytes)
    return buf.getvalue()


def _load_model(lang_code: str):
    """Load or retrieve cached MMS-TTS model for a language."""
    with _models_lock:
        if lang_code in _models:
            return _models[lang_code]

        model_name = f"facebook/mms-tts-{lang_code}"
        logger.info(f"Loading MMS-TTS model: {model_name}")
        try:
            model = VitsModel.from_pretrained(model_name)
            tokenizer = AutoTokenizer.from_pretrained(model_name)
            model.eval()
            _models[lang_code] = (model, tokenizer)
            logger.info(f"Model loaded: {model_name} (sample_rate={model.config.sampling_rate})")
            return _models[lang_code]
        except Exception as e:
            logger.error(f"Failed to load {model_name}: {e}")
            return None


@app.get("/health")
def health():
    return {"status": "ok", "loaded_models": list(_models.keys())}


@app.post("/synthesise")
async def synthesise(req: SynthesiseRequest):
    # Extract 3-letter ISO 639-3 prefix
    lang_code = req.language.split("_")[0].lower()[:3]

    result = _load_model(lang_code)
    if result is None:
        return Response(status_code=404, content=f"No MMS-TTS model for language: {lang_code}")

    model, tokenizer = result

    try:
        inputs = tokenizer(req.text, return_tensors="pt")
        with torch.no_grad():
            output = model(**inputs).waveform

        # Convert to 16-bit PCM
        audio = output.float().squeeze().cpu().numpy()
        audio_int16 = (audio * 32767).clip(-32768, 32767).astype(np.int16)
        sample_rate = model.config.sampling_rate

        wav_bytes = _build_wav(audio_int16.tobytes(), sample_rate)
        return Response(content=wav_bytes, media_type="audio/wav")

    except Exception as e:
        logger.error(f"Synthesis failed for {lang_code}: {e}")
        return Response(status_code=500, content=str(e))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="MMS-TTS sidecar server")
    parser.add_argument("--port", type=int, default=5092)
    parser.add_argument("--cache-dir", type=str, default="",
                        help="HuggingFace cache directory for models")
    args = parser.parse_args()

    if args.cache_dir:
        os.environ["HF_HOME"] = args.cache_dir
        os.environ["TRANSFORMERS_CACHE"] = args.cache_dir

    import uvicorn
    logger.info(f"MMS-TTS server starting on port {args.port}")
    uvicorn.run(app, host="127.0.0.1", port=args.port, log_level="warning")
