using System;
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

            return "Default";
        }

        internal static void ApplyToEntry(WebDashboardConfigEntryDto entry, string sectionId, string key)
        {
            entry.InputKind = ResolveInputKind(sectionId, key);
        }

        internal static void AssignEntryGroups(WebDashboardConfigSectionDto section)
        {
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
            if (sectionId == LootSectionId)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["LootItemFilterMode"] = "lootFilter",
                    ["LootAllowlist"] = "lootFilter",
                    ["LootBlocklist"] = "lootFilter",
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
                };
            }

            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
