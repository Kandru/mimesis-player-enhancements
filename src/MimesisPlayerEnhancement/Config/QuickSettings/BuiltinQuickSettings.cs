using System;
namespace MimesisPlayerEnhancement.Config.QuickSettings
{
    internal static class BuiltinQuickSettings
    {
        internal const string LootPicnicId = "builtin:loot_picnic";
        internal const string AsIntendedId = "builtin:as_intended";
        internal const string FullHouseId = "builtin:full_house";
        internal const string AbandonHopeId = "builtin:abandon_hope";

        private static readonly Dictionary<string, QuickSettingPreset> Presets =
            new(StringComparer.OrdinalIgnoreCase);

        internal static void EnsureInitialized()
        {
            if (Presets.Count > 0)
            {
                return;
            }

            Register(BuildLootPicnic());
            Register(BuildAsIntended());
            Register(BuildFullHouse());
            Register(BuildAbandonHope());
        }

        internal static IReadOnlyCollection<QuickSettingPreset> GetAll()
        {
            EnsureInitialized();
            return Presets.Values;
        }

        internal static bool TryGet(string presetId, out QuickSettingPreset preset)
        {
            EnsureInitialized();
            return Presets.TryGetValue(presetId, out preset!);
        }

        internal static bool IsBuiltin(string presetId)
        {
            EnsureInitialized();
            return Presets.ContainsKey(presetId);
        }

        private static void Register(QuickSettingPreset preset)
        {
            Presets[preset.Id] = preset;
        }

        private static QuickSettingPreset BuildLootPicnic()
        {
            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CreateMap();
            QuickSettingsValuesBuilder.SetAllFeatureEnables(values, false);
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_MorePlayers", "EnableMorePlayers", true);
            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_MorePlayers", "MaxPlayers", "16");
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_JoinAnytime", "EnableJoinAnytime", true);
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_LootMultiplicator", "EnableLootMultiplicator", true);
            QuickSettingsValuesBuilder.SetLootMultipliers(values, mapLoot: 3f, dropLoot: 2f);
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_SpawnScaling", "EnableSpawnScaling", true);
            QuickSettingsValuesBuilder.SetSpawnMultipliers(values, 0.25f);
            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_SpawnScaling", "MimicSpawnMultiplier", "0.3");
            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_SpawnScaling", "BossSpawnMultiplier", "0.5");
            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_SpawnScaling", "SpecialSpawnMultiplier", "0.4");

            return new QuickSettingPreset
            {
                Id = LootPicnicId,
                Revision = 1,
                IsBuiltin = true,
                Values = values,
            };
        }

        private static QuickSettingPreset BuildAsIntended()
        {
            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CreateMap();
            QuickSettingsValuesBuilder.SetAllFeatureEnables(values, false);
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_MorePlayers", "EnableMorePlayers", true);
            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_MorePlayers", "MaxPlayers", "16");
            QuickSettingsValuesBuilder.SetBool(values, "MimesisPlayerEnhancement_JoinAnytime", "EnableJoinAnytime", true);

            return new QuickSettingPreset
            {
                Id = AsIntendedId,
                Revision = 1,
                IsBuiltin = true,
                Values = values,
            };
        }

        private static QuickSettingPreset BuildFullHouse()
        {
            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CreateMap();
            QuickSettingsValuesBuilder.SetAllFeatureEnables(values, false);

            string[] enabledSections =
            [
                "MimesisPlayerEnhancement_SpawnScaling",
                "MimesisPlayerEnhancement_LootMultiplicator",
                "MimesisPlayerEnhancement_Economy",
                "MimesisPlayerEnhancement_DungeonTime",
                "MimesisPlayerEnhancement_PlayerTuning",
                "MimesisPlayerEnhancement_Statistics",
                "MimesisPlayerEnhancement_Persistence",
                "MimesisPlayerEnhancement_JoinAnytime",
                "MimesisPlayerEnhancement_MorePlayers",
            ];

            foreach (string sectionId in enabledSections)
            {
                if (ModConfigRegistry.TryGetFeatureToggleKey(sectionId, out string toggleKey))
                {
                    QuickSettingsValuesBuilder.SetBool(values, sectionId, toggleKey, true);
                }
            }

            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_MorePlayers", "MaxPlayers", "16");
            QuickSettingsValuesBuilder.SetAllAutoScaleByPlayerCount(values, true);
            QuickSettingsValuesBuilder.SetAllPlayerCountScaleRates(values, 0.10f);
            QuickSettingsValuesBuilder.SetSpawnMultipliers(values, 1.25f);
            QuickSettingsValuesBuilder.SetLootMultipliers(values, mapLoot: 1.25f, dropLoot: 1.25f);
            QuickSettingsValuesBuilder.SetEconomyMultipliers(values, startup: 1.25f, roundGoal: 1.25f, scrap: 1.25f, shop: 1.25f, reinforce: 1.25f);

            return new QuickSettingPreset
            {
                Id = FullHouseId,
                Revision = 1,
                IsBuiltin = true,
                Values = values,
            };
        }

        private static QuickSettingPreset BuildAbandonHope()
        {
            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CreateMap();
            QuickSettingsValuesBuilder.SetAllFeatureEnables(values, false);

            string[] enabledSections =
            [
                "MimesisPlayerEnhancement_SpawnScaling",
                "MimesisPlayerEnhancement_LootMultiplicator",
                "MimesisPlayerEnhancement_Economy",
                "MimesisPlayerEnhancement_DungeonTime",
                "MimesisPlayerEnhancement_PlayerTuning",
                "MimesisPlayerEnhancement_Statistics",
                "MimesisPlayerEnhancement_Persistence",
                "MimesisPlayerEnhancement_JoinAnytime",
                "MimesisPlayerEnhancement_MorePlayers",
                "MimesisPlayerEnhancement_DeadPlayerFeatures",
            ];

            foreach (string sectionId in enabledSections)
            {
                if (ModConfigRegistry.TryGetFeatureToggleKey(sectionId, out string toggleKey))
                {
                    QuickSettingsValuesBuilder.SetBool(values, sectionId, toggleKey, true);
                }
            }

            QuickSettingsValuesBuilder.Set(values, "MimesisPlayerEnhancement_MorePlayers", "MaxPlayers", "16");
            QuickSettingsValuesBuilder.SetAllAutoScaleByPlayerCount(values, true);
            QuickSettingsValuesBuilder.SetAllPlayerCountScaleRates(values, 0.10f);
            QuickSettingsValuesBuilder.SetSpawnMultipliers(values, 1.9f);
            QuickSettingsValuesBuilder.SetLootMultipliers(values, mapLoot: 1.8f, dropLoot: 1.8f);
            QuickSettingsValuesBuilder.SetEconomyMultipliers(values, startup: 1.5f, roundGoal: 2f, scrap: 0.8f, shop: 1.8f, reinforce: 1.8f);

            return new QuickSettingPreset
            {
                Id = AbandonHopeId,
                Revision = 1,
                IsBuiltin = true,
                Values = values,
            };
        }
    }
}
