using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Economy
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_Economy] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class EconomyConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Economy", "Economy");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableEconomy = ModConfig.CreateTrackedEntry(_category,
                "EnableEconomy",
                false,
                "Enable Economy",
                "Scale startup money, round goal quota, scrap/sell values, shop buy prices, and reinforce costs. Optionally retain unspent currency between maintenance cycles. Host only.");

            ModConfig.EconomyPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "EconomyPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate,
                "Economy Player Count Scale Rate",
                "Extra multiplier per player above 4 when an Auto Scale … by Player Count toggle is enabled (0.10 = +10% per extra player, stacks with money multipliers). Minimum is 0.");

            ModConfig.AutoScaleStartupMoneyByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleStartupMoneyByPlayerCount",
                true,
                "Auto Scale Startup Money by Player Count",
                "When enabled, apply Economy Player Count Scale Rate per player above 4 (stacks with Startup Money Multiplier).");

            ModConfig.StartupMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StartupMoneyMultiplier",
                1f,
                "Startup Money Multiplier",
                "Starting maintenance-room currency on a new save slot or session reset to vanilla initial money (1 = vanilla, 2 = double). Does not apply when loading a save game.");

            ModConfig.AutoScaleRoundGoalMoneyByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleRoundGoalMoneyByPlayerCount",
                true,
                "Auto Scale Round Goal Money by Player Count",
                "When enabled, apply Economy Player Count Scale Rate per player above 4 (stacks with Round Goal Money Multiplier).");

            ModConfig.RoundGoalMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalMoneyMultiplier",
                1f,
                "Round Goal Money Multiplier",
                "Target currency required to finish a stage (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleScrapSellValueByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleScrapSellValueByPlayerCount",
                true,
                "Auto Scale Scrap Sell Value by Player Count",
                "When enabled, apply Economy Player Count Scale Rate per player above 4 (stacks with Scrap Sell Value Multiplier).");

            ModConfig.ScrapSellValueMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ScrapSellValueMultiplier",
                1f,
                "Scrap Sell Value Multiplier",
                "Currency earned when scrapping items and item value counted toward the tram quota (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleShopBuyPriceByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleShopBuyPriceByPlayerCount",
                true,
                "Auto Scale Shop Buy Price by Player Count",
                "When enabled, apply Economy Player Count Scale Rate per player above 4 (stacks with Shop Buy Price Multiplier).");

            ModConfig.ShopBuyPriceMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ShopBuyPriceMultiplier",
                1f,
                "Shop Buy Price Multiplier",
                "Maintenance shop and vending-machine kiosk purchase cost multiplier (1 = vanilla, 2 = double). Applied when shop items are initialized each maintenance round.");

            ModConfig.ShopDiscountMinPercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountMinPercent",
                0,
                "Shop Discount Min (percent)",
                "Minimum shop discount percentage when a discount is rolled (0–100). Only used when Shop Discount Chance (percent) is above 0.");

            ModConfig.ShopDiscountMaxPercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountMaxPercent",
                100,
                "Shop Discount Max (percent)",
                "Maximum shop discount percentage when a discount is rolled (0–100). Must be ≥ Shop Discount Min (percent).");

            ModConfig.ShopDiscountChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountChancePercent",
                0,
                "Shop Discount Chance (percent)",
                "Chance per shop item to receive a discount between min and max percent (0 = vanilla shop discounts, 100 = every item discounted).");

            ModConfig.AutoScaleReinforcePriceByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleReinforcePriceByPlayerCount",
                true,
                "Auto Scale Reinforce Price by Player Count",
                "When enabled, apply Economy Player Count Scale Rate per player above 4 (stacks with Reinforce Price Multiplier).");

            ModConfig.ReinforcePriceMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ReinforcePriceMultiplier",
                1f,
                "Reinforce Price Multiplier",
                "Maintenance item reinforcement cost multiplier (1 = vanilla, 2 = double).");

            ModConfig.RetainUnspentCurrencyBetweenCycles = ModConfig.CreateTrackedEntry(_category,
                "RetainUnspentCurrencyBetweenCycles",
                false,
                "Retain Unspent Currency Between Cycles",
                "Keep unspent maintenance-room currency when departing for the next dungeon instead of zeroing it. Does not affect tram repair cost. Host only.");
        }

        /// <summary>Clamps persisted shop discount percents once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnShopDiscountPercentChanged(logger, ModConfig.ShopDiscountMinPercent.Value, ModConfig.ShopDiscountMinPercent);
            OnShopDiscountPercentChanged(logger, ModConfig.ShopDiscountMaxPercent.Value, ModConfig.ShopDiscountMaxPercent);
            OnShopDiscountPercentChanged(logger, ModConfig.ShopDiscountChancePercent.Value, ModConfig.ShopDiscountChancePercent);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableEconomy.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableEconomy));
            ModConfig.EconomyPlayerCountScaleRate.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.EconomyPlayerCountScaleRate));
            ModConfig.AutoScaleStartupMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleStartupMoneyByPlayerCount));
            ModConfig.AutoScaleRoundGoalMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleRoundGoalMoneyByPlayerCount));
            ModConfig.AutoScaleScrapSellValueByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleScrapSellValueByPlayerCount));
            ModConfig.AutoScaleShopBuyPriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleShopBuyPriceByPlayerCount));
            ModConfig.AutoScaleReinforcePriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleReinforcePriceByPlayerCount));
            ModConfig.RetainUnspentCurrencyBetweenCycles.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.RetainUnspentCurrencyBetweenCycles));

            ModConfig.StartupMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.StartupMoneyMultiplier));
            ModConfig.RoundGoalMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.RoundGoalMoneyMultiplier));
            ModConfig.ScrapSellValueMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.ScrapSellValueMultiplier));
            ModConfig.ShopBuyPriceMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.ShopBuyPriceMultiplier));
            ModConfig.ShopDiscountMinPercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ModConfig.ShopDiscountMinPercent));
            ModConfig.ShopDiscountMaxPercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ModConfig.ShopDiscountMaxPercent));
            ModConfig.ShopDiscountChancePercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ModConfig.ShopDiscountChancePercent));
            ModConfig.ReinforcePriceMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.ReinforcePriceMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.EconomyPlayerCountScaleRate);
            ModConfig.TrackFloatEntry(ModConfig.StartupMoneyMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.RoundGoalMoneyMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.ScrapSellValueMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.ShopBuyPriceMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.ReinforcePriceMultiplier);
        }

        private static void OnShopDiscountPercentChanged(MelonLogger.Instance logger, int value, MelonPreferences_Entry<int> entry)
        {
            if (value < 0)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0;
                return;
            }

            if (value > 100)
            {
                logger.Warning($"{entry.Identifier} must be <= 100; resetting to 100.");
                entry.Value = 100;
                return;
            }

            if (ModConfig.ShopDiscountMaxPercent.Value < ModConfig.ShopDiscountMinPercent.Value)
            {
                logger.Warning("ShopDiscountMaxPercent must be >= ShopDiscountMinPercent; syncing max to min.");
                ModConfig.ShopDiscountMaxPercent.Value = ModConfig.ShopDiscountMinPercent.Value;
            }

            ModConfig.NotifyChanged(entry);
        }
    }
}
