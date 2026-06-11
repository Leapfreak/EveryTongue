"""VAD pipeline package for live transcription.

Public API:
    VadPipeline — orchestrates audio capture, VAD, and transcription
    VadConfig   — configuration dataclass for pipeline parameters
    SessionStats — session telemetry and statistics
"""
import dataclasses


@dataclasses.dataclass
class VadConfig:
    """Configuration for the VAD pipeline. Constructed by server.py from endpoint params."""
    device_index: int = 0
    language: str = "auto"
    beam_size: int = 5
    best_of: int = 1
    initial_prompt: str = ""
    vad_speech_threshold: float = 0.7
    vad_silence_threshold: float = 0.45
    soft_commit_ms: int = 400
    vad_silence_ms: int = 750
    vad_max_segment_s: int = 25
    vad_max_soft_segment_s: int = 8
    vad_preroll_ms: int = 400
    vad_speech_confirm_frames: int = 2
    merge_similarity_threshold: float = 0.75
    enable_interim: bool = True
    interim_interval_s: float = 3.0
    enable_sentence_split: bool = True
    # When True, the transcription worker accumulates audio across multiple
    # VAD commits and only transcribes when there's a long pause or enough
    # audio. Designed for cloud STT APIs that work best with longer audio.
    accumulate_audio: bool = False
    accumulate_pause_s: float = 1.5   # flush after this much silence
    accumulate_max_s: float = 20.0    # flush when accumulated audio exceeds this


from .pipeline import VadPipeline  # noqa: E402
from .segment import SessionStats  # noqa: E402

try:
    from .accumulating_pipeline import AccumulatingPipeline  # noqa: E402
except Exception as _accum_err:
    import traceback as _tb
    print(f"[VAD] WARNING: AccumulatingPipeline import failed: {_accum_err}", flush=True)
    _tb.print_exc()
    AccumulatingPipeline = None

__all__ = ["VadPipeline", "AccumulatingPipeline", "VadConfig", "SessionStats"]
