# Dungeon Randomizer

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_DungeonRandomizer`](../CONFIG.md#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer)

Shakes up repeat runs by randomizing which dungeon the tram picks, which map variant loads, and — optionally — the procedural **map flavor** (room layout shape). Off by default; each layer has its own toggle.

Settings use a scene snapshot captured at dungeon enter. Changes during an active scene are deferred until that scene ends.

## How dungeon layout works

MIMESIS builds each dungeon **locally on every player** from a shared **dungeon seed** sent when the tram departs. The host cannot edit room geometry after generation and have it sync — only the seed (plus dungeon ID and map variant) is networked. All players with the same game data and seed get the same room graph.

The mod's **map flavor** setting does not change the dungeon type or flow asset. It picks one of up to **500** pre-tested seeds for that dungeon's layout flow, biased toward a structural style you choose. Each run still varies within that style because the mod randomly picks one seed from the pool.

## Dungeon pick

`RandomizeDungeonPick` overrides tram dungeon master ID selection.

`DungeonPickPoolMode`:

- `WidenVanilla` — keep vanilla cycle weights; optionally allow repeats sooner via `IgnoreDungeonExcludeList`.
- `AllActiveUniform` — pick uniformly from all active dungeons.

`DungeonAllowlist` / `DungeonBlocklist` filter the pool (allowlist wins when non-empty).

## Map variant

`RandomizeMapVariant` picks map variants uniformly from each dungeon's `MapIDs` on the host before departure. The chosen variant is synced to all players.

## Map flavor (`DungeonSeedFlavor`)

Replaces the old `RandomizeDungeonSeed` toggle. Controls how the host picks the procedural dungeon seed when a run starts.

- **`Vanilla`** — the host uses the game's normal random seed roll. No curated bias.
- **Any other flavor** — before the seed is sent to players, the host replaces it with a random pick from **up to 500 precomputed seeds** for that dungeon's DunGen flow (e.g. all Factory Sector 1 layouts share one pool). Pools are baked into the mod and refreshed by a developer tool when the game updates — not editable in settings. When more than 500 seeds qualify for a flavor, 500 are chosen at random during the scan merge.

Flavors only affect **layout shape** (room count, branching, connectivity, footprint). They do not change enemy counts, loot tables, weather, or quota. Room counts stay within each dungeon flow's designed min/max; flavors pick the best examples inside those bounds.

### Size & footprint

#### `Compact` — fewest rooms

Lowest **total room count**. Main path and branches are both kept short.

**Good for:** faster clears, smaller teams, speed-focused shifts.

**Feels like:** tight, efficient maps with less to sweep.

#### `Expansive` — most rooms

Highest **total room count** within the flow's limits.

**Good for:** exploration, larger teams, maximising searchable space.

**Feels like:** the biggest valid version of this dungeon type.

#### `ShortMainPath` — shortest critical path

Lowest **main-path room count** (start-to-goal spine), regardless of how many branch rooms exist.

**Good for:** reaching the exit quickly, speed goals, less mandatory forward travel.

**Feels like:** a short express route — side rooms may still exist but the core run is brief.

**Unlike `Compact`:** branch wings can remain; only the spine is minimised.

#### `LongMainPath` — longest critical path

Highest **main-path room count** — the longest start-to-goal spine.

**Good for:** extended forward travel, marathon shifts, teams that stay on the critical route.

**Feels like:** a long haul through the dungeon — side rooms may exist but the spine dominates.

**Unlike `ShortMainPath`:** maximises mandatory forward distance instead of minimising it.

#### `Sprawling` — spread out

Largest **physical footprint** (dungeon bounds volume) — rooms are geographically distant.

**Good for:** teams using teleporters, long sightlines, spread-out search patterns.

**Feels like:** walking more metres even if room count is average.

#### `Dense` — packed together

Smallest **footprint per room** — many rooms in a tight cluster.

**Good for:** fast rotation between areas, close-quarters navigation, minimap looks "chunky".

**Feels like:** compact real-estate — less empty space between rooms.

#### `Cramped` — many rooms, tiny footprint

Many **total rooms** packed into the **smallest bounds volume per room**.

**Good for:** claustrophobic navigation, rapid room-to-room rotation in a small area.

**Feels like:** a packed warren — lots to search without walking far.

**Unlike `Dense`:** prioritises high room count in a tight space, not just small footprint alone.

### Shape & connectivity

#### `Linear` — corridor spine

Long **main path relative to branch rooms** (high main/branch ratio).

**Good for:** groups staying together, predictable forward progress.

**Feels like:** a strong spine with short or rare detours.

#### `MinimalBranches` — fewest side rooms

Lowest **branch room count** — almost everything is on the main route.

**Good for:** no-fork decision making, minimal "check the side path" moments.

**Unlike `Linear`:** scored on absolute branch tiles, not ratio — can pair with a long or short main path.

#### `Branching` — side paths

Most **branch rooms and branch depth** combined.

**Good for:** teams that fan out, optional loot wings.

**Feels like:** frequent forks off the critical path.

#### `BroadBranches` — many shallow side wings

Many **branch rooms** with **low maximum branch depth** — wide but not deep.

**Good for:** optional nearby detours, teams that peel off briefly and rejoin.

**Feels like:** lots of short side pockets off the main route.

**Unlike `Deep`:** many branches that do not extend far from the spine.

#### `Deep` — deep branches

Deepest **branch chains** (most steps from main path into a side wing).

**Good for:** nested side areas, multi-room detours.

**Feels like:** side paths that keep going.

#### `Open` — highly connected

Most **doorway connections** between rooms.

**Good for:** loop-back routes, fluid movement, fewer one-way dead runs.

**Feels like:** a web — many ways to move between neighbours.

#### `Maze` — dead-end heavy

Most **unused doorways** (cul-de-sacs and alcoves).

**Good for:** search-heavy runs, checking many small dead ends for loot.

**Feels like:** lots of "check this nook" moments — not necessarily deep, but fiddly.

#### `Loopy` — circular routes

Highest **connections per room** — many loop-back paths, few dead ends.

**Good for:** fluid movement, teams circling back without backtracking.

**Feels like:** you can often go around a block instead of reversing.

#### `DeadEnds` — stub corridors

Many **unused doorways per room** relative to low **connection count** — stubby cul-de-sacs.

**Good for:** meticulous room-by-room search, finding tucked-away loot.

**Unlike `Maze`:** scored on dead-end density, not absolute unused doorway count.

### Composites — combined traits

Each composite is ranked separately (up to 500 seeds per flow, randomly sampled when more qualify), not a mix of two other pools.

#### `TightCorridor` — small + linear

**Compact** then **linear**: fewest rooms, then best main/branch ratio among those.

**Good for:** fastest co-op clears with minimal side exploration.

**Feels like:** a short, straight job — small and focused.

#### `Labyrinth` — big + branchy + deep

**Expansive**, then **branching/deep**: most rooms, then most side structure.

**Good for:** maximum dungeon — full exploration, large teams, long shifts.

**Feels like:** the "full meal" layout for that flow.

#### `Honeycomb` — connected + compact space

**Open per unit space**: most connections relative to footprint, favouring smaller total area.

**Good for:** tight maps where you can still loop between rooms quickly.

**Feels like:** dense web — lots of links in a small area.

#### `WideOpen` — connected + spread out

**Open**, then **sprawling**: many connections across a large footprint.

**Good for:** large teams with multiple loop routes across a wide map.

**Feels like:** an open campus — room to spread out, many paths between areas.

#### `StableCompact` — reliable + small

**Reliable**, then **compact**: cleanest generation first, then fewest rooms among those.

**Good for:** multiplayer speedruns where layout must sync and stay small.

**Feels like:** dependably tight — no weird gen failures, minimal sweep.

#### `DeepMaze` — deep + dead-ends

**Deep**, then **maze**: deepest branches with many dead-end doorways.

**Good for:** hardcore search, thorough looting, players who like disorienting wings.

**Feels like:** nested cul-de-sacs — confusing side areas that keep branching.

### Quality

#### `Reliable` — stable generation

Fewest **DunGen retries**; seeds that fail generation are excluded.

**Good for:** avoiding rare broken layouts, consistent multiplayer builds.

**Feels like:** dependable vanilla-quality generation.

#### `Balanced` — intentionally average

Closest to the **median** room count, branch score, and connection count across scanned seeds.

**Good for:** runs that avoid extreme layouts — neither tiny nor sprawling.

**Feels like:** a typical dungeon for that flow — no strong bias toward any extreme.

**Unlike `Vanilla`:** still curated from pre-tested seeds; avoids outlier layouts rather than using a fully random game roll.

## Layout flow (advanced)

`RandomizeLayoutFlow` swaps which DunGen flow asset is used inside a dungeon, picked uniformly instead of vanilla weights.

**Multiplayer warning:** this runs during local map load and uses non-synced randomness. It can cause **different layouts on host vs clients**. Prefer **map flavor** for layout control in co-op. Leave `RandomizeLayoutFlow` off unless you understand the risk.

## Multiplayer notes

- Map flavor, dungeon pick, and map variant are applied on the **host** before `MoveToDungeonSig` — all clients receive the same seed.
- Clients do **not** need the mod for vanilla seeds; they **do** need matching game data to interpret the same seed identically.
- If no seed pool exists yet for a flow (e.g. before the developer scanner has been run for a game version), the mod falls back to the vanilla host seed and logs a warning.

**Full config keys →** [Dungeon Randomizer](../CONFIG.md#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer)
