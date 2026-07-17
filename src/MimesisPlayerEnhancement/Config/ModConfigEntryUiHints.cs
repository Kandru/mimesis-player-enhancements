using MimesisPlayerEnhancement.Features.UserInterface;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Web Dashboard presentation hints for config entries (input widgets and visual grouping).
    /// </summary>
    internal static class ModConfigEntryUiHints
    {
        private const string LootSectionId = "MimesisPlayerEnhancement_LootMultiplicator";
        private const string DungeonSectionId = "MimesisPlayerEnhancement_DungeonRandomizer";
        private const string WeatherSectionId = "MimesisPlayerEnhancement_Weather";
        private const string SpawnScalingSectionId = "MimesisPlayerEnhancement_SpawnScaling";
        private const string MimicTuningSectionId = "MimesisPlayerEnhancement_MimicTuning";
        private const string UiSectionId = UiConfig.SectionId;

        private static readonly Dictionary<(string SectionId, string Key), string[]> SelectValuesByEntry =
            new(EntryKeyComparer.Instance)
            {
                [(LootSectionId, "LootItemFilterMode")] = ["All", "AllowlistOnly", "BlocklistOnly"],
                [(DungeonSectionId, "DungeonPickPoolMode")] = ["WidenVanilla", "AllActiveUniform"],
                [(DungeonSectionId, "DungeonSeedFlavor")] = DungeonSeedFlavorUtil.AllNames,
                [(WeatherSectionId, "WeatherMode")] = ["Vanilla", "Fixed", "Cycle"],
                [(WeatherSectionId, "FixedWeatherPreset")] = ["Sunny", "Rain", "HeavyRain", "Squall"],
                [(WeatherSectionId, "StartTimePreset")] = ["Vanilla", "Morning", "Noon", "Dusk", "Night", "Midnight"],
                [(SpawnScalingSectionId, "PeriodicSpawnWaitMode")] = ["Vanilla", "Fixed", "Random"],
                [(MimicTuningSectionId, "MimicVoiceTuningMode")] = ["Vanilla", "Custom"],
                [(MimicTuningSectionId, "MimicInventoryCopyMode")] = ["Vanilla", "Custom"],
                [(MimicTuningSectionId, "MimicInventoryCopyPickRule")] = ["MinDistance", "MaxDistance", "Random"],
                [(UiSectionId, "RoundStartSoundMode")] = ["Vanilla", "Random", "Specific"],
                [(UiSectionId, "CustomLoadingScreenMode")] = ["Vanilla", "Random", "Specific"],
            };

        internal static string ResolveInputKind(string sectionId, string key)
        {
            if (sectionId == LootSectionId
                && (string.Equals(key, "LootAllowlist", StringComparison.Ordinal)
                    || string.Equals(key, "LootBlocklist", StringComparison.Ordinal)))
            {
                return "ItemIdList";
            }

            if (sectionId == DungeonSectionId
                && (string.Equals(key, "DungeonAllowlist", StringComparison.Ordinal)
                    || string.Equals(key, "DungeonBlocklist", StringComparison.Ordinal)))
            {
                return "DungeonIdList";
            }

            if (sectionId == WeatherSectionId
                && string.Equals(key, "WeatherCyclePresets", StringComparison.Ordinal))
            {
                return "WeatherPresetList";
            }

            if (sectionId == UiSectionId
                && (string.Equals(key, "RoundStartSoundRandomPool", StringComparison.Ordinal)
                    || string.Equals(key, "CustomLoadingScreenRandomPool", StringComparison.Ordinal)))
            {
                return "VariantIdList";
            }

            if (sectionId == UiSectionId
                && string.Equals(key, "RoundStartSoundVariant", StringComparison.Ordinal))
            {
                return "Select";
            }

            if (sectionId == UiSectionId
                && string.Equals(key, "CustomLoadingScreenVariant", StringComparison.Ordinal))
            {
                return "Select";
            }

            if (SelectValuesByEntry.ContainsKey((sectionId, key)))
            {
                return "Select";
            }

            return "Default";
        }

        internal static void ApplyToEntry(WebDashboardConfigEntryDto entry, string sectionId, string key)
        {
            entry.InputKind = ResolveInputKind(sectionId, key);

            if (string.Equals(entry.InputKind, "VariantIdList", StringComparison.Ordinal))
            {
                if (sectionId == UiSectionId
                    && string.Equals(key, "RoundStartSoundRandomPool", StringComparison.Ordinal))
                {
                    entry.SelectOptions = BuildRoundStartSoundVariantOptions();
                }
                else if (sectionId == UiSectionId
                    && string.Equals(key, "CustomLoadingScreenRandomPool", StringComparison.Ordinal))
                {
                    entry.SelectOptions = BuildCustomLoadingScreenVariantOptions();
                }

                return;
            }

            if (!string.Equals(entry.InputKind, "Select", StringComparison.Ordinal))
            {
                return;
            }

            if (sectionId == UiSectionId
                && string.Equals(key, "RoundStartSoundVariant", StringComparison.Ordinal))
            {
                entry.SelectOptions = BuildRoundStartSoundVariantOptions();
                return;
            }

            if (sectionId == UiSectionId
                && string.Equals(key, "CustomLoadingScreenVariant", StringComparison.Ordinal))
            {
                entry.SelectOptions = BuildCustomLoadingScreenVariantOptions();
                return;
            }

            if (!SelectValuesByEntry.TryGetValue((sectionId, key), out string[]? values))
            {
                return;
            }

            List<WebDashboardConfigSelectOptionDto> options = [];
            foreach (string value in values)
            {
                string? label = WebDashboardL10n.GetConfigSelectOptionLabel(sectionId, key, value);
                options.Add(new WebDashboardConfigSelectOptionDto
                {
                    Value = value,
                    Label = string.IsNullOrWhiteSpace(label) ? value : label,
                });
            }

            entry.SelectOptions = options;
        }

        internal static void AssignEntryGroups(WebDashboardConfigSectionDto section)
        {
            // New explicit or auto-inferred groupIds need matching labels in l10n:
            // config.{sectionId}._groups.{groupId}
            string sectionId = section.Id;
            string? featureToggleKey = section.FeatureToggle?.Key;
            List<WebDashboardConfigEntryDto> entries = section.Entries;
            Dictionary<string, string> explicitGroups = GetExplicitGroups(sectionId);

            foreach (WebDashboardConfigEntryDto entry in entries)
            {
                if (explicitGroups.TryGetValue(entry.Key, out string? groupId))
                {
                    entry.EntryGroup = $"{sectionId}::{groupId}";
                }
            }

            for (int i = 0; i < entries.Count - 1; i++)
            {
                WebDashboardConfigEntryDto current = entries[i];
                WebDashboardConfigEntryDto next = entries[i + 1];
                if (!string.IsNullOrEmpty(current.EntryGroup) || !string.IsNullOrEmpty(next.EntryGroup))
                {
                    continue;
                }

                if (current.Key.StartsWith("AutoScale", StringComparison.Ordinal)
                    && current.Key.EndsWith("ByPlayerCount", StringComparison.Ordinal)
                    && next.Key.EndsWith("Multiplier", StringComparison.Ordinal))
                {
                    string groupId = $"{sectionId}::{current.Key}";
                    current.EntryGroup = groupId;
                    next.EntryGroup = groupId;
                    i++;
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                WebDashboardConfigEntryDto current = entries[i];
                if (!current.Key.StartsWith("Enable", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(current.Key, featureToggleKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(current.EntryGroup))
                {
                    continue;
                }

                string groupId = $"{sectionId}::{current.Key}";
                current.EntryGroup = groupId;
                for (int j = i + 1; j < entries.Count; j++)
                {
                    WebDashboardConfigEntryDto next = entries[j];
                    if (next.Key.StartsWith("Enable", StringComparison.Ordinal)
                        || next.Key.StartsWith("AutoScale", StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(next.EntryGroup))
                    {
                        break;
                    }

                    next.EntryGroup = groupId;
                }
            }
        }

        private static Dictionary<string, string> GetExplicitGroups(string sectionId)
        {
            // Each groupId value must have a label under config.{sectionId}._groups in l10n/*.json.
            if (sectionId == LootSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["LootItemFilterMode"] = "lootFilter",
                    ["LootAllowlist"] = "lootFilter",
                    ["LootBlocklist"] = "lootFilter",
                    ["AutoScaleMapLootBudgetForFilter"] = "lootFilter",
                };
            }

            if (sectionId == DungeonSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["RandomizeDungeonPick"] = "dungeonPick",
                    ["DungeonPickPoolMode"] = "dungeonPick",
                    ["DungeonAllowlist"] = "dungeonPick",
                    ["DungeonBlocklist"] = "dungeonPick",
                    ["IgnoreDungeonExcludeList"] = "dungeonPick",
                    ["DungeonSeedFlavor"] = "dungeonSeed",
                };
            }

            if (sectionId == SpawnScalingSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["PeriodicSpawnWaitMode"] = "periodicSpawnWait",
                    ["InitialPeriodicSpawnWaitSeconds"] = "periodicSpawnWait",
                    ["InitialPeriodicSpawnWaitMinSeconds"] = "periodicSpawnWait",
                    ["InitialPeriodicSpawnWaitMaxSeconds"] = "periodicSpawnWait",
                    ["PeriodicSpawnIntervalSeconds"] = "periodicSpawnWait",
                    ["PeriodicSpawnIntervalMinSeconds"] = "periodicSpawnWait",
                    ["PeriodicSpawnIntervalMaxSeconds"] = "periodicSpawnWait",
                };
            }

            if (sectionId == MimicTuningSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["EnableMimicPossessionTuning"] = "mimicPossession",
                    ["RandomizeMimicPossessionDuration"] = "mimicPossession",
                    ["MimicPossessionMinTimeSeconds"] = "mimicPossession",
                    ["MimicPossessionMaxTimeSeconds"] = "mimicPossession",
                    ["MimicPossessionCooltimeMultiplier"] = "mimicPossession",
                };
            }

            if (sectionId == WeatherSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["WeatherMode"] = "weatherMode",
                    ["FixedWeatherPreset"] = "weatherMode",
                    ["DisableRandomWeather"] = "weatherMode",
                    ["WeatherCyclePresets"] = "weatherCycle",
                    ["WeatherCycleMinDelaySeconds"] = "weatherCycle",
                    ["WeatherCycleMaxDelaySeconds"] = "weatherCycle",
                    ["StartTimePreset"] = "startTime",
                    ["EnableRealtimeTramClock"] = "startTime",
                };
            }

            if (sectionId == UiSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ModToastDurationSeconds"] = "modToasts",
                    ["EnableExtendedSaveSlots"] = "extendedSaveSlots",
                    ["EnableExtendedSpectatorPlayerList"] = "extendedSpectatorList",
                    ["EnableLoadingWaitPlayerList"] = "customLoadingScreen",
                    ["EnableExtendedInGameMenuPlayerList"] = "extendedInGameMenuPlayerList",
                    ["EnableDamageHealthGlow"] = "damageHealthGlow",
                    ["EnableFloatingDamageNumbers"] = "floatingDamage",
                    ["FloatingDamageDurationSeconds"] = "floatingDamage",
                    ["EnableFpsUi"] = "fpsUi",
                    ["EnableFpsUiInventoryNetWorth"] = "fpsUi",
                    ["RoundStartSoundMode"] = "roundStartSound",
                    ["RoundStartSoundRandomPool"] = "roundStartSound",
                    ["RoundStartSoundVariant"] = "roundStartSound",
                    ["RoundStartSoundVolume"] = "roundStartSound",
                    ["CustomLoadingScreenMode"] = "customLoadingScreen",
                    ["CustomLoadingScreenRandomPool"] = "customLoadingScreen",
                    ["CustomLoadingScreenVariant"] = "customLoadingScreen",
                    ["CustomLoadingScreenMotion"] = "customLoadingScreen",
                };
            }

            if (sectionId == PrivacyConfig.SectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["BlockReluTelemetry"] = "telemetry",
                    ["BlockReplayUpload"] = "replay",
                    ["BlockReplayRecording"] = "replay",
                    ["BlockCrashReports"] = "crashReports",
                    ["StripCrashReportMetadata"] = "crashReports",
                    ["BlockKraftonGppSdk"] = "thirdParty",
                };
            }

            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static List<WebDashboardConfigSelectOptionDto> BuildRoundStartSoundVariantOptions()
        {
            List<WebDashboardConfigSelectOptionDto> options = [];
            foreach (string value in RoundStartSoundResolver.ListVariantOptionValues())
            {
                string? label = WebDashboardL10n.GetConfigSelectOptionLabel(UiSectionId, "RoundStartSoundVariant", value);
                options.Add(new WebDashboardConfigSelectOptionDto
                {
                    Value = value,
                    Label = string.IsNullOrWhiteSpace(label)
                        ? RoundStartSoundResolver.FormatVariantDisplayName(value)
                        : label,
                });
            }

            return options;
        }

        private static List<WebDashboardConfigSelectOptionDto> BuildCustomLoadingScreenVariantOptions()
        {
            List<WebDashboardConfigSelectOptionDto> options = [];
            foreach (string value in CustomLoadingScreenResolver.ListVariantOptionValues())
            {
                string? label = WebDashboardL10n.GetConfigSelectOptionLabel(UiSectionId, "CustomLoadingScreenVariant", value);
                options.Add(new WebDashboardConfigSelectOptionDto
                {
                    Value = value,
                    Label = string.IsNullOrWhiteSpace(label)
                        ? CustomLoadingScreenResolver.FormatVariantDisplayName(value)
                        : label,
                });
            }

            return options;
        }

        private sealed class EntryKeyComparer : IEqualityComparer<(string SectionId, string Key)>
        {
            internal static readonly EntryKeyComparer Instance = new();

            public bool Equals((string SectionId, string Key) x, (string SectionId, string Key) y)
            {
                return string.Equals(x.SectionId, y.SectionId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string SectionId, string Key) obj)
            {
                return HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SectionId),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key));
            }
        }
    }
}
