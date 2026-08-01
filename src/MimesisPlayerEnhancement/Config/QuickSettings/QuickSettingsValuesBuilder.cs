using System.Globalization;
using MelonLoader;

namespace MimesisPlayerEnhancement.Config.QuickSettings
{
    internal static class QuickSettingsValuesBuilder
    {
        internal static Dictionary<string, Dictionary<string, string>> CreateMap()
        {
            return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        internal static void Set(
            Dictionary<string, Dictionary<string, string>> map,
            string sectionId,
            string key,
            string value)
        {
            if (!map.TryGetValue(sectionId, out Dictionary<string, string>? keys))
            {
                keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                map[sectionId] = keys;
            }

            keys[key] = value;
        }

        internal static void SetBool(
            Dictionary<string, Dictionary<string, string>> map,
            string sectionId,
            string key,
            bool value)
        {
            Set(map, sectionId, key, value ? "true" : "false");
        }

        internal static void SetAllFeatureEnables(
            Dictionary<string, Dictionary<string, string>> map,
            bool enabled)
        {
            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                if (ModConfigRegistry.IsGlobalOnlySection(sectionId))
                {
                    continue;
                }

                if (ModConfigRegistry.TryGetFeatureToggleKey(sectionId, out string toggleKey))
                {
                    if (ModConfigRegistry.IsSaveOverrideAllowed(sectionId, toggleKey))
                    {
                        SetBool(map, sectionId, toggleKey, enabled);
                    }
                }
            }
        }

        internal static void SetAllPerPlayerMultipliers(
            Dictionary<string, Dictionary<string, string>> map,
            float value)
        {
            string formatted = ModConfigFloatHelper.Format(value);
            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!key.EndsWith("PerPlayerMultiplier", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
                    {
                        Set(map, sectionId, key, formatted);
                    }
                }
            }
        }

        internal static void SetSpawnMultipliers(
            Dictionary<string, Dictionary<string, string>> map,
            float multiplier)
        {
            string formatted = ModConfigFloatHelper.Format(multiplier);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "MimicSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "BossSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "GruntSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "SpecialSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "TrapSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "OtherSpawnMultiplier", formatted);
        }

        internal static void SetAmbientWaveRandom(
            Dictionary<string, Dictionary<string, string>> map,
            float initialMinSeconds,
            float initialMaxSeconds,
            float intervalMinSeconds,
            float intervalMaxSeconds)
        {
            SetWaveRandom(map, "MimicWave", initialMinSeconds, initialMaxSeconds, intervalMinSeconds, intervalMaxSeconds);
            SetWaveRandom(map, "GruntWave", initialMinSeconds, initialMaxSeconds, intervalMinSeconds, intervalMaxSeconds);
        }

        private static void SetWaveRandom(
            Dictionary<string, Dictionary<string, string>> map,
            string prefix,
            float initialMinSeconds,
            float initialMaxSeconds,
            float intervalMinSeconds,
            float intervalMaxSeconds)
        {
            const string sectionId = "MimesisPlayerEnhancement_SpawnScaling";
            Set(map, sectionId, $"{prefix}Mode", "Random");
            Set(map, sectionId, $"{prefix}InitialDelayMinSeconds", ModConfigFloatHelper.Format(initialMinSeconds));
            Set(map, sectionId, $"{prefix}InitialDelayMaxSeconds", ModConfigFloatHelper.Format(initialMaxSeconds));
            Set(map, sectionId, $"{prefix}IntervalMinSeconds", ModConfigFloatHelper.Format(intervalMinSeconds));
            Set(map, sectionId, $"{prefix}IntervalMaxSeconds", ModConfigFloatHelper.Format(intervalMaxSeconds));
        }

        internal static void SetLootMultipliers(
            Dictionary<string, Dictionary<string, string>> map,
            float mapLoot,
            float dropLoot)
        {
            Set(map, "MimesisPlayerEnhancement_LootMultiplicator", "MapLootMultiplier", ModConfigFloatHelper.Format(mapLoot));
            Set(map, "MimesisPlayerEnhancement_LootMultiplicator", "DropLootMultiplier", ModConfigFloatHelper.Format(dropLoot));
        }

        internal static void SetEconomyMultipliers(
            Dictionary<string, Dictionary<string, string>> map,
            int startupMoney,
            float scrap,
            float shop,
            float reinforce)
        {
            Set(map, SavegamePreparationConfig.SectionId, "StartupMoney", startupMoney.ToString(CultureInfo.InvariantCulture));
            Set(map, "MimesisPlayerEnhancement_Economy", "ScrapSellValueMultiplier", ModConfigFloatHelper.Format(scrap));
            Set(map, "MimesisPlayerEnhancement_Economy", "ShopBuyPriceMultiplier", ModConfigFloatHelper.Format(shop));
            Set(map, "MimesisPlayerEnhancement_Economy", "ReinforcePriceMultiplier", ModConfigFloatHelper.Format(reinforce));
        }

        internal static void SetRoundGoalMultipliers(
            Dictionary<string, Dictionary<string, string>> map,
            float moneyMultiplier,
            float basePerZone,
            float curveExponent)
        {
            Set(map, "MimesisPlayerEnhancement_MorePlayers", "RoundGoalMoneyMultiplier", ModConfigFloatHelper.Format(moneyMultiplier));
            Set(map, "MimesisPlayerEnhancement_MorePlayers", "RoundGoalBasePerZone", ModConfigFloatHelper.Format(basePerZone));
            Set(map, "MimesisPlayerEnhancement_MorePlayers", "RoundGoalCurveExponent", ModConfigFloatHelper.Format(curveExponent));
        }

        internal static Dictionary<string, Dictionary<string, string>> CloneValues(
            Dictionary<string, Dictionary<string, string>> source)
        {
            Dictionary<string, Dictionary<string, string>> clone =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, Dictionary<string, string>> section in source)
            {
                clone[section.Key] = new Dictionary<string, string>(section.Value, StringComparer.OrdinalIgnoreCase);
            }

            return clone;
        }

        internal static Dictionary<string, Dictionary<string, string>> CollectEffectiveValues()
        {
            Dictionary<string, Dictionary<string, string>> values = CreateMap();
            if (!ModConfig.IsInitialized)
            {
                return values;
            }

            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
                    {
                        continue;
                    }

                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                    {
                        continue;
                    }

                    Set(values, sectionId, key, ModConfigRegistry.FormatEntryValue(entry));
                }
            }

            return values;
        }

        internal static Dictionary<string, Dictionary<string, string>> CollectValuesDifferingFromGlobal()
        {
            Dictionary<string, Dictionary<string, string>> values = CreateMap();
            if (!ModConfig.IsInitialized)
            {
                return values;
            }

            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
                    {
                        continue;
                    }

                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                    {
                        continue;
                    }

                    string effective = ModConfigRegistry.FormatEntryValue(entry);
                    string global = ModConfigRegistry.TryGetGlobalRawValue(sectionId, key, out string globalRaw)
                        ? globalRaw
                        : ModConfigRegistry.FormatEntryDefaultValue(entry);

                    if (!ModConfigRegistry.RawValuesEqual(sectionId, key, effective, global))
                    {
                        Set(values, sectionId, key, effective);
                    }
                }
            }

            return values;
        }
    }
}
