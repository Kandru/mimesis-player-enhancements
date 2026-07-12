using MelonLoader;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_LootMultiplicator] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class LootMultiplicatorConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_LootMultiplicator");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableLootMultiplicator = ModConfig.CreateTrackedEntry(_category,
                "EnableLootMultiplicator",
                false);

            ModConfig.LootMultiplicatorPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "LootMultiplicatorPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate);

            ModConfig.AutoScaleMapLootByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMapLootByPlayerCount",
                true);

            ModConfig.MapLootMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MapLootMultiplier",
                1f);

            ModConfig.AutoScaleDropLootByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleDropLootByPlayerCount",
                true);

            ModConfig.DropLootMultiplier = ModConfig.CreateTrackedEntry(_category,
                "DropLootMultiplier",
                1f);

            ModConfig.LootItemFilterMode = ModConfig.CreateTrackedEntry(_category,
                "LootItemFilterMode",
                "All");

            ModConfig.LootAllowlist = ModConfig.CreateTrackedEntry(_category,
                "LootAllowlist",
                "");

            ModConfig.LootBlocklist = ModConfig.CreateTrackedEntry(_category,
                "LootBlocklist",
                "");

            ModConfig.AutoScaleMapLootBudgetForFilter = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMapLootBudgetForFilter",
                true);

            ModConfig.ConvertFakeActorDyingDropChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ConvertFakeActorDyingDropChancePercent",
                30);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableLootMultiplicator.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableLootMultiplicator));
            ModConfig.LootMultiplicatorPlayerCountScaleRate.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.LootMultiplicatorPlayerCountScaleRate));
            ModConfig.AutoScaleMapLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleMapLootByPlayerCount));
            ModConfig.AutoScaleDropLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleDropLootByPlayerCount));

            ModConfig.MapLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MapLootMultiplier));
            ModConfig.DropLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.DropLootMultiplier));
            ModConfig.LootItemFilterMode.OnEntryValueChanged.Subscribe((_, value) => OnLootItemFilterModeChanged(logger, value));
            ModConfig.LootAllowlist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.LootAllowlist));
            ModConfig.LootBlocklist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.LootBlocklist));
            ModConfig.AutoScaleMapLootBudgetForFilter.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.AutoScaleMapLootBudgetForFilter));
            ModConfig.ConvertFakeActorDyingDropChancePercent.OnEntryValueChanged.Subscribe((_, value) =>
                OnFakeActorDyingDropChancePercentChanged(logger, value));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.LootMultiplicatorPlayerCountScaleRate);
            ModConfig.TrackFloatEntry(ModConfig.MapLootMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.DropLootMultiplier);
        }

        private static void OnLootItemFilterModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "All", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "AllowlistOnly", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "BlocklistOnly", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("LootItemFilterMode must be All, AllowlistOnly, or BlocklistOnly; resetting to All.");
                ModConfig.LootItemFilterMode.Value = "All";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.LootItemFilterMode);
        }

        private static void OnFakeActorDyingDropChancePercentChanged(MelonLogger.Instance logger, int value)
        {
            if (value is < 0 or > 100)
            {
                logger.Warning("ConvertFakeActorDyingDropChancePercent must be 0-100; resetting to 30.");
                ModConfig.ConvertFakeActorDyingDropChancePercent.Value = 30;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.ConvertFakeActorDyingDropChancePercent);
        }
    }
}
