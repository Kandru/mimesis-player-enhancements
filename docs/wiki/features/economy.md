# Economy

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_Economy`](../CONFIG.md#economy--mimesisplayerenhancement_economy)

Changes money-related values for your run: starting cash, scrap/sell values, shop and reinforce prices, and optional currency retention between maintenance cycles. Tram repair quotas are handled by [More Players](./more-players.md) → Scaling round goals.

Each multiplier has an **Auto Scale … By Player Count** toggle using `EconomyPlayerCountScaleRate` per player above 4. Complements [Loot Multiplicator](./loot-multiplicator.md) — Economy scales currency amounts and prices, not item spawn counts.

## Startup money

`StartupMoneyMultiplier` scales starting currency on a new save or session reset. Does **not** apply when loading an existing save. `AutoScaleStartupMoneyByPlayerCount` stacks player-count scaling.

## Scrap values

`ScrapSellValueMultiplier` scales currency from scrapping items and item value counted toward the tram quota. `AutoScaleScrapSellValueByPlayerCount` stacks player-count scaling.

## Shop prices

`ShopBuyPriceMultiplier` scales maintenance shop and vending-machine purchase costs (`1` = vanilla, `0.1` = 10% of vanilla). Applied when shop items initialize each maintenance round.

Optional discount rolls: `ShopDiscountChancePercent` with `ShopDiscountMinPercent` / `MaxPercent` per item. At `0` chance, vanilla shop discount tables are unchanged.

## Reinforce costs

`ReinforcePriceMultiplier` scales maintenance item reinforcement cost. `AutoScaleReinforcePriceByPlayerCount` stacks player-count scaling.

## Currency retention

`RetainUnspentCurrencyBetweenCycles` keeps unspent maintenance-room currency when departing for the next dungeon instead of zeroing it (vanilla zeros it). Does not affect tram repair cost.

**Full config keys →** [Economy](../CONFIG.md#economy--mimesisplayerenhancement_economy)
