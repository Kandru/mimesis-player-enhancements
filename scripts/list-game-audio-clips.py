#!/usr/bin/env python3
"""Debug helper: list AudioClip names containing tram/horn/stop."""

from __future__ import annotations

import sys
from pathlib import Path

import UnityPy


def main() -> int:
    data_dir = Path(sys.argv[1] if len(sys.argv) > 1 else "/game/MIMESIS_Data")
    for asset_path in data_dir.glob("**/*.assets"):
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

            name = getattr(data, "name", "") or getattr(data, "m_Name", "")
            if not name:
                continue
            lower = name.lower()
            if "tram" in lower or "horn" in lower or "stop" in lower:
                samples = getattr(data, "samples", None)
                print(f"{name}\t{type(samples).__name__}\t{asset_path.name}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
