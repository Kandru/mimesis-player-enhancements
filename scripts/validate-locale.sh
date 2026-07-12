#!/usr/bin/env python3
"""Validate l10n/en.json config entries against *Config.cs registrations."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "MimesisPlayerEnhancement"
EN_JSON = ROOT / "l10n" / "en.json"
DE_JSON = ROOT / "l10n" / "de.json"

METADATA_KEYS = {"_section", "_groups", "title", "description", "options"}


def load_config_entries(path: Path) -> dict[str, set[str]]:
    data = json.loads(path.read_text(encoding="utf-8"))
    config = data.get("config", {})
    entries: dict[str, set[str]] = {}
    for section_id, section in config.items():
        if not isinstance(section, dict):
            continue
        keys = {
            key
            for key, value in section.items()
            if key not in METADATA_KEYS and isinstance(value, dict)
        }
        entries[section_id] = keys
    return entries


def resolve_section_constants(text: str) -> dict[str, str]:
    constants: dict[str, str] = {"MainCategoryId": "MimesisPlayerEnhancement"}
    for match in re.finditer(
        r'(?:internal\s+)?const\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*"([^"]+)"',
        text,
    ):
        constants[match.group(1)] = match.group(2)
    return constants


def scan_csharp_entries() -> dict[str, set[str]]:
    entries: dict[str, set[str]] = {}
    category_pattern = re.compile(
        r'(?:ModConfig\.)?CreateCategory\(\s*(?:"([^"]+)"|([A-Za-z_][A-Za-z0-9_]*))\s*\)'
    )
    entry_pattern = re.compile(
        r'Create(?:Hidden)?TrackedEntry\(\s*(?:_category|MainCategory)\s*,\s*"([^"]+)"',
        re.MULTILINE,
    )

    for path in sorted(SRC.rglob("*Config.cs")):
        if path.name in {"SparseTomlConfig.cs", "ModConfig.cs"}:
            continue

        text = path.read_text(encoding="utf-8")
        constants = resolve_section_constants(text)
        section_ids = []
        for match in category_pattern.finditer(text):
            section_id = match.group(1) or constants.get(match.group(2) or "", match.group(2) or "")
            if section_id:
                section_ids.append(section_id)

        if not section_ids:
            continue

        keys = {match.group(1) for match in entry_pattern.finditer(text)}
        if not keys:
            continue

        if len(section_ids) != 1:
            print(
                f"error: expected one CreateCategory per config file — {path} ({section_ids})",
                file=sys.stderr,
            )
            sys.exit(1)

        section_id = section_ids[0]
        entries.setdefault(section_id, set()).update(keys)

    mod_config = SRC / "Config" / "ModConfig.cs"
    if mod_config.exists():
        text = mod_config.read_text(encoding="utf-8")
        keys = {match.group(1) for match in entry_pattern.finditer(text)}
        if keys:
            entries.setdefault("MimesisPlayerEnhancement", set()).update(keys)

    return entries


def validate_locale_tree(path: Path, csharp_entries: dict[str, set[str]]) -> list[str]:
    data = json.loads(path.read_text(encoding="utf-8"))
    config = data.get("config", {})
    errors: list[str] = []

    for section_id, keys in sorted(csharp_entries.items()):
        section = config.get(section_id)
        if not isinstance(section, dict):
            errors.append(f"missing config section: {section_id}")
            continue

        if not section.get("_section"):
            errors.append(f"missing config.{section_id}._section in {path.name}")

        for key in sorted(keys):
            entry = section.get(key)
            if not isinstance(entry, dict):
                errors.append(f"missing config.{section_id}.{key} in {path.name}")
                continue
            if not entry.get("title"):
                errors.append(f"missing title for config.{section_id}.{key} in {path.name}")
            if not entry.get("description"):
                errors.append(f"missing description for config.{section_id}.{key} in {path.name}")

    for section_id, section in config.items():
        if not isinstance(section, dict):
            continue
        csharp_keys = csharp_entries.get(section_id, set())
        for key, value in section.items():
            if key in METADATA_KEYS or not isinstance(value, dict):
                continue
            if key not in csharp_keys:
                errors.append(f"orphan locale key config.{section_id}.{key} in {path.name}")

    return errors


def main() -> int:
    if not EN_JSON.exists():
        print(f"error: missing {EN_JSON}", file=sys.stderr)
        return 1

    csharp_entries = scan_csharp_entries()
    errors = validate_locale_tree(EN_JSON, csharp_entries)

    if DE_JSON.exists():
        de_data = json.loads(DE_JSON.read_text(encoding="utf-8"))
        en_data = json.loads(EN_JSON.read_text(encoding="utf-8"))
        en_config = en_data.get("config", {})
        de_config = de_data.get("config", {})

        for section_id, section in en_config.items():
            if not isinstance(section, dict):
                continue
            de_section = de_config.get(section_id, {})
            for key, value in section.items():
                if key in METADATA_KEYS or not isinstance(value, dict):
                    continue
                if key not in de_section:
                    print(f"warning: de.json missing config.{section_id}.{key}")

    if errors:
        print("locale validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print(f"locale validation passed ({sum(len(v) for v in csharp_entries.values())} config entries)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
