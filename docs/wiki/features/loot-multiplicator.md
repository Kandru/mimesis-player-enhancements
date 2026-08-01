# Loot Multiplicator

**Host only** — only the host must enable this for the whole lobby to get the effect. Joining clients do not need the mod.

Scales map loot and enemy death drops, optionally filters which items can spawn, and can turn mimic inventory decoys into real pickup loot. Use it when you want more loot in larger groups or tighter control over what appears. Most settings apply at the start of the next dungeon scene; turning the feature off applies immediately.

Each loot type uses a general multiplier plus a per-player bonus above a shared baseline. Effective multiplier: `general + max(0, players − baseline) × perPlayer`.

## Configuration

### `EnableLootMultiplicator`

Master switch for map loot scaling, enemy death-drop scaling, item filters, and mimic decoy conversion. When off, pending bonus loot respawns are cleared right away.

| Value | Meaning |
|---|---|
| `false` | Vanilla loot behavior (default) |
| `true` | Feature active on the host |

Default: `false`

### `LootMultiplicatorBaselinePlayerCount`

Player count where per-player scaling starts. At or below this count, only each type's general multiplier applies.

| Value | Meaning |
|---|---|
| `1` | Minimum allowed |
| `4` | Vanilla four-player baseline (default) |
| Higher | Per-player bonus only applies above this count |

Default: `4`

### `MapLootMultiplier`

Multiplier for map-placed pickup loot. `1` = vanilla; values above `1` increase quantity. Values below `1` do not reduce loot today. Stacks additively with `MapLootPerPlayerMultiplier` for players above the baseline.

Affects fixed markers (consumable stack size, respawn count, bonus copies on unused slots) and random pool markers (dungeon scrap-value budget — more budget fills more empty markers). Trigger/event loot is not scaled.

| Value | Meaning |
|---|---|
| `1` | Vanilla map loot (default) |
| `2` | Roughly double map loot |
| `> 1` | More fixed copies, respawns, and random-pool spawns |

Default: `1`

### `MapLootPerPlayerMultiplier`

Added to `MapLootMultiplier` for each player above `LootMultiplicatorBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for map loot |
| `0.10` | +0.10 per extra player (default) |
| Higher | Stronger scaling for large lobbies |

Default: `0.10`

### `DropLootMultiplier`

Multiplier for items from enemy death tables and inventory dropped on death. `1` = vanilla; values above `1` add extra weighted rolls from the same table and scale consumable stack counts when items spawn. Values below `1` do not reduce drops today. Stacks additively with `DropLootPerPlayerMultiplier` for players above the baseline.

Does **not** affect shop purchases, Crow Shop exchange, deathmatch MVP rewards, admin/cheat spawns, or other non-combat spawn reasons.

| Value | Meaning |
|---|---|
| `1` | Vanilla death drops (default) |
| `2` | Roughly double death-drop quantity |
| `> 1` | Extra drop-table rolls and larger consumable stacks |

Default: `1`

### `DropLootPerPlayerMultiplier`

Added to `DropLootMultiplier` for each player above `LootMultiplicatorBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for death drops |
| `0.10` | +0.10 per extra player (default) |
| Higher | Stronger scaling for large lobbies |

Default: `0.10`

### `LootItemFilterMode`

Restricts which item master IDs can spawn on the map and from enemy drops. Invalid values reset to `All`.

| Value | Meaning |
|---|---|
| `All` | No filter — any item can spawn (default) |
| `AllowlistOnly` | Only IDs in `LootAllowlist` |
| `BlocklistOnly` | All IDs except those in `LootBlocklist` |

Default: `All`

### `LootAllowlist`

Comma-separated item master IDs allowed to spawn when `LootItemFilterMode` is `AllowlistOnly`. Off-list IDs in random pool markers can be replaced; missing allowlist IDs may be injected into the pool. See [LOOT_ITEM_IDS.md](../../LOOT_ITEM_IDS.md) for ID reference.

Default: `""` (empty — no effect unless mode is `AllowlistOnly`)

### `LootBlocklist`

Comma-separated item master IDs excluded from spawning when `LootItemFilterMode` is `BlocklistOnly`. See [LOOT_ITEM_IDS.md](../../LOOT_ITEM_IDS.md).

Default: `""` (empty — no effect unless mode is `BlocklistOnly`)

### `AutoScaleMapLootBudgetForFilter`

When filter mode is not `All`, multiplies the random-pool scrap budget by the ratio of filtered vs vanilla average item sell value (on top of `MapLootMultiplier`). Keeps expensive allowlists from starving random-pool spawns.

| Value | Meaning |
|---|---|
| `true` | Compensate budget for filtered item prices (default) |
| `false` | Use only `MapLootMultiplier` on the budget |

Default: `true`

### `ConvertFakeActorDyingDropChancePercent`

Chance that fake items dropped from a dying enemy's inventory (e.g. mimic decoys) become real pickup loot. Monster drop-table loot is already real and is not affected. Out-of-range values reset to `30`.

| Value | Meaning |
|---|---|
| `0` | Vanilla — decoys vanish on grab |
| `30` | 30% become real (default) |
| `100` | Always real |

Default: `30`
