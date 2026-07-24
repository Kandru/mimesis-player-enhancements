# Dungeon Randomizer

Randomizes which dungeon the tram picks, which map variant loads, and — optionally — the procedural layout seed (map flavor). Only the host must enable this for the whole lobby to get the effect; joining clients do not need the mod. The host applies pick, variant, and seed before departure; all players receive the same choices over the network.

Settings use a scene snapshot captured when you enter a gameplay scene (maintenance, tram, dungeon, deathmatch). Changes during an active scene are deferred until that scene ends. Turning the master toggle off applies immediately.

## Configuration

### `EnableDungeonRandomizer`

Master toggle for all three layers: dungeon pick, map variant, and map flavor. When off, vanilla tram rolls and seeds apply.

| Value | Meaning |
|---|---|
| `false` | Vanilla dungeon selection (default) |
| `true` | Apply enabled sub-layers below |

Default: `false`

### `RandomizeDungeonPick`

Overrides which dungeon master ID the tram roll selects. Has no effect when the master toggle is off.

| Value | Meaning |
|---|---|
| `false` | Keep vanilla tram dungeon pick |
| `true` | Apply pool mode, allowlist/blocklist, and reroll exclude rules |

Default: `true`

### `DungeonPickPoolMode`

How the tram builds its dungeon pool when `RandomizeDungeonPick` is on. Invalid values reset to `WidenVanilla` at load.

| Value | Meaning |
|---|---|
| `WidenVanilla` | Keep vanilla cycle weights when the vanilla result is still eligible; otherwise pick from the filtered active pool |
| `AllActiveUniform` | Pick uniformly from all active dungeons (ignores the cycle table) |

Default: `WidenVanilla`

### `DungeonAllowlist`

Comma-separated dungeon master IDs. When non-empty, only listed IDs are eligible — allowlist wins over blocklist. Empty means no allowlist filter. Stored as raw IDs, not display names.

Default: `""` (unset = no allowlist)

### `DungeonBlocklist`

Comma-separated dungeon master IDs to exclude from the pool. Applied in both pool modes. Ignored when allowlist is non-empty.

Default: `""` (unset = no blocklist)

### `IgnoreDungeonExcludeList`

On a tram **reroll** only (not the first roll), clears the game's recent-dungeon exclude list before picking. Requires master toggle on, `RandomizeDungeonPick` on, and `DungeonPickPoolMode` = `WidenVanilla`. The first tram pick still respects vanilla excludes.

| Value | Meaning |
|---|---|
| `false` | Rerolls keep the recent-dungeon exclude list |
| `true` | Rerolls ignore recent-dungeon excludes (when gates above are met) |

Default: `true`

### `RandomizeMapVariant`

Before departure, the host picks a map variant uniformly from the chosen dungeon's `MapIDs`. The result syncs to all players. Has no effect when the master toggle is off.

| Value | Meaning |
|---|---|
| `false` | Keep vanilla map variant |
| `true` | Uniform random pick from available map IDs |

Default: `true`

### `DungeonSeedFlavor`

Controls how the host picks the procedural dungeon seed sent at departure. `Vanilla` uses the game's normal roll. Any other value replaces the seed with a random pick from up to **500** pre-tested seeds per DunGen layout flow (baked into the mod, not editable in settings). Layout shape only — not enemy counts, loot, weather, or quota. Invalid flavor names reset to `Vanilla` at load. Web dashboard labels come from l10n; config/TOML use the enum name.

Multiplayer: clients do not need the mod but must have matching game data to build the same layout from the seed. If no pool exists for a flow, or a multi-flow dungeon cannot match the same flow at load, the host falls back to the vanilla seed and logs a warning. On rare DunGen failure the game retries with `Seed++` up to **3** times (4 attempts total); the curated seed may be abandoned.

| Value | Meaning |
|---|---|
| `Vanilla` | Game's normal random seed — no curated bias |
| `Compact` | Fewest total rooms |
| `Expansive` | Most total rooms within flow limits |
| `ShortMainPath` | Shortest start-to-goal main path |
| `LongMainPath` | Longest start-to-goal main path |
| `Sprawling` | Largest physical footprint (spread-out rooms) |
| `Dense` | Smallest footprint per room (tight cluster) |
| `Cramped` | Many rooms packed into the smallest bounds per room |
| `Linear` | Long main path relative to branch rooms |
| `MinimalBranches` | Fewest branch rooms |
| `Branching` | Most branch rooms and branch depth |
| `BroadBranches` | Many shallow side wings (wide, not deep) |
| `Deep` | Deepest branch chains off the main path |
| `Open` | Most doorway connections between rooms |
| `Maze` | Most unused doorways and cul-de-sacs |
| `Loopy` | Highest connections per room; loop-back routes |
| `DeadEnds` | Many stub corridors per room |
| `TightCorridor` | Compact, then most linear among those |
| `Labyrinth` | Expansive, then most branchy and deep |
| `Honeycomb` | Most connections per unit space, favouring compact area |
| `WideOpen` | Most connections across a large footprint |
| `StableCompact` | Most reliable generation, then fewest rooms |
| `DeepMaze` | Deepest branches, then most dead-end doorways |
| `Reliable` | Fewest DunGen generation retries |
| `Balanced` | Closest to median room count, branching, and connectivity |

Default: `Vanilla`

**Full config keys →** [Dungeon Randomizer](../CONFIG.md#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer)
