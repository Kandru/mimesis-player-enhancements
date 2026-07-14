using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement
{
    internal readonly struct ModConfigEntryDependency
    {
        internal ModConfigEntryDependency(string dependsOnKey, string? dependsOnValue = null)
        {
            DependsOnKey = dependsOnKey;
            DependsOnValue = dependsOnValue;
        }

        internal string DependsOnKey { get; }
        internal string? DependsOnValue { get; }
    }

    /// <summary>
    /// Dashboard visibility rules mirroring backend resolver gating.
    /// </summary>
    internal static class ModConfigEntryDependencies
    {
        internal static bool IsEntryVisible(
            WebDashboardConfigSectionDto section,
            WebDashboardConfigEntryDto entry)
        {
            if (section.FeatureToggle != null
                && !string.Equals(entry.Key, section.FeatureToggle.Key, StringComparison.Ordinal)
                && !ParseBool(section.FeatureToggle.Value))
            {
                return false;
            }

            if (string.Equals(section.Id, "MimesisPlayerEnhancement_LootMultiplicator", StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.Key, "AutoScaleMapLootBudgetForFilter", StringComparison.Ordinal))
            {
                WebDashboardConfigEntryDto? filterMode = FindEntry(section, "LootItemFilterMode");
                if (filterMode != null
                    && string.Equals(filterMode.Value, "All", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (string.Equals(section.Id, "MimesisPlayerEnhancement_Weather", StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.Key, "DisableRandomWeather", StringComparison.Ordinal))
            {
                WebDashboardConfigEntryDto? weatherMode = FindEntry(section, "WeatherMode");
                if (weatherMode != null
                    && string.Equals(weatherMode.Value, "Vanilla", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (string.Equals(section.Id, PrivacyConfig.SectionId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.Key, "StripCrashReportMetadata", StringComparison.Ordinal))
            {
                WebDashboardConfigEntryDto? blockCrashReports = FindEntry(section, "BlockCrashReports");
                if (blockCrashReports != null && ParseBool(blockCrashReports.Value))
                {
                    return false;
                }
            }

            return IsDependencyChainVisible(section, entry.Key, []);
        }

        private static bool IsDependencyChainVisible(
            WebDashboardConfigSectionDto section,
            string key,
            HashSet<string> visited)
        {
            if (!visited.Add(key))
            {
                return true;
            }

            if (!TryGetDependency(section.Id, key, out ModConfigEntryDependency dependency))
            {
                return true;
            }

            WebDashboardConfigEntryDto? parent = FindEntry(section, dependency.DependsOnKey);
            if (parent == null)
            {
                return true;
            }

            if (!MatchesDependency(parent, dependency.DependsOnValue))
            {
                return false;
            }

            return IsDependencyChainVisible(section, dependency.DependsOnKey, visited);
        }

        internal static void ApplyToEntry(WebDashboardConfigSectionDto section, WebDashboardConfigEntryDto entry)
        {
            if (!TryGetDependency(section.Id, entry.Key, out ModConfigEntryDependency dependency))
            {
                return;
            }

            entry.DependsOnKey = dependency.DependsOnKey;
            entry.DependsOnValue = dependency.DependsOnValue;
        }

        private static WebDashboardConfigEntryDto? FindEntry(WebDashboardConfigSectionDto section, string key)
        {
            if (section.FeatureToggle != null
                && string.Equals(section.FeatureToggle.Key, key, StringComparison.Ordinal))
            {
                return section.FeatureToggle;
            }

            foreach (WebDashboardConfigEntryDto candidate in section.Entries)
            {
                if (string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool MatchesDependency(WebDashboardConfigEntryDto parent, string? expectedValue)
        {
            if (string.IsNullOrWhiteSpace(expectedValue))
            {
                return ParseBool(parent.Value);
            }

            if (string.Equals(expectedValue, ">0", StringComparison.Ordinal))
            {
                return int.TryParse(parent.Value, out int value) && value > 0;
            }

            return string.Equals(parent.Value, expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseBool(string? value) =>
            bool.TryParse(value, out bool parsed) && parsed;

        private static bool TryGetDependency(string sectionId, string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (sectionId == MimicTuningConfig.SectionId)
            {
                return TryGetMimicTuningDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_SpawnScaling")
            {
                return TryGetSpawnScalingDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_LootMultiplicator")
            {
                return TryGetLootDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_Economy")
            {
                return TryGetEconomyDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_DungeonRandomizer")
            {
                return TryGetDungeonRandomizerDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_Weather")
            {
                return TryGetWeatherDependency(key, out dependency);
            }

            return false;
        }

        private static bool TryGetMimicTuningDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "PeriodicVoiceIntervalMultiplier":
                case "PlayerVoiceResponseChancePercent":
                case "PlayerVoiceResponseCooldownSeconds":
                case "PlayerVoiceResponseDelayMinSeconds":
                case "PlayerVoiceResponseDelayMaxSeconds":
                case "PlayerVoiceResponseMaxDistance":
                    dependency = new ModConfigEntryDependency("MimicVoiceTuningMode", "Custom");
                    return true;
                case "MimicInventoryCopyPickRule":
                    dependency = new ModConfigEntryDependency("MimicInventoryCopyMode", "Custom");
                    return true;
                case "RandomizeMimicPossessionDuration":
                    dependency = new ModConfigEntryDependency("EnableMimicPossessionTuning");
                    return true;
                case "MimicPossessionMinTimeSeconds":
                case "MimicPossessionMaxTimeSeconds":
                    dependency = new ModConfigEntryDependency("RandomizeMimicPossessionDuration");
                    return true;
                case "MimicPossessionCooltimeMultiplier":
                    dependency = new ModConfigEntryDependency("EnableMimicPossessionTuning");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetSpawnScalingDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            if (IsAutoScaleMultiplier(key))
            {
                dependency = new ModConfigEntryDependency(GetAutoScaleToggleKey(key));
                return true;
            }

            switch (key)
            {
                case "InitialPeriodicSpawnWaitSeconds":
                case "PeriodicSpawnIntervalSeconds":
                    dependency = new ModConfigEntryDependency("PeriodicSpawnWaitMode", "Fixed");
                    return true;
                case "InitialPeriodicSpawnWaitMinSeconds":
                case "InitialPeriodicSpawnWaitMaxSeconds":
                case "PeriodicSpawnIntervalMinSeconds":
                case "PeriodicSpawnIntervalMaxSeconds":
                    dependency = new ModConfigEntryDependency("PeriodicSpawnWaitMode", "Random");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetLootDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "LootAllowlist":
                    dependency = new ModConfigEntryDependency("LootItemFilterMode", "AllowlistOnly");
                    return true;
                case "LootBlocklist":
                    dependency = new ModConfigEntryDependency("LootItemFilterMode", "BlocklistOnly");
                    return true;
                default:
                    if (IsAutoScaleMultiplier(key))
                    {
                        dependency = new ModConfigEntryDependency(GetAutoScaleToggleKey(key));
                        return true;
                    }

                    return false;
            }
        }

        private static bool TryGetEconomyDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "ShopDiscountMinPercent":
                case "ShopDiscountMaxPercent":
                    dependency = new ModConfigEntryDependency("ShopDiscountChancePercent", ">0");
                    return true;
                default:
                    if (IsAutoScaleMultiplier(key))
                    {
                        dependency = new ModConfigEntryDependency(GetAutoScaleToggleKey(key));
                        return true;
                    }

                    return false;
            }
        }

        private static bool TryGetDungeonRandomizerDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "DungeonPickPoolMode":
                case "DungeonAllowlist":
                case "DungeonBlocklist":
                case "IgnoreDungeonExcludeList":
                    dependency = new ModConfigEntryDependency("RandomizeDungeonPick");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetWeatherDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "FixedWeatherPreset":
                    dependency = new ModConfigEntryDependency("WeatherMode", "Fixed");
                    return true;
                case "DisableRandomWeather":
                    dependency = new ModConfigEntryDependency("WeatherMode", "Vanilla");
                    return true;
                case "WeatherCyclePresets":
                case "WeatherCycleMinDelaySeconds":
                case "WeatherCycleMaxDelaySeconds":
                    dependency = new ModConfigEntryDependency("WeatherMode", "Cycle");
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAutoScaleMultiplier(string key) =>
            MultiplierToAutoScaleToggle.ContainsKey(key);

        private static string GetAutoScaleToggleKey(string multiplierKey) =>
            MultiplierToAutoScaleToggle[multiplierKey];

        private static readonly Dictionary<string, string> MultiplierToAutoScaleToggle =
            new(StringComparer.Ordinal)
            {
                ["MimicSpawnMultiplier"] = "AutoScaleMimicSpawnsByPlayerCount",
                ["BossSpawnMultiplier"] = "AutoScaleBossSpawnsByPlayerCount",
                ["JakoSpawnMultiplier"] = "AutoScaleJakoSpawnsByPlayerCount",
                ["SpecialSpawnMultiplier"] = "AutoScaleSpecialSpawnsByPlayerCount",
                ["TrapSpawnMultiplier"] = "AutoScaleTrapSpawnsByPlayerCount",
                ["OtherSpawnMultiplier"] = "AutoScaleOtherSpawnsByPlayerCount",
                ["MapLootMultiplier"] = "AutoScaleMapLootByPlayerCount",
                ["DropLootMultiplier"] = "AutoScaleDropLootByPlayerCount",
                ["StartupMoneyMultiplier"] = "AutoScaleStartupMoneyByPlayerCount",
                ["ScrapSellValueMultiplier"] = "AutoScaleScrapSellValueByPlayerCount",
                ["ShopBuyPriceMultiplier"] = "AutoScaleShopBuyPriceByPlayerCount",
                ["ReinforcePriceMultiplier"] = "AutoScaleReinforcePriceByPlayerCount",
            };
    }
}
