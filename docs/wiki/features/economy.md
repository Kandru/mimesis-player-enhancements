# Economy

**Scope:** host

Scales scrap/sell value, shop and reinforce prices, and optional unspent currency between maintenance cycles during a run. Complements [Loot Multiplicator](./loot-multiplicator.md) (spawn counts, not prices); tram repair quotas stay under [More Players](./more-players.md).

Each money type uses a general multiplier plus a per-player bonus above a shared baseline. Effective multiplier: `general + max(0, players − baseline) × perPlayer`.

## Configuration

Changes apply without a game restart. Value changes during maintenance, tram, dungeon, or deathmatch scenes are held until that scene ends; turning `EnableEconomy` off applies immediately. Unset keys use the defaults below.

### `EnableEconomy`

Master toggle for all Economy scaling and optional currency retention. Turning it off restores shop prices to their cached vanilla base (mod discount rates cannot be restored once applied).

| Value | Meaning |
|---|---|
| `false` | Vanilla money values and cycle currency reset |
| `true` | Apply the multipliers and retention settings below |

Default: `false`

### `EconomyBaselinePlayerCount`

Player count where per-player scaling starts. At or below this count, only each type's general multiplier applies.

| Value | Meaning |
|---|---|
| `1` | Minimum allowed |
| `4` | Vanilla four-player baseline (default) |
| Higher | Per-player bonus only applies above this count |

Default: `4`

### `ScrapSellValueMultiplier`

Scales currency from scrapping items and item value counted toward the tram quota. Stacks additively with `ScrapSellValuePerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `1` | Vanilla scrap/sell value |
| `2` | Double scrap/sell value |

Default: `1`

### `ScrapSellValuePerPlayerMultiplier`

Added to `ScrapSellValueMultiplier` for each player above `EconomyBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for scrap/sell values |
| `0.10` | +0.10 per extra player (default) |
| Higher | Stronger scaling for large lobbies |

Default: `0.10`

### `ShopBuyPriceMultiplier`

Scales maintenance shop and vending-machine purchase costs. Applied when shop items initialize each maintenance round (not when loading a save). Stacks additively with `ShopBuyPricePerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `1` | Vanilla shop prices |
| `0.1` | 10% of vanilla |
| `2` | Double shop prices |

Default: `1`

### `ShopBuyPricePerPlayerMultiplier`

Added to `ShopBuyPriceMultiplier` for each player above `EconomyBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for shop buy prices |
| `0.10` | +0.10 per extra player (default) |
| Higher | Stronger scaling for large lobbies |

Default: `0.10`

### `ShopDiscountMinPercent`

Minimum discount percentage when a shop discount roll succeeds. Only used when `ShopDiscountChancePercent` is above `0`.

| Value | Meaning |
|---|---|
| `0`–`100` | Lower bound of the random discount range |

Default: `0`

### `ShopDiscountMaxPercent`

Maximum discount percentage when a shop discount roll succeeds. Must be ≥ `ShopDiscountMinPercent` (the mod syncs max to min if they drift).

| Value | Meaning |
|---|---|
| `0`–`100` | Upper bound of the random discount range |

Default: `100`

### `ShopDiscountChancePercent`

Chance per shop item to receive a discount in the min–max range. At `0`, vanilla shop discount tables are unchanged. Rolls once when the maintenance shop is initialized (or when discount settings change), not when players join.

| Value | Meaning |
|---|---|
| `0` | Vanilla shop discounts |
| `50` | Each item has a 50% chance of a mod discount |
| `100` | Every item gets a mod discount |

Default: `0`

### `ReinforcePriceMultiplier`

Scales maintenance item reinforcement cost. Stacks additively with `ReinforcePricePerPlayerMultiplier` for players above the baseline.

| Value | Meaning |
|---|---|
| `1` | Vanilla reinforce cost |
| `2` | Double reinforce cost |

Default: `1`

### `ReinforcePricePerPlayerMultiplier`

Added to `ReinforcePriceMultiplier` for each player above `EconomyBaselinePlayerCount`.

| Value | Meaning |
|---|---|
| `0` | No player-count scaling for reinforce costs |
| `0.10` | +0.10 per extra player (default) |
| Higher | Stronger scaling for large lobbies |

Default: `0.10`

### `RetainUnspentCurrencyBetweenCycles`

Keeps unspent maintenance-room currency when departing for the next dungeon instead of zeroing it (vanilla zeros it). Does not affect tram repair cost.

| Value | Meaning |
|---|---|
| `false` | Vanilla — currency resets between cycles |
| `true` | Carry unspent cash into the next cycle |

Default: `false`
