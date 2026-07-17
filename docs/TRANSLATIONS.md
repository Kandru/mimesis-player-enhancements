# Translations

All user-facing strings for the mod and web dashboard live in a single source tree at [`l10n/`](../l10n/). English (`en.json`) is the canonical source; other languages mirror the same structure.

## Layout

Each locale file is one JSON document with top-level namespaces:

| Namespace | Used by |
|-----------|---------|
| `config` | MelonPreferences titles/descriptions, web dashboard settings |
| `dashboard` | Web dashboard UI |
| `announce`, `stats`, `joinanytime`, etc. | In-game mod messages |

Config keys stay English (for example `EnableLootMultiplicator`). Only display strings are translated.

Config entry shape:

```json
"config": {
  "MimesisPlayerEnhancement_LootMultiplicator": {
    "_section": "Loot Multiplier",
    "_description": "Adjust map loot and enemy drops; scale with player count or filter item types. Host only.",
    "EnableLootMultiplicator": {
      "title": "Enable Loot Multiplier",
      "description": "Scale map loot and enemy death drops..."
    }
  }
}
```

- `_section` — category title (MelonPreferences and web settings nav)
- `_description` — category subtitle on the web dashboard settings panel
- `_groups` — optional map of entry-group labels for the web dashboard (`groupId` → display string). Group IDs come from `ModConfigEntryUiHints.AssignEntryGroups` (explicit maps or auto-inferred from entry keys). When missing, the web UI falls back to the raw `groupId`.

```json
"MimesisPlayerEnhancement_Ui": {
  "_section": "User Interface",
  "_description": "Extended save picker and local HUD tweaks. Client-only.",
  "_groups": {
    "fpsUi": "FPS UI",
    "roundStartSound": "Dungeon landing sound"
  },
  "EnableFpsUi": {
    "title": "Enable FPS UI",
    "description": "Replace the top-left health bar with numeric readouts."
  }
}
```

## Copy style

Config titles and descriptions are user-facing. Keep them short and plain:

- **Titles:** 2–5 words, sentence case. Keep `Enable …` for master toggles.
- **Descriptions:** 1–2 short sentences. Lead with what the player or host will see or control.
- **Avoid:** implementation jargon (`scrap-value budget`, `master ID`, `embedded`), literal `…` in auto-scale descriptions, and repo-internal details unless needed (e.g. `docs/LOOT_ITEM_IDS.md` on allow/block lists).
- **Keep:** `Host only.` / client-only notes, numeric baselines (`1 = vanilla`), and option labels for enum values.
- **Sounds and themes:** describe round-start sounds and loading-screen themes as things users can apply — not as “embedded” assets.

When adding a new entry group in `ModConfigEntryUiHints.cs`, add matching `config.{section}._groups.{groupId}` strings in every locale file.

## Build pipeline

1. Edit files under `l10n/` only.
2. `make check` validates every `*Config.cs` entry has `title` and `description` in `en.json` (via Docker).

`make debug` / `make release` embed `l10n/*.json` directly and run validation automatically.

## Adding a language

1. Copy `l10n/en.json` to `l10n/<lang>.json` (for example `fr.json`).
2. Translate string values only; keep keys, section IDs, and option value keys unchanged.
3. Run `make check` and `make debug`.

No C# or web code changes are required — embedded locale discovery picks up new `l10n/*.json` files at build time.

## Contributing via GitHub

1. Branch from `main`.
2. Update `l10n/en.json` when adding config entries in code.
3. Update translation files for languages you can provide.
4. Run `make check` and `make debug`.
5. Open a pull request.

The JSON structure is compatible with future Weblate or Crowdin integration if you add a translation platform later.

## Runtime behavior

- `ModL10n` resolves strings from embedded locale JSON.
- `CreateTrackedEntry` reads config titles/descriptions from locale files at registration time.
- `ModConfigLocalization` refreshes MelonPreferences metadata when the game language changes and re-saves `MimesisPlayerEnhancement.cfg` so comments stay translated.
- The web dashboard loads the same JSON via `GET /api/locale/{lang}`.
