#!/usr/bin/env python3
"""Generate docs/LOOT_ITEM_IDS.md from MIMESIS StreamingAssets masterdata."""

from __future__ import annotations

import json
import os
import re
import sys
from datetime import date
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_PATH = REPO_ROOT / "docs" / "LOOT_ITEM_IDS.md"
PATH_CONFIG = REPO_ROOT / "PathConfig.props"

ITEM_FILES: tuple[tuple[str, str], ...] = (
    ("ItemConsumable.json", "Consumable"),
    ("ItemEquipment.json", "Equipment"),
    ("ItemMiscellany.json", "Miscellany"),
)


def resolve_game_root() -> Path:
    env_path = os.environ.get("MIMESIS_PATH", "").strip()
    if env_path:
        return Path(env_path)

    if PATH_CONFIG.is_file():
        match = re.search(r"<GamePath>([^<]+)</GamePath>", PATH_CONFIG.read_text(encoding="utf-8"))
        if match:
            candidate = Path(match.group(1).strip())
            if candidate.is_dir():
                return candidate

    raise SystemExit(
        "Game install not found. Set MIMESIS_PATH or add GamePath to PathConfig.props."
    )


def load_localization(masterdata_dir: Path) -> dict[str, str]:
    loc_path = masterdata_dir / "LocalizationData.json"
    if not loc_path.is_file():
        return {}

    with loc_path.open(encoding="utf-8") as handle:
        entries = json.load(handle)

    resolved: dict[str, str] = {}
    for entry in entries:
        key = entry.get("key")
        if not key:
            continue
        english = (entry.get("en") or "").strip()
        resolved[key] = english or key
    return resolved


def load_items(masterdata_dir: Path) -> list[dict[str, object]]:
    items: list[dict[str, object]] = []
    for filename, item_type in ITEM_FILES:
        path = masterdata_dir / filename
        if not path.is_file():
            raise SystemExit(f"Missing masterdata file: {path}")

        with path.open(encoding="utf-8") as handle:
            rows = json.load(handle)

        for row in rows:
            items.append(
                {
                    "id": int(row["id"]),
                    "type": item_type,
                    "name_key": str(row.get("name") or ""),
                    "looting_object_id": str(row.get("looting_object_id") or ""),
                }
            )

    items.sort(key=lambda row: (str(row["type"]), int(row["id"])))
    return items


def render_markdown(items: list[dict[str, object]], localization: dict[str, str], game_root: Path) -> str:
    today = date.today().isoformat()
    lines = [
        "# Loot item master IDs",
        "",
        "Reference for `LootAllowlist` / `LootBlocklist` in `[MimesisPlayerEnhancement_LootMultiplicator]`.",
        "Use comma-separated **master IDs** (first column), not localization keys.",
        "",
        f"Generated from `{game_root}` masterdata on {today}.",
        "Regenerate after game updates:",
        "",
        "```bash",
        "./scripts/generate-loot-item-list.sh",
        "```",
        "",
        f"Total items: **{len(items)}** (Consumable, Equipment, Miscellany).",
        "",
        "## Quick copy lists",
        "",
        "### All master IDs",
        "",
        "```",
        ",".join(str(item["id"]) for item in items),
        "```",
        "",
        "## Full table",
        "",
        "| Master ID | Type | English name | Internal name key | Loot prefab |",
        "|-----------|------|--------------|-------------------|-------------|",
    ]

    for item in items:
        name_key = str(item["name_key"])
        english = localization.get(name_key, name_key)
        english = english.replace("|", "\\|")
        prefab = str(item["looting_object_id"]).replace("|", "\\|")
        lines.append(
            f"| {item['id']} | {item['type']} | {english} | `{name_key}` | `{prefab}` |"
        )

    lines.extend(
        [
            "",
            "## By type",
            "",
        ]
    )

    current_type = None
    for item in items:
        item_type = str(item["type"])
        if item_type != current_type:
            current_type = item_type
            lines.extend(["", f"### {item_type}", ""])

        name_key = str(item["name_key"])
        english = localization.get(name_key, name_key)
        lines.append(f"- `{item['id']}` — {english}")

    lines.append("")
    return "\n".join(lines)


def main() -> int:
    game_root = resolve_game_root()
    masterdata_dir = game_root / "MIMESIS_Data" / "StreamingAssets" / "masterdata"
    if not masterdata_dir.is_dir():
        raise SystemExit(f"Masterdata directory not found: {masterdata_dir}")

    localization = load_localization(masterdata_dir)
    items = load_items(masterdata_dir)
    markdown = render_markdown(items, localization, game_root)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(markdown, encoding="utf-8")
    print(f"Wrote {OUTPUT_PATH} ({len(items)} items)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
