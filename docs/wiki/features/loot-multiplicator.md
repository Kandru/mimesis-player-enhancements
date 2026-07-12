# Loot Multiplicator

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_LootMultiplicator`](../CONFIG.md#loot-multiplicator--mimesisplayerenhancement_lootmultiplicator)

Adjusts how much loot you find on the map and what enemies drop when defeated. Increase or decrease quantities, scale with player count, and optionally limit which item types are affected.

Each multiplier stacks with its **Auto Scale … By Player Count** toggle using `LootMultiplicatorPlayerCountScaleRate` per player above 4.

## Map loot

Map-placed pickup loot uses two main paths:

- **Fixed loot** — markers with preset items: stack size, respawn count, duplicate markers on unused slots.
- **Random pool loot** — empty markers filled at dungeon start from a scrap-value budget. `MapLootMultiplier` scales that budget (more budget → more markers filled).

`AutoScaleMapLootByPlayerCount` adds player-count scaling. Trigger/event loot is **not** scaled.

## Drop loot

`DropLootMultiplier` scales items from enemy death tables and inventory drops on death. Adds extra weighted re-rolls from the same drop table. Consumable stack count is scaled when the item spawns. `AutoScaleDropLootByPlayerCount` stacks player-count scaling.

Does **not** scale shop purchases, Crow Shop exchange, deathmatch MVP rewards, or admin/cheat spawns.

## Item filters

`LootItemFilterMode` restricts which item master IDs can spawn (`All`, `AllowlistOnly`, `BlocklistOnly`). Allowlist/blocklist use comma-separated IDs — see [LOOT_ITEM_IDS.md](../LOOT_ITEM_IDS.md).

When filtering, `AutoScaleMapLootBudgetForFilter` (default on) adjusts the random-pool budget so expensive allowlists still get proportionally more spawns.

## Mimic decoy conversion

Mimics often drop fake decoy items from inventory. `ConvertFakeActorDyingDropChancePercent` sets the chance those become real pickup loot (`0` = vanilla, `100` = always real). Monster drop-table loot is already real.

**Full config keys →** [Loot Multiplicator](../CONFIG.md#loot-multiplicator--mimesisplayerenhancement_lootmultiplicator)
