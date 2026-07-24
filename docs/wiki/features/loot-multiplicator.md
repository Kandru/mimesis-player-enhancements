# Loot Multiplicator

**Host only** — only the host must enable this for the whole lobby to get the effect. Joining clients do not need the mod.

Scales map loot and enemy death drops, optionally filters which items can spawn, and can turn mimic inventory decoys into real pickup loot. Use it when you want more loot in larger groups or tighter control over what appears. Most settings apply at the start of the next dungeon scene; turning the feature off applies immediately.

**Config section:** [`MimesisPlayerEnhancement_LootMultiplicator`](../CONFIG.md#loot-multiplicator--mimesisplayerenhancement_lootmultiplicator)

## Configuration

### `EnableLootMultiplicator`

Master switch for map loot scaling, enemy death-drop scaling, item filters, and mimic decoy conversion. When off, pending bonus loot respawns are cleared right away.

| Value | Meaning |
|---|---|
| `false` | Vanilla loot behavior (default) |
| `true` | Feature active on the host |

Default: `false`

### `LootMultiplicatorPlayerCountScaleRate`

Extra multiplier per player above four when an **Auto Scale … By Player Count** toggle is on. Stacks with `MapLootMultiplier` and `DropLootMultiplier`. Example: `0.10` with six players → ×1.2 on top of the base multiplier.

| Value | Meaning |
|---|---|
| `0` | No player-count bonus |
| `0.10` | +10% per extra player (default) |
| Higher | Stronger scaling for large lobbies |

Default: `0.10`

### `AutoScaleMapLootByPlayerCount`

Adds player-count scaling to map-placed loot (fixed markers and random pool markers). Does not affect trigger/event spawns.

| Value | Meaning |
|---|---|
| `true` | Scale map loot with lobby size (default) |
| `false` | Use only `MapLootMultiplier` |

Default: `true`

### `MapLootMultiplier`

Multiplier for map-placed pickup loot. `1` = vanilla; values above `1` increase quantity. Values below `1` do not reduce loot today.

Affects fixed markers (consumable stack size, respawn count, bonus copies on unused slots) and random pool markers (dungeon scrap-value budget — more budget fills more empty markers). Trigger/event loot is not scaled.

| Value | Meaning |
|---|---|
| `1` | Vanilla map loot (default) |
| `2` | Roughly double map loot |
| `> 1` | More fixed copies, respawns, and random-pool spawns |

Default: `1`

### `AutoScaleDropLootByPlayerCount`

Adds player-count scaling to enemy death drops.

| Value | Meaning |
|---|---|
| `true` | Scale death drops with lobby size (default) |
| `false` | Use only `DropLootMultiplier` |

Default: `true`

### `DropLootMultiplier`

Multiplier for items from enemy death tables and inventory dropped on death. `1` = vanilla; values above `1` add extra weighted rolls from the same table and scale consumable stack counts when items spawn. Values below `1` do not reduce drops today.

Does **not** affect shop purchases, Crow Shop exchange, deathmatch MVP rewards, admin/cheat spawns, or other non-combat spawn reasons.

| Value | Meaning |
|---|---|
| `1` | Vanilla death drops (default) |
| `2` | Roughly double death-drop quantity |
| `> 1` | Extra drop-table rolls and larger consumable stacks |

Default: `1`

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
