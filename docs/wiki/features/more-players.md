# More Players

**Host only** — only the host must enable this for the whole lobby to get the effect. Joining clients do not need the mod.

The base game limits sessions to four players. This feature raises that cap so larger groups can play together.

## Configuration

### `EnableMorePlayers`

Master switch for the higher player cap. When off, the session stays at the vanilla four-player limit.

| Value | Meaning |
|---|---|
| `false` | Vanilla four-player cap |
| `true` | Use `MaxPlayers` instead |

Default: `false`

Applies live when the config reloads (no restart). Unset key uses the default.

### `MaxPlayers`

Maximum players allowed in a session, host included. The host's session enforces this cap for everyone.

| Value | Meaning |
|---|---|
| `1` | Solo |
| `2` | Host plus one other player |
| `16` (default) | Up to 16 players |

Default: `16`

Minimum `1`. No upper limit in code. Applies live to the server socket and lobby UI when More Players is enabled. Unset key uses the default.

### `EnableScalingRoundGoals`

Scales tram repair quotas by zone instead of capping at vanilla stage 5. Requires `EnableMorePlayers`.

| Value | Meaning |
|---|---|
| `false` | Vanilla stage-5 quota cap |
| `true` | Zone-based scaling (see round-goal keys below) |

Default: `true`

Takes effect on the next round-goal hook (departing maintenance or loading a save). Unset key uses the default.

### `OverrideMaxStageCount`

Allow Savegame Preparation `StartingZone` past the game's stage-table maximum (currently the last defined stage in `Stage.json`). Requires `EnableMorePlayers`.

| Value | Meaning |
|---|---|
| `false` | StartingZone is clamped to the game's max stage |
| `true` | StartingZone may use the full config range (`1`–`99`) |

Default: `true`

Only affects new-save StartingZone apply. Unset key uses the default.

### `RoundGoalBasePerZone`

Base dollar amount multiplied by the zone curve before spread and global multiplier. At defaults, zone 1 is about $200 before those adjustments.

Default: `200`

Minimum `0` (dollars). Unset key uses the default.

### `RoundGoalMoneyMultiplier`

Global multiplier on the computed tram repair quota after the zone curve.

| Value | Meaning |
|---|---|
| `0` | Quota scales to zero |
| `1.0` (default) | No change |
| `2.0` | Double the computed quota |

Default: `1.0`

Minimum `0`. Unset key uses the default.

### `RoundGoalRandomSpreadPercent`

Random ±% band around the computed center quota when departing maintenance. Loading a save uses the low bound instead of rolling.

| Value | Meaning |
|---|---|
| `0` | No random spread (fixed center) |
| `10` (default) | ±10% around center |
| `100` | Maximum spread |

Default: `10`

Range `0`–`100` (percent). Unset key uses the default.

### `RoundGoalCurveExponent`

Controls how fast quotas grow across zones. Applied as `stageCount` raised to this exponent.

| Value | Meaning |
|---|---|
| `0.1`–`0.9` | Flatter late-game growth |
| `1.0` | Linear |
| `1.1`–`2.0` | Steeper late-game growth |

Default: `0.9`

Range `0.1`–`2`. Unset key uses the default.
