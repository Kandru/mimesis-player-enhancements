# Spawn Scaling

**Host only** — only the host must enable this for the whole lobby to get the effect. Joining clients do not need the mod.

Scales dungeon monster and trap spawn budgets by type, optionally with party size. Changes inside the dungeon wait until the dungeon ends; turning the feature off applies immediately.

Each spawn type uses a general multiplier plus a per-player bonus above a shared baseline. Effective multiplier: `general + max(0, players − baseline) × perPlayer`.

## Configuration

### `EnableSpawnScaling`

Master toggle for spawn scaling. When off, spawn budgets stay vanilla.

| Value | Meaning |
|---|---|
| `false` | Disabled (vanilla spawns) |
| `true` | Scale spawns using the settings below |

Default: `false`

### `SpawnScalingBaselinePlayerCount`

Player count where per-player scaling starts. At or below this count, only each type's general multiplier applies.

| Value | Meaning |
|---|---|
| `1` | Minimum allowed |
| `4` | Vanilla four-player baseline (default) |
| Higher | Per-player bonus only applies above this count |

Default: `4`

### `MimicSpawnMultiplier`

Total mimic spawn budget across the run, including periodic spawns. `1` = vanilla, `2` = double. Stacks additively with `MimicSpawnPerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `0` | No mimics from this budget |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more mimics |

Default: `1.0`

### `MimicSpawnPerPlayerMultiplier`

Added to `MimicSpawnMultiplier` for each player above `SpawnScalingBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for mimics |
| `0.10` | +0.10 per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`

### `BossSpawnMultiplier`

Map-placed boss budget: recover inactive markers and add nav-jittered synthetic slots at load. `1` = vanilla. Stacks additively with `BossSpawnPerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `0` | No boss scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more bosses |

Default: `1.0`

### `BossSpawnPerPlayerMultiplier`

Added to `BossSpawnMultiplier` for each player above `SpawnScalingBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for bosses |
| `0.10` | +0.10 per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`

### `JakoSpawnMultiplier`

Normal-monster threat budget for ambient dungeon spawns (periodic waves). `1` = vanilla. Stacks additively with `JakoSpawnPerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `0` | No jako scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more jakos |

Default: `1.0`

### `JakoSpawnPerPlayerMultiplier`

Added to `JakoSpawnMultiplier` for each player above `SpawnScalingBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for jakos |
| `0.10` | +0.10 per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`

### `SpecialSpawnMultiplier`

Special monster budget for periodic spawns and map-placed specials. `1` = vanilla. Stacks additively with `SpecialSpawnPerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `0` | No special scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more specials |

Default: `1.0`

### `SpecialSpawnPerPlayerMultiplier`

Added to `SpecialSpawnMultiplier` for each player above `SpawnScalingBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for specials |
| `0.10` | +0.10 per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`

### `TrapSpawnMultiplier`

Map-placed trap budget: recover inactive markers at load. Traps are not given synthetic slots. `1` = vanilla. Stacks additively with `TrapSpawnPerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `0` | No trap scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more traps |

Default: `1.0`

### `TrapSpawnPerPlayerMultiplier`

Added to `TrapSpawnMultiplier` for each player above `SpawnScalingBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for traps |
| `0.10` | +0.10 per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`

### `TrapRespawnMode`

Whether cleared **map traps** respawn at their marker. This is separate from ambient monster wave timing (`AmbientMonsterWaveMode`) and boss/special bonus spawns (`BonusEncounterDelay*`).

| Value | Meaning |
|---|---|
| `Vanilla` | Use dungeon data defaults (traps typically do not respawn) |
| `Fixed` | Use `TrapRespawnDelaySeconds` |
| `Random` | Pick between min/max trap respawn delay |

Default: `Vanilla`

### `TrapRespawnDelaySeconds`

Seconds after a trap is cleared before it can respawn at the marker. Used only when `TrapRespawnMode` is `Fixed`.

Default: `5.0`

### `TrapRespawnDelayMinSeconds`

Shortest trap respawn delay. Used only when `TrapRespawnMode` is `Random`. Must be ≤ max.

Default: `5.0`

### `TrapRespawnDelayMaxSeconds`

Longest trap respawn delay. Used only when `TrapRespawnMode` is `Random`. Must be ≥ min.

Default: `30.0`

### `TrapRespawnMinPlayerDistanceMeters`

Fixed and Random modes: hold the trap respawn until no living players are within this radius (meters). Proximity is rechecked about once per second. Set `0` to spawn as soon as the delay ends.

Default: `10.0`

### `AmbientMonsterWaveMode`

Controls timing for **periodic ambient jako (normal monster) and mimic spawn waves only**. Does not affect traps, bosses, specials, or other map-placed encounters. Spawn multipliers do not shorten wave intervals — timing is independent.

| Value | Meaning |
|---|---|
| `Vanilla` | Use dungeon data defaults |
| `Fixed` | Use the fixed ambient wave seconds keys below |
| `Random` | Pick between min/max pairs for initial wait and interval |

Default: `Vanilla`

### `AmbientMonsterWaveInitialDelaySeconds`

Seconds after dungeon start before the first ambient jako/mimic spawn wave. Used only when `AmbientMonsterWaveMode` is `Fixed`.

Default: `60.0`

### `AmbientMonsterWaveInitialDelayMinSeconds`

Shortest initial wait before the first ambient jako/mimic wave. Used only when `AmbientMonsterWaveMode` is `Random`. Must be ≤ max.

Default: `30.0`

### `AmbientMonsterWaveInitialDelayMaxSeconds`

Longest initial wait before the first ambient jako/mimic wave. Used only when `AmbientMonsterWaveMode` is `Random`. Must be ≥ min.

Default: `90.0`

### `AmbientMonsterWaveIntervalSeconds`

Seconds between subsequent ambient jako/mimic spawn waves. Used only when `AmbientMonsterWaveMode` is `Fixed`.

Default: `30.0`

### `AmbientMonsterWaveIntervalMinSeconds`

Shortest interval between ambient jako/mimic waves. Used only when `AmbientMonsterWaveMode` is `Random`. Must be ≤ max.

Default: `20.0`

### `AmbientMonsterWaveIntervalMaxSeconds`

Longest interval between ambient jako/mimic waves. Used only when `AmbientMonsterWaveMode` is `Random`. Must be ≥ min.

Default: `45.0`

### `BonusEncounterDelayMinSeconds`

Shortest wait (seconds) after a map-placed **boss or special with a respawn budget** is cleared before it can respawn. Not used for traps — traps use `TrapRespawnMode` instead. Actual delay is picked randomly between min and max.

Default: `5.0`

### `BonusEncounterDelayMaxSeconds`

Longest wait for that bonus spawn delay. Not used for traps. Must be ≥ `BonusEncounterDelayMinSeconds`.

Default: `30.0`

### `BonusEncounterMinPlayerDistanceMeters`

After the delay, hold boss/special respawns until no living players are within this radius (meters) of the marker. Set `0` to spawn as soon as the delay elapses.

Default: `10.0`

### `OtherSpawnMultiplier`

Spawn multiplier for entities not in the mimic, boss, jako, special, or trap categories. `1` = vanilla. Stacks additively with `OtherSpawnPerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `0` | No other-entity scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more spawns |

Default: `1.0`

### `OtherSpawnPerPlayerMultiplier`

Added to `OtherSpawnMultiplier` for each player above `SpawnScalingBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for other spawns |
| `0.10` | +0.10 per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`
