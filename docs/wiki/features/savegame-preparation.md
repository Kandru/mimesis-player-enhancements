# Savegame Preparation

**Host only** — only the host must enable this for the whole lobby to get the effect. **Global settings only** — configure in Global Settings before creating a new save; these keys are not available as per-slot overrides.

Settings here apply **only when a new savegame is created**. They do not apply when loading an existing save or after a wipeout/session fail.

## Configuration

Section: `[MimesisPlayerEnhancement_SavegamePreparation]`

### `StartupMoneyMultiplier`

Scales starting maintenance-room currency when a new save is created.

| Value | Meaning |
|---|---|
| `1` | Vanilla starting cash |
| `2` | Double starting cash |
| `0` | No starting cash |

Default: `1`

Does not require `EnableEconomy`.

### `AutoScaleStartupMoneyByPlayerCount`

When on, startup money also uses `EconomyPlayerCountScaleRate` from the Economy section for players above 4.

| Value | Meaning |
|---|---|
| `false` | Startup money uses only `StartupMoneyMultiplier` |
| `true` | Stack player-count scaling on startup money |

Default: `true`

### `StartingZone`

Zone (stage) to begin on when a new save is created. Affects the maintenance hub scene, session stage count, and the first save file.

| Value | Meaning |
|---|---|
| `1` | Vanilla — start at zone 1 |
| `5` | Begin at zone 5 (higher quotas, later hub) |

Default: `1`. Clamped to the game's maximum stage at apply time.
