"""Pluggable STT engine registry for the live-server.

Importing this package registers all built-in online engines (their modules call
engines.common.register_engine at import time). server.py imports this package
and drives everything through the registry — it contains no engine-specific code.

To add a new online engine:
  1. Create engines/<name>.py with a streaming pipeline matching the contract in
     engines.common, and call register_engine("<key>", ...) at module level.
  2. Import it below so it registers on package import.
"""
from .common import (
    SAMPLE_RATE,
    SegmentInfo,
    TranscribeInfo,
    WordInfo,
    apply_vad_preset,
    audio_to_wav_bytes,
    create_streaming_pipeline,
    get_api_key,
    get_transcribe_fn,
    is_registered,
    online_keys,
    register_engine,
    requires_model,
    set_api_key,
)

# Register built-in online engines (order doesn't matter).
from . import google  # noqa: E402,F401
from . import speechmatics  # noqa: E402,F401

__all__ = [
    "SAMPLE_RATE", "SegmentInfo", "TranscribeInfo", "WordInfo",
    "apply_vad_preset", "audio_to_wav_bytes", "create_streaming_pipeline",
    "get_api_key", "get_transcribe_fn", "is_registered", "online_keys",
    "register_engine", "requires_model", "set_api_key",
]
