using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardConfigBridge
    {
        private static readonly string[] CategoryOrder =
        [
            "MimesisPlayerEnhancement",
            "MimesisPlayerEnhancement_MorePlayers",
            "MimesisPlayerEnhancement_MoreVoices",
            "MimesisPlayerEnhancement_Persistence",
            "MimesisPlayerEnhancement_Statistics",
            "MimesisPlayerEnhancement_PlayerAnnouncements",
            "MimesisPlayerEnhancement_JoinAnytime",
            "MimesisPlayerEnhancement_SpawnScaling",
            "MimesisPlayerEnhancement_LootMultiplicator",
            "MimesisPlayerEnhancement_MoneyMultiplier",
            "MimesisPlayerEnhancement_DungeonTime",
            "MimesisPlayerEnhancement_DungeonSizeScaling",
            "MimesisPlayerEnhancement_SpectatorTransition",
            "MimesisPlayerEnhancement_DungeonRandomizer",
            "MimesisPlayerEnhancement_WebDashboard",
        ];

        internal static WebDashboardSettingsDto BuildSettings()
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardSettingsDto
                {
                    ConfigPath = ModConfig.FilePath,
                    ConfigVersion = ModConfig.Version,
                };
            }

            Dictionary<string, WebDashboardConfigSectionDto> sections = [];
            List<string> discoveredOrder = [];

            foreach (PropertyInfo property in typeof(ModConfig).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                Type propertyType = property.PropertyType;
                if (!propertyType.IsGenericType
                    || propertyType.GetGenericTypeDefinition() != typeof(MelonPreferences_Entry<>))
                {
                    continue;
                }

                object? raw = property.GetValue(null);
                if (raw is not MelonPreferences_Entry entry)
                {
                    continue;
                }

                MelonPreferences_Category category = entry.Category;
                string sectionId = category.Identifier;
                if (!sections.TryGetValue(sectionId, out WebDashboardConfigSectionDto? section))
                {
                    section = new WebDashboardConfigSectionDto
                    {
                        Id = sectionId,
                        Title = category.DisplayName ?? sectionId,
                    };
                    sections[sectionId] = section;
                    discoveredOrder.Add(sectionId);
                }

                string typeName = entry.GetReflectedType()?.Name ?? "Unknown";
                string value = SafeGetString(entry.GetValueAsString);
                string defaultValue = SafeGetString(entry.GetDefaultValueAsString);

                section.Entries.Add(new WebDashboardConfigEntryDto
                {
                    Key = entry.Identifier,
                    Title = entry.DisplayName ?? entry.Identifier,
                    Description = entry.Description ?? "",
                    Type = typeName,
                    Value = value,
                    DefaultValue = defaultValue,
                    IsHidden = entry.IsHidden,
                });
            }

            List<WebDashboardConfigSectionDto> ordered = [];
            HashSet<string> added = [];

            foreach (string sectionId in CategoryOrder)
            {
                if (sections.TryGetValue(sectionId, out WebDashboardConfigSectionDto? section))
                {
                    SortEntries(section);
                    ordered.Add(section);
                    _ = added.Add(sectionId);
                }
            }

            foreach (string sectionId in discoveredOrder)
            {
                if (added.Contains(sectionId))
                {
                    continue;
                }

                WebDashboardConfigSectionDto section = sections[sectionId];
                SortEntries(section);
                ordered.Add(section);
            }

            return new WebDashboardSettingsDto
            {
                ConfigPath = ModConfig.FilePath,
                ConfigVersion = ModConfig.Version,
                Sections = ordered,
            };
        }

        internal static WebDashboardConfigUpdateResult ApplyUpdate(string sectionId, string key, string value)
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Configuration is not initialized.",
                };
            }

            if (!ModConfig.TrySetEntryValue(sectionId, key, value, out string? error))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = error ?? "Invalid value.",
                };
            }

            ModConfig.SaveToFile();

            return !ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null
                ? new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = "Saved.",
                }
                : new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = "Saved.",
                    SectionId = sectionId,
                    Key = key,
                    Value = SafeGetString(entry.GetValueAsString),
                    Type = entry.GetReflectedType()?.Name ?? "Unknown",
                };
        }

        private static void SortEntries(WebDashboardConfigSectionDto section)
        {
            section.Entries.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        }

        private static string SafeGetString(Func<string> getter)
        {
            try
            {
                return getter() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
