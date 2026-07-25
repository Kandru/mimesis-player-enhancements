# Spawn Scaling

**Host only** — only the host must enable this for the whole lobby to get the effect. Joining clients do not need the mod.

cales dungeon monster and trap spawn budgets by type, optionally with party size. Changes inside the dungeon wait until the dungeon ends; turning the feature off applies immediately.

## Configuration

### `EnableSpawnScaling`

Master toggle for spawn scaling. When off, spawn budgets stay vanilla.

| Value | Meaning |
|---|---|
| `false` | Disabled (vanilla spawns) |
| `true` | Scale spawns using the settings below |

Default: `false`

### `SpawnScalingPlayerCountScaleRate`

Extra multiplier per player above 4 when an **Auto Scale … By Player Count** toggle is on. Stacks with each type's multiplier. Formula: `1 + (players − 4) × rate` (no bonus at 4 or fewer players). Set `0.25` to approximate the old `players / 4` curve.

| Value | Meaning |
|---|---|
| `0` | No player-count bonus |
| `0.10` | +10% per extra player (default) |
| `≥ 0` | Allowed; higher = steeper scaling |

Default: `0.10`

### `AutoScaleMimicSpawnsByPlayerCount`

Whether mimic spawns get the player-count bonus from `SpawnScalingPlayerCountScaleRate`. Stacks with `MimicSpawnMultiplier`.

| Value | Meaning |
|---|---|
| `true` | Scale mimics with party size |
| `false` | Mimics ignore player count |

Default: `true`

### `MimicSpawnMultiplier`

Total mimic spawn budget across the run, including periodic spawns. `1` = vanilla, `2` = double.

| Value | Meaning |
|---|---|
| `0` | No mimics from this budget |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more mimics |

Default: `1.0`

### `AutoScaleBossSpawnsByPlayerCount`

Whether boss spawns get the player-count bonus. Stacks with `BossSpawnMultiplier`.

| Value | Meaning |
|---|---|
| `true` | Scale bosses with party size |
| `false` | Bosses ignore player count |

Default: `true`

### `BossSpawnMultiplier`

Map-placed boss budget: unused alternate markers at load, plus bonus encounters after a kill. `1` = vanilla.

| Value | Meaning |
|---|---|
| `0` | No boss scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more bosses |

Default: `1.0`

### `AutoScaleJakoSpawnsByPlayerCount`

Whether ambient jako spawns get the player-count bonus. Stacks with `JakoSpawnMultiplier`.

| Value | Meaning |
|---|---|
| `true` | Scale jakos with party size |
| `false` | Jakos ignore player count |

Default: `true`

### `JakoSpawnMultiplier`

Normal-monster threat budget for ambient dungeon spawns (periodic waves). `1` = vanilla.

| Value | Meaning |
|---|---|
| `0` | No jako scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more jakos |

Default: `1.0`

### `AutoScaleSpecialSpawnsByPlayerCount`

Whether special spawns get the player-count bonus. Stacks with `SpecialSpawnMultiplier`.

| Value | Meaning |
|---|---|
| `true` | Scale specials with party size |
| `false` | Specials ignore player count |

Default: `true`

### `SpecialSpawnMultiplier`

Special monster budget for periodic spawns and map-placed specials. `1` = vanilla.

| Value | Meaning |
|---|---|
| `0` | No special scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more specials |

Default: `1.0`

### `AutoScaleTrapSpawnsByPlayerCount`

Whether trap spawns get the player-count bonus. Stacks with `TrapSpawnMultiplier`.

| Value | Meaning |
|---|---|
| `true` | Scale traps with party size |
| `false` | Traps ignore player count |

Default: `true`

### `TrapSpawnMultiplier`

Map-placed trap budget: unused alternate markers at load, plus bonus encounters after trigger or kill. `1` = vanilla.

| Value | Meaning |
|---|---|
| `0` | No trap scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more traps |

Default: `1.0`

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

Shortest wait (seconds) after a scaled map-placed **boss or special** is cleared before the next bonus encounter can spawn. Not used for traps — traps use `TrapRespawnMode` instead. Actual delay is picked randomly between min and max.

Default: `5.0`

### `BonusEncounterDelayMaxSeconds`

Longest wait for that bonus spawn delay. Not used for traps. Must be ≥ `BonusEncounterDelayMinSeconds`.

Default: `30.0`

### `BonusEncounterMinPlayerDistanceMeters`

After the delay, hold boss/special bonus spawns until no living players are within this radius (meters) of the marker. Set `0` to spawn as soon as the delay elapses.

Default: `10.0`

### `AutoScaleOtherSpawnsByPlayerCount`

Whether spawns outside mimic, boss, jako, special, and trap categories get the player-count bonus. Stacks with `OtherSpawnMultiplier`.

| Value | Meaning |
|---|---|
| `true` | Scale other spawns with party size |
| `false` | Other spawns ignore player count |

Default: `true`

### `OtherSpawnMultiplier`

Spawn multiplier for entities not in the mimic, boss, jako, special, or trap categories. `1` = vanilla.

| Value | Meaning |
|---|---|
| `0` | No other-entity scaling |
| `1` | Vanilla |
| `≥ 0` | Allowed; higher = more spawns |

Default: `1.0`
