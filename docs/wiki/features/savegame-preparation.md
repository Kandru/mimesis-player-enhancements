# Savegame Preparation

**Host only** — only the host must enable this for the whole lobby to get the effect. **Global settings only** — configure in Global Settings before creating a new save; these keys are not available as per-slot overrides.

Settings here apply **only when a new savegame is created**. They do not apply when loading an existing save or after a wipeout/session fail.

## Configuration

Section: `[MimesisPlayerEnhancement_SavegamePreparation]`

### `StartupMoney`

Starting maintenance-room currency when a new save is created. Absolute dollar amount (not a multiplier).

| Value | Meaning |
|---|---|
| `120` | Vanilla starting cash |
| `240` | Double vanilla starting cash |
| `0` | No starting cash |

Default: `120`

Does not require `EnableEconomy`. Applied when the host creates the save (lobby size at that moment does not matter).

### `StartingZone`

Zone (stage) to begin on when a new save is created. Affects the maintenance hub scene, session stage count, and the first save file.

| Value | Meaning |
|---|---|
| `1` | Vanilla — start at zone 1 |
| `5` | Begin at zone 5 (higher quotas, later hub) |

Default: `1`. Clamped to the game's maximum stage at apply time unless **More Players** is on and `OverrideMaxStageCount` is enabled (default), in which case the cap is 99. For scaled tram quotas past the vanilla stage table, also enable **More Players** → `EnableScalingRoundGoals`.
