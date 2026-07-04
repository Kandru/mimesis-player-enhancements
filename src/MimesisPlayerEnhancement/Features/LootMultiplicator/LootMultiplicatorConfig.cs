using System;
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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_LootMultiplicator", "Loot Multiplicator");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableLootMultiplicator = ModConfig.CreateTrackedEntry(_category,
                "EnableLootMultiplicator",
                false,
                "Enable Loot Multiplicator",
                "Scale map loot and enemy death drops, and optionally convert mimic fake drops to real loot. Host only.");

            ModConfig.LootMultiplicatorPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "LootMultiplicatorPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate,
                "Player Count Scale Rate",
                "Extra multiplier per player above 4 when an Auto Scale … By Player Count toggle is enabled (0.10 = +10% per extra player, stacks with loot multipliers). Minimum is 0.");

            ModConfig.AutoScaleMapLootByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMapLootByPlayerCount",
                true,
                "Auto Scale Map Loot By Player Count",
                "Map loot = items placed on the dungeon map (spawn markers, shelves, floors). When enabled, apply LootMultiplicatorPlayerCountScaleRate per player above 4 (stacks with MapLootMultiplier).");

            ModConfig.MapLootMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MapLootMultiplier",
                1f,
                "Map Loot Multiplier",
                "Multiplier for all map-placed pickup loot: fixed markers, respawn counts, and random pool budgets. 1 = vanilla, 2 = double.");

            ModConfig.AutoScaleDropLootByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleDropLootByPlayerCount",
                true,
                "Auto Scale Drop Loot By Player Count",
                "Drop loot = items from enemy death tables when killed. When enabled, apply LootMultiplicatorPlayerCountScaleRate per player above 4 (stacks with DropLootMultiplier).");

            ModConfig.DropLootMultiplier = ModConfig.CreateTrackedEntry(_category,
                "DropLootMultiplier",
                1f,
                "Drop Loot Multiplier",
                "Multiplier for enemy death drops: extra weighted re-rolls from drop tables and consumable stack count on spawn. 1 = vanilla, 2 = double.");

            ModConfig.LootItemFilterMode = ModConfig.CreateTrackedEntry(_category,
                "LootItemFilterMode",
                "All",
                "Loot Item Filter Mode",
                "All = every item can be scaled; AllowlistOnly = only comma-separated master IDs in LootAllowlist; BlocklistOnly = all items except LootBlocklist.");

            ModConfig.LootAllowlist = ModConfig.CreateTrackedEntry(_category,
                "LootAllowlist",
                "",
                "Loot Allowlist",
                "Comma-separated item master IDs (e.g. 12345,67890). Used when LootItemFilterMode is AllowlistOnly. See docs/LOOT_ITEM_IDS.md in the repo for the full list.");

            ModConfig.LootBlocklist = ModConfig.CreateTrackedEntry(_category,
                "LootBlocklist",
                "",
                "Loot Blocklist",
                "Comma-separated item master IDs to exclude from scaling. Used when LootItemFilterMode is BlocklistOnly. See docs/LOOT_ITEM_IDS.md in the repo for the full list.");

            ModConfig.ConvertFakeActorDyingDropChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ConvertFakeActorDyingDropChancePercent",
                30,
                "Convert Fake Death Drops To Real Chance",
                "Chance (0-100) that fake items dropped on enemy death (ActorDying, e.g. mimic inventory decoys) become real pickup loot. 0 = vanilla (fake items vanish on grab), 100 = always real. Monster drop-table loot is already real.");
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
