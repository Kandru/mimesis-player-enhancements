#!/usr/bin/env python3
"""Extract tram_stop (dungeon landing) audio from MIMESIS Unity assets."""

from __future__ import annotations

import argparse
import os
import sys
from pathlib import Path


def resolve_game_data_dir(game: str | None, data_dir: str | None) -> Path:
    if data_dir:
        path = Path(data_dir).expanduser().resolve()
        if not path.is_dir():
            raise SystemExit(f"Data directory not found: {path}")
        return path

    game_path = game or os.environ.get("MIMESIS_PATH") or os.environ.get("GAME_PATH")
    if not game_path:
        raise SystemExit(
            "Set MIMESIS_PATH, pass --game, or pass --data-dir pointing at MIMESIS_Data."
        )

    path = Path(game_path).expanduser().resolve() / "MIMESIS_Data"
    if not path.is_dir():
        raise SystemExit(f"MIMESIS_Data not found under: {game_path}")
    return path


def iter_asset_files(data_dir: Path):
    for pattern in ("**/*.assets", "**/*.bundle", "**/*.resS"):
        yield from data_dir.glob(pattern)


def name_matches(candidate: str, target: str) -> bool:
    normalized = candidate.replace("\\", "/").lower()
    target_lower = target.lower()
    base = Path(normalized).name
    return (
        base == target_lower
        or base.startswith(target_lower + ".")
        or target_lower in normalized
    )


def try_extract_with_unitypy(data_dir: Path, names: list[str]) -> tuple[bytes, str, str] | None:
    try:
        import UnityPy  # type: ignore
    except ImportError as exc:
        raise SystemExit(
            "UnityPy is required. Install with: pip install UnityPy"
        ) from exc

    for asset_path in iter_asset_files(data_dir):
        try:
            env = UnityPy.load(str(asset_path))
        except Exception:
            continue

        for obj in env.objects:
            if obj.type.name != "AudioClip":
                continue
            try:
                data = obj.read()
            except Exception:
                continue

            clip_name = getattr(data, "name", "") or getattr(data, "m_Name", "") or ""
            if not any(name_matches(clip_name, name) for name in names):
                continue

            samples = extract_audio_bytes(data)
            if not samples:
                continue

            ext = ".wav"
            if hasattr(data, "extension") and data.extension:
                ext = data.extension if data.extension.startswith(".") else f".{data.extension}"

            return samples, ext, clip_name or names[0]

    return None


def extract_audio_bytes(data) -> bytes | None:
    samples = getattr(data, "samples", None)
    if isinstance(samples, dict):
        for value in samples.values():
            if isinstance(value, (bytes, bytearray)):
                return bytes(value)
            if hasattr(value, "tobytes"):
                return bytes(value.tobytes())
    if samples is not None:
        if isinstance(samples, (bytes, bytearray)):
            return bytes(samples)
        if hasattr(samples, "tobytes"):
            return bytes(samples.tobytes())

    resource = getattr(data, "m_Resource", None)
    if resource is not None:
        source = getattr(resource, "m_Source", None)
        if isinstance(source, (bytes, bytearray)):
            return bytes(source)
        if hasattr(source, "tobytes"):
            return bytes(source.tobytes())

    if hasattr(data, "save"):
        import tempfile

        with tempfile.TemporaryDirectory() as tmp:
            try:
                data.save(tmp)
            except TypeError:
                data.save(path=tmp)
            for path in Path(tmp).rglob("*"):
                if path.is_file() and path.suffix.lower() in {".wav", ".ogg", ".mp3", ".fsb"}:
                    return path.read_bytes()

    return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Export dungeon landing stinger from MIMESIS assets.")
    parser.add_argument("--game", help="MIMESIS install root")
    parser.add_argument("--data-dir", help="Path to MIMESIS_Data")
    parser.add_argument(
        "--output",
        default="src/MimesisPlayerEnhancement/Assets/RoundStartSound/vanilla.wav",
        help="Output file path",
    )
    parser.add_argument(
        "--name",
        default="Sound_UI_TramStopBGM_01",
        help="Primary audio name to search for",
    )
    parser.add_argument(
        "--fallback-name",
        default="Sound_UI_TramStopBGM",
        help="Fallback audio name if primary is missing",
    )
    args = parser.parse_args()

    data_dir = resolve_game_data_dir(args.game, args.data_dir)
    output_path = Path(args.output).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

    result = try_extract_with_unitypy(data_dir, [args.name])
    used_fallback = False
    if result is None:
        print(f"Warning: '{args.name}' not found; trying fallback '{args.fallback_name}'.", file=sys.stderr)
        result = try_extract_with_unitypy(data_dir, [args.fallback_name])
        used_fallback = result is not None

    if result is None:
        raise SystemExit(
            f"Could not find audio named '{args.name}' (or fallback '{args.fallback_name}') under {data_dir}."
        )

    samples, ext, clip_name = result
    if ext.lower() == ".fsb" and samples[:4] == b"RIFF":
        ext = ".wav"

    if ext.lower() != ".wav":
        final_path = output_path.with_suffix(ext.lower())
    else:
        final_path = output_path

    final_path.write_bytes(samples)
    print(f"Exported '{clip_name}' ({len(samples)} bytes) -> {final_path}")
    if used_fallback:
        print(
            f"Note: used fallback '{args.fallback_name}'. Re-export after confirming tram_stop exists.",
            file=sys.stderr,
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
