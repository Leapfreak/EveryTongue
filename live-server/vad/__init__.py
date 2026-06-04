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
    initial_prompt: str = ""
    vad_speech_threshold: float = 0.6
    vad_silence_threshold: float = 0.4
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


from .pipeline import VadPipeline  # noqa: E402
from .segment import SessionStats  # noqa: E402

__all__ = ["VadPipeline", "VadConfig", "SessionStats"]
