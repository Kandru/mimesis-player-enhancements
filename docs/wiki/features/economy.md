# Economy

Scales startup cash, scrap/sell value, shop and reinforce prices, and optional unspent currency between maintenance cycles. Only the host must enable this feature. Complements [Loot Multiplicator](./loot-multiplicator.md) (spawn counts, not prices); tram repair quotas stay under [More Players](./more-players.md).

**Config:** [`MimesisPlayerEnhancement_Economy`](../CONFIG.md#economy--mimesisplayerenhancement_economy)

## Configuration

Changes apply without a game restart. Value changes during maintenance, tram, dungeon, or deathmatch scenes are held until that scene ends; turning `EnableEconomy` off applies immediately. Unset keys use the defaults below.

### `EnableEconomy`

Master toggle for all Economy scaling and optional currency retention. Turning it off restores shop prices to their cached vanilla base (mod discount rates cannot be restored once applied).

| Value | Meaning |
|---|---|
| `false` | Vanilla money values and cycle currency reset |
| `true` | Apply the multipliers and retention settings below |

Default: `false`

### `EconomyPlayerCountScaleRate`

Extra multiplier per player above 4 when an **Auto Scale … By Player Count** toggle is on. Stacks with each money multiplier: `multiplier × (1 + (players − 4) × rate)`. At 4 or fewer players, player-count scaling is `1` (no change).

| Value | Meaning |
|---|---|
| `0` | No player-count bonus |
| `0.10` | +10% per extra player (5 players → ×1.10 on top of the type multiplier) |

Default: `0.10`

### `AutoScaleStartupMoneyByPlayerCount`

When on, startup money also uses `EconomyPlayerCountScaleRate` for players above 4.

| Value | Meaning |
|---|---|
| `false` | Startup money uses only `StartupMoneyMultiplier` |
| `true` | Stack player-count scaling on startup money |

Default: `true`

### `StartupMoneyMultiplier`

Scales starting maintenance-room currency on a new save or session reset. Does not apply when loading an existing save.

| Value | Meaning |
|---|---|
| `1` | Vanilla starting cash |
| `2` | Double starting cash |
| `0` | No starting cash |

Default: `1`

### `AutoScaleScrapSellValueByPlayerCount`

When on, scrap/sell values also use `EconomyPlayerCountScaleRate` for players above 4.

| Value | Meaning |
|---|---|
| `false` | Scrap values use only `ScrapSellValueMultiplier` |
| `true` | Stack player-count scaling on scrap/sell values |

Default: `true`

### `ScrapSellValueMultiplier`

Scales currency from scrapping items and item value counted toward the tram quota.

| Value | Meaning |
|---|---|
| `1` | Vanilla scrap/sell value |
| `2` | Double scrap/sell value |

Default: `1`

### `AutoScaleShopBuyPriceByPlayerCount`

When on, shop buy prices also use `EconomyPlayerCountScaleRate` for players above 4.

| Value | Meaning |
|---|---|
| `false` | Shop prices use only `ShopBuyPriceMultiplier` |
| `true` | Stack player-count scaling on shop buy prices |

Default: `true`

### `ShopBuyPriceMultiplier`

Scales maintenance shop and vending-machine purchase costs. Applied when shop items initialize each maintenance round (not when loading a save).

| Value | Meaning |
|---|---|
| `1` | Vanilla shop prices |
| `0.1` | 10% of vanilla |
| `2` | Double shop prices |

Default: `1`

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

Chance per shop item to receive a discount in the min–max range. At `0`, vanilla shop discount tables are unchanged.

| Value | Meaning |
|---|---|
| `0` | Vanilla shop discounts |
| `50` | Each item has a 50% chance of a mod discount |
| `100` | Every item gets a mod discount |

Default: `0`

### `AutoScaleReinforcePriceByPlayerCount`

When on, reinforce costs also use `EconomyPlayerCountScaleRate` for players above 4.

| Value | Meaning |
|---|---|
| `false` | Reinforce costs use only `ReinforcePriceMultiplier` |
| `true` | Stack player-count scaling on reinforce costs |

Default: `true`

### `ReinforcePriceMultiplier`

Scales maintenance item reinforcement cost.

| Value | Meaning |
|---|---|
| `1` | Vanilla reinforce cost |
| `2` | Double reinforce cost |

Default: `1`

### `RetainUnspentCurrencyBetweenCycles`

Keeps unspent maintenance-room currency when departing for the next dungeon instead of zeroing it (vanilla zeros it). Does not affect tram repair cost.

| Value | Meaning |
|---|---|
| `false` | Vanilla — currency resets between cycles |
| `true` | Carry unspent cash into the next cycle |

Default: `false`
