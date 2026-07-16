#!/usr/bin/env python3
import os
import sys
import tempfile
from pathlib import Path

import UnityPy


def main() -> int:
    assets = sys.argv[1] if len(sys.argv) > 1 else "/game/MIMESIS_Data/sharedassets0.assets"
    env = UnityPy.load(assets)
    for obj in env.objects:
        if obj.type.name != "AudioClip":
            continue
        data = obj.read()
        name = getattr(data, "name", "") or getattr(data, "m_Name", "")
        if name != "tram_stop":
            continue
        print("name", name)
        print("samples type", type(data.samples))
        if isinstance(data.samples, dict):
            print("samples keys", list(data.samples.keys()))
        print("extension", getattr(data, "extension", None))
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "out"
            out.mkdir()
            if hasattr(data, "export"):
                try:
                    exported = data.export(str(out))
                    print("export returned", exported)
                except Exception as exc:
                    print("export err", exc)
            for path in Path(tmp).rglob("*"):
                if path.is_file():
                    print("file", path, path.stat().st_size)
        return 0
    print("tram_stop not found")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
