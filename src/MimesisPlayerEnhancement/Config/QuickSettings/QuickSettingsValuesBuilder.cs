using System;
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

        internal static void SetAllAutoScaleByPlayerCount(
            Dictionary<string, Dictionary<string, string>> map,
            bool enabled)
        {
            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!key.StartsWith("AutoScale", StringComparison.Ordinal)
                        || !key.EndsWith("ByPlayerCount", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
                    {
                        SetBool(map, sectionId, key, enabled);
                    }
                }
            }
        }

        internal static void SetAllPlayerCountScaleRates(
            Dictionary<string, Dictionary<string, string>> map,
            float rate)
        {
            string formatted = ModConfigFloatHelper.Format(rate);
            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!key.EndsWith("PlayerCountScaleRate", StringComparison.Ordinal))
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
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "JakoSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "SpecialSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "TrapSpawnMultiplier", formatted);
            Set(map, "MimesisPlayerEnhancement_SpawnScaling", "OtherSpawnMultiplier", formatted);
        }

        internal static void SetPeriodicSpawnWaitRandom(
            Dictionary<string, Dictionary<string, string>> map,
            float initialMinSeconds,
            float initialMaxSeconds,
            float intervalMinSeconds,
            float intervalMaxSeconds)
        {
            const string sectionId = "MimesisPlayerEnhancement_SpawnScaling";
            Set(map, sectionId, "PeriodicSpawnWaitMode", "Random");
            Set(map, sectionId, "InitialPeriodicSpawnWaitMinSeconds", ModConfigFloatHelper.Format(initialMinSeconds));
            Set(map, sectionId, "InitialPeriodicSpawnWaitMaxSeconds", ModConfigFloatHelper.Format(initialMaxSeconds));
            Set(map, sectionId, "PeriodicSpawnIntervalMinSeconds", ModConfigFloatHelper.Format(intervalMinSeconds));
            Set(map, sectionId, "PeriodicSpawnIntervalMaxSeconds", ModConfigFloatHelper.Format(intervalMaxSeconds));
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
            float startup,
            float roundGoal,
            float scrap,
            float shop,
            float reinforce)
        {
            Set(map, "MimesisPlayerEnhancement_Economy", "StartupMoneyMultiplier", ModConfigFloatHelper.Format(startup));
            Set(map, "MimesisPlayerEnhancement_Economy", "RoundGoalMoneyMultiplier", ModConfigFloatHelper.Format(roundGoal));
            Set(map, "MimesisPlayerEnhancement_Economy", "ScrapSellValueMultiplier", ModConfigFloatHelper.Format(scrap));
            Set(map, "MimesisPlayerEnhancement_Economy", "ShopBuyPriceMultiplier", ModConfigFloatHelper.Format(shop));
            Set(map, "MimesisPlayerEnhancement_Economy", "ReinforcePriceMultiplier", ModConfigFloatHelper.Format(reinforce));
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
