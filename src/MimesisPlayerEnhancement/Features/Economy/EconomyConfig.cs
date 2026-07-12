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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Economy");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableEconomy = ModConfig.CreateTrackedEntry(_category,
                "EnableEconomy",
                false);

            ModConfig.EconomyPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "EconomyPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate);

            ModConfig.AutoScaleStartupMoneyByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleStartupMoneyByPlayerCount",
                true);

            ModConfig.StartupMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StartupMoneyMultiplier",
                1f);

            ModConfig.AutoScaleScrapSellValueByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleScrapSellValueByPlayerCount",
                true);

            ModConfig.ScrapSellValueMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ScrapSellValueMultiplier",
                1f);

            ModConfig.AutoScaleShopBuyPriceByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleShopBuyPriceByPlayerCount",
                true);

            ModConfig.ShopBuyPriceMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ShopBuyPriceMultiplier",
                1f);

            ModConfig.ShopDiscountMinPercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountMinPercent",
                0);

            ModConfig.ShopDiscountMaxPercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountMaxPercent",
                100);

            ModConfig.ShopDiscountChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountChancePercent",
                0);

            ModConfig.AutoScaleReinforcePriceByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleReinforcePriceByPlayerCount",
                true);

            ModConfig.ReinforcePriceMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ReinforcePriceMultiplier",
                1f);

            ModConfig.RetainUnspentCurrencyBetweenCycles = ModConfig.CreateTrackedEntry(_category,
                "RetainUnspentCurrencyBetweenCycles",
                false);
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
            ModConfig.AutoScaleScrapSellValueByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleScrapSellValueByPlayerCount));
            ModConfig.AutoScaleShopBuyPriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleShopBuyPriceByPlayerCount));
            ModConfig.AutoScaleReinforcePriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleReinforcePriceByPlayerCount));
            ModConfig.RetainUnspentCurrencyBetweenCycles.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.RetainUnspentCurrencyBetweenCycles));

            ModConfig.StartupMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.StartupMoneyMultiplier));
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
