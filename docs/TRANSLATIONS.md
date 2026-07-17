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
    "_section": "Loot Multiplicator",
    "_description": "Adjust map loot and enemy drops; scale with player count or filter item types. Host only.",
    "EnableLootMultiplicator": {
      "title": "Enable Loot Multiplicator",
      "description": "Scale map loot and enemy death drops..."
    }
  }
}
```

- `_section` — category title (MelonPreferences and web settings nav)
- `_description` — category subtitle on the web dashboard settings panel

Select options add an `options` map keyed by the stored value.

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
