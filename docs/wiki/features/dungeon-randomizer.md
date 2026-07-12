# Dungeon Randomizer

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_DungeonRandomizer`](../CONFIG.md#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer)

Shakes up repeat runs by randomizing which dungeon the tram picks, the layout inside that dungeon, which map variant loads, and the procedural seed that shapes rooms. Off by default — each layer has its own toggle for partial randomization.

Rolls use a scene snapshot captured at dungeon enter — changes during an active scene are deferred until that scene ends.

## Dungeon pick

`RandomizeDungeonPick` overrides tram dungeon master ID selection.

`DungeonPickPoolMode`:

- `WidenVanilla` — keep vanilla cycle weights; optionally allow repeats sooner via `IgnoreDungeonExcludeList`.
- `AllActiveUniform` — pick uniformly from all active dungeons.

`DungeonAllowlist` / `DungeonBlocklist` filter the pool (allowlist wins when non-empty).

## Layout flow

`RandomizeLayoutFlow` picks DunGen layout flows uniformly instead of weighted vanilla rolls.

## Map variant

`RandomizeMapVariant` picks map variants uniformly from each dungeon's `MapIDs`.

## Procedural seed

`RandomizeDungeonSeed` replaces the procedural dungeon seed when a dungeon is chosen, reshaping room generation.

**Full config keys →** [Dungeon Randomizer](../CONFIG.md#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer)
