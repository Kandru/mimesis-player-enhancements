#!/usr/bin/env python3
"""Generate docs/LOOT_ITEM_IDS.md from MIMESIS StreamingAssets masterdata."""

from __future__ import annotations

import json
import os
import re
import sys
from collections import Counter
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

TABLE_COLUMNS = ("id", "name", "looting_object_id")

# Gameplay-relevant fields only — effects, quantities, durability, economy, variants.
_SHARED_INTERESTING_KEYS = frozenset(
    {
        "is_promotion_item",
        "price_for_sell_max",
        "price_for_sell_min",
        "weight",
    }
)
INTERESTING_KEYS_BY_TYPE: dict[str, frozenset[str]] = {
    "Consumable": _SHARED_INTERESTING_KEYS
    | {
        "actions",
        "bullet_type",
        "consume_type",
        "default_provide_count",
        "max_stack_count",
    },
    "Equipment": _SHARED_INTERESTING_KEYS
    | {
        "blackout_rate",
        "charge_affix",
        "dec_gauge_initial_only",
        "dec_gauge_per_use",
        "dec_gauge_use_period",
        "default_provide_gauge",
        "equip_type",
        "hand_weapon_type",
        "handheld_abnormal_by_gauge",
        "handheld_abnormal_id",
        "handheld_auraskill_by_gauge",
        "handheld_auraskill_id",
        "inc_gauge_when_move",
        "is_two_hand",
        "item_upgrade_cost",
        "item_upgradedid",
        "max_durability",
        "max_gauge",
        "min_durability",
        "overflow_price",
        "price_inc_per_gauge",
        "skill_gauge_on",
        "skill_list",
        "skill_reload",
        "stat_list",
        "use_bonus_item",
        "use_charge",
        "use_destroy_by_gauge",
        "use_item_upgrade",
        "visible_durability_count",
        "visible_gauge_count",
    },
    "Miscellany": _SHARED_INTERESTING_KEYS
    | {
        "accessory_group",
        "deteriorate_item",
        "forbid_change",
        "use_bonus_item",
        "use_vending_machine_exchange",
    },
}


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


def load_items_by_type(masterdata_dir: Path) -> dict[str, list[dict[str, object]]]:
    items_by_type: dict[str, list[dict[str, object]]] = {}

    for filename, item_type in ITEM_FILES:
        path = masterdata_dir / filename
        if not path.is_file():
            raise SystemExit(f"Missing masterdata file: {path}")

        with path.open(encoding="utf-8") as handle:
            rows = json.load(handle)

        typed_rows = [dict(row) for row in rows]
        typed_rows.sort(key=lambda row: int(row["id"]))
        items_by_type[item_type] = typed_rows

    return items_by_type


def escape_cell(value: str) -> str:
    return value.replace("|", "\\|").replace("\n", " ")


def format_property_value(value: object) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, list):
        if not value:
            return "[]"
        return "[" + ", ".join(format_property_value(entry) for entry in value) + "]"
    if value is None:
        return ""
    return escape_cell(str(value))


def compute_type_defaults(
    rows: list[dict[str, object]],
    interesting_keys: frozenset[str],
) -> dict[str, object]:
    defaults: dict[str, object] = {}
    if not rows:
        return defaults

    for key in sorted(interesting_keys):
        counts = Counter(
            json.dumps(row.get(key), sort_keys=True, ensure_ascii=False) for row in rows
        )
        defaults[key] = json.loads(counts.most_common(1)[0][0])
    return defaults


def format_properties(
    row: dict[str, object],
    type_defaults: dict[str, object],
    interesting_keys: frozenset[str],
) -> str:
    parts: list[str] = []
    for key in sorted(interesting_keys):
        value = row.get(key)
        if value == type_defaults.get(key):
            continue
        parts.append(f"`{key}`={format_property_value(value)}")
    return "; ".join(parts) if parts else "—"


def render_type_table(
    item_type: str,
    rows: list[dict[str, object]],
    localization: dict[str, str],
) -> list[str]:
    interesting_keys = INTERESTING_KEYS_BY_TYPE[item_type]
    type_defaults = compute_type_defaults(rows, interesting_keys)
    lines = [
        "| Master ID | English name | Name key | Loot prefab | Key properties |",
        "|-----------|--------------|----------|-------------|----------------|",
    ]

    for row in rows:
        master_id = int(row["id"])
        name_key = str(row.get("name") or "")
        english = escape_cell(localization.get(name_key, name_key))
        prefab = escape_cell(str(row.get("looting_object_id") or ""))
        properties = format_properties(row, type_defaults, interesting_keys)
        lines.append(
            f"| {master_id} | {english} | `{name_key}` | `{prefab}` | {properties} |"
        )

    return lines


def render_markdown(
    items_by_type: dict[str, list[dict[str, object]]],
    localization: dict[str, str],
    game_root: Path,
) -> str:
    today = date.today().isoformat()
    type_order = [item_type for _, item_type in ITEM_FILES]
    counts = {item_type: len(items_by_type[item_type]) for item_type in type_order}
    total = sum(counts.values())
    count_summary = ", ".join(f"{item_type} **{counts[item_type]}**" for item_type in type_order)

    all_ids = [
        str(int(row["id"]))
        for item_type in type_order
        for row in items_by_type[item_type]
    ]

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
        f"Total items: **{total}** — {count_summary}.",
        "",
        "Each section lists every item from game masterdata (`ItemConsumable.json`, `ItemEquipment.json`,",
        "`ItemMiscellany.json`). **Key properties** lists gameplay-relevant fields that differ from the",
        "type default — effect strength, ammo/stack counts, durability, sell price, upgrade path, etc.",
        "(`—` = no distinguishing values among those fields.)",
        "",
        "## Quick copy — all master IDs",
        "",
        "```",
        ",".join(all_ids),
        "```",
        "",
    ]

    for item_type in type_order:
        rows = items_by_type[item_type]
        lines.extend(
            [
                f"## {item_type} ({len(rows)})",
                "",
                *render_type_table(item_type, rows, localization),
                "",
            ]
        )

    return "\n".join(lines)


def main() -> int:
    game_root = resolve_game_root()
    masterdata_dir = game_root / "MIMESIS_Data" / "StreamingAssets" / "masterdata"
    if not masterdata_dir.is_dir():
        raise SystemExit(f"Masterdata directory not found: {masterdata_dir}")

    localization = load_localization(masterdata_dir)
    items_by_type = load_items_by_type(masterdata_dir)
    markdown = render_markdown(items_by_type, localization, game_root)

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(markdown, encoding="utf-8")

    total = sum(len(rows) for rows in items_by_type.values())
    breakdown = ", ".join(
        f"{item_type}={len(items_by_type[item_type])}" for _, item_type in ITEM_FILES
    )
    print(f"Wrote {OUTPUT_PATH} ({total} items: {breakdown})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
