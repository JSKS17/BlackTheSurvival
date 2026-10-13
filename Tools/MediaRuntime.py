"""Locate one shared FFmpeg executable without depending on disposable Temp data."""
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]


def ffmpeg_path():
    local = ROOT / "Tools/.runtime/ffmpeg.exe"
    if local.is_file():
        return local
    installed = shutil.which("ffmpeg")
    if installed:
        return Path(installed)
    try:
        import imageio_ffmpeg
        return Path(imageio_ffmpeg.get_ffmpeg_exe())
    except ImportError as error:
        raise RuntimeError("Install FFmpeg on PATH, install imageio-ffmpeg, or put ffmpeg.exe in Tools/.runtime/.") from error
