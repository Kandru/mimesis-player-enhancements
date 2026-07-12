# Spawn Scaling

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_SpawnScaling`](../CONFIG.md#spawn-scaling--mimesisplayerenhancement_spawnscaling)

Control how busy dungeons feel by changing how many enemies and traps appear. Set fixed multipliers per spawn type, scale automatically when more players join, or mix both.

Each **Auto Scale … By Player Count** toggle stacks with its per-type multiplier using `SpawnScalingPlayerCountScaleRate` per player above 4 (`0.10` = +10% per extra player).

## Mimic spawns

`MimicSpawnMultiplier` scales total mimic spawn budget across the run, including periodic spawns (`1` = vanilla, `2` = double). `AutoScaleMimicSpawnsByPlayerCount` adds player-count scaling on top.

## Boss spawns

`BossSpawnMultiplier` affects map-placed bosses: unused alternate markers plus bonus encounters after kill. `AutoScaleBossSpawnsByPlayerCount` stacks player-count scaling.

## Jako spawns

`JakoSpawnMultiplier` scales the normal-monster threat budget for ambient dungeon spawns. `AutoScaleJakoSpawnsByPlayerCount` stacks player-count scaling.

## Special spawns

`SpecialSpawnMultiplier` scales special monster budget for periodic spawns and map-placed specials. `AutoScaleSpecialSpawnsByPlayerCount` stacks player-count scaling.

## Trap spawns

`TrapSpawnMultiplier` affects map-placed traps: unused alternate markers plus bonus encounters after trigger/kill. `AutoScaleTrapSpawnsByPlayerCount` stacks player-count scaling.

## Other spawns

`OtherSpawnMultiplier` covers entities not in mimic/boss/jako/special/trap categories. `AutoScaleOtherSpawnsByPlayerCount` stacks player-count scaling.

## Periodic spawn timing

`PeriodicSpawnWaitMode` controls initial delay and interval between ambient jako/mimic waves:

- `Vanilla` — dungeon data defaults.
- `Fixed` — use `InitialPeriodicSpawnWaitSeconds` and `PeriodicSpawnIntervalSeconds`.
- `Random` — pick between min/max pairs for initial wait and interval.

Spawn multipliers no longer shorten wave intervals — timing is independent.

## Map-placed encounters

After a map-placed enemy or trap is cleared, bonus encounters can spawn at that marker:

- `MapPlacedEncounterDelayMinSeconds` / `MaxSeconds` — random wait before next spawn.
- `MapPlacedEncounterMinPlayerDistanceMeters` — hold spawn until no living players are within this radius.

**Full config keys →** [Spawn Scaling](../CONFIG.md#spawn-scaling--mimesisplayerenhancement_spawnscaling)
