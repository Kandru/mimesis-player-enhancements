using System;
using System.Collections.Generic;
using MelonLoader;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal enum WebDashboardConfigScope
    {
        Global,
        Save,
    }

    internal static class WebDashboardConfigBridge
    {
        private const string Feature = "WebDashboard";

        internal static WebDashboardSettingsDto BuildGlobalSettings()
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardSettingsDto
                {
                    ConfigPath = ModConfig.FilePath,
                    ConfigVersion = ModConfig.Version,
                    Scope = "global",
                };
            }

            return new WebDashboardSettingsDto
            {
                ConfigPath = ModConfig.FilePath,
                ConfigVersion = ModConfig.Version,
                Scope = "global",
                Sections = BuildSections(includeGlobalOnly: true, saveSlotId: -1, saveScope: false),
            };
        }

        internal static WebDashboardSettingsDto BuildSaveSettings(int slotId)
        {
            string? overridePath = SaveSlotConfigStore.GetOverrideFilePath(slotId);
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardSettingsDto
                {
                    ConfigPath = overridePath ?? "",
                    ConfigVersion = ModConfig.Version,
                    SaveSlotId = slotId,
                    Scope = "save",
                };
            }

            return new WebDashboardSettingsDto
            {
                ConfigPath = overridePath ?? "",
                ConfigVersion = ModConfig.Version,
                SaveSlotId = slotId,
                Scope = "save",
                Sections = BuildSections(includeGlobalOnly: false, saveSlotId: slotId, saveScope: true),
            };
        }

        internal static WebDashboardConfigUpdateResult ApplyGlobalUpdate(string sectionId, string key, string value)
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Configuration is not initialized.",
                };
            }

            if (ModConfigRegistry.IsWebDashboardSection(sectionId))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Web dashboard settings cannot be changed from the web UI. Edit the config file instead.",
                };
            }

            if (!ModConfigRegistry.TryNormalizeRawValue(sectionId, key, value, out string normalized, out string? error))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = error ?? "Invalid value.",
                };
            }

            ModConfigChangeTracker.BeginBatch();
            try
            {
                if (!ModConfigRegistry.TrySetEntryValue(sectionId, key, normalized, out error))
                {
                    return new WebDashboardConfigUpdateResult
                    {
                        Success = false,
                        Message = error ?? "Failed to apply setting.",
                    };
                }

                ModConfig.SanitizeFloatEntries();

                if (!GlobalConfigStore.TryWriteValue(sectionId, key, normalized, out error, waitForCompletion: false))
                {
                    return new WebDashboardConfigUpdateResult
                    {
                        Success = false,
                        Message = error ?? "Failed to save global config.",
                    };
                }

                int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
                if (activeSlotId < 0 && MimesisSaveManager.TryGetActiveSaveSlotId(out int resolvedSlotId))
                {
                    activeSlotId = resolvedSlotId;
                }

                if (activeSlotId >= 0)
                {
                    ReconcileActiveSaveOverride(activeSlotId, sectionId, key, normalized, out error);
                }
            }
            finally
            {
                ModConfigChangeTracker.EndBatch();
            }

            return BuildUpdateResult(sectionId, key, normalized, "Saved to global config.");
        }

        internal static WebDashboardConfigUpdateResult ApplySaveUpdate(int slotId, string sectionId, string key, string value)
        {
            if (ModConfigRegistry.IsWebDashboardSection(sectionId))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Web dashboard settings cannot be overridden per save slot.",
                };
            }

            if (!SaveSlotConfigStore.TrySetOverride(slotId, sectionId, key, value, out string? error, waitForCompletion: false))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = error ?? "Invalid value.",
                };
            }

            if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = "Saved to save slot overrides.",
                    ConfigVersion = ModConfig.Version,
                    SectionId = sectionId,
                    Key = key,
                    Value = value,
                };
            }

            string effectiveValue = ModConfigRegistry.FormatEntryValue(entry);
            bool isOverridden = SaveSlotConfigStore.IsOverridden(slotId, sectionId, key);
            return BuildSaveUpdateResult(sectionId, key, effectiveValue, isOverridden, entry);
        }

        private static void ReconcileActiveSaveOverride(
            int slotId,
            string sectionId,
            string key,
            string normalizedGlobal,
            out string? error)
        {
            error = null;

            if (!SaveSlotConfigStore.TryGetOverrideRaw(slotId, sectionId, key, out string overrideRaw))
            {
                return;
            }

            if (ModConfigRegistry.RawValuesEqual(sectionId, key, overrideRaw, normalizedGlobal))
            {
                SaveSlotConfigStore.ClearOverrideKey(slotId, sectionId, key);
                return;
            }

            if (!ModConfigRegistry.TrySetEntryValue(sectionId, key, overrideRaw, out error))
            {
                ModLog.Warn(Feature, $"Save override {sectionId}/{key} for slot {slotId} could not be re-applied: {error}");
            }
        }

        private static WebDashboardConfigUpdateResult BuildUpdateResult(
            string sectionId,
            string key,
            string savedValue,
            string message)
        {
            if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = message,
                    ConfigVersion = ModConfig.Version,
                    SectionId = sectionId,
                    Key = key,
                    Value = savedValue,
                };
            }

            return new WebDashboardConfigUpdateResult
            {
                Success = true,
                Message = message,
                ConfigVersion = ModConfig.Version,
                SectionId = sectionId,
                Key = key,
                Value = savedValue,
                Type = entry.GetReflectedType()?.Name ?? "Unknown",
            };
        }

        private static WebDashboardConfigUpdateResult BuildSaveUpdateResult(
            string sectionId,
            string key,
            string effectiveValue,
            bool isOverridden,
            MelonPreferences_Entry entry)
        {
            return new WebDashboardConfigUpdateResult
            {
                Success = true,
                Message = "Saved to save slot overrides.",
                ConfigVersion = ModConfig.Version,
                SectionId = sectionId,
                Key = key,
                Value = effectiveValue,
                Type = entry.GetReflectedType()?.Name ?? "Unknown",
                IsOverridden = isOverridden,
            };
        }

        private static List<WebDashboardConfigSectionDto> BuildSections(
            bool includeGlobalOnly,
            int saveSlotId,
            bool saveScope)
        {
            List<WebDashboardConfigSectionDto> sections = [];

            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                if (ModConfigRegistry.IsWebDashboardSection(sectionId))
                {
                    continue;
                }

                if (!ModConfigRegistry.TryGetSectionTitle(sectionId, out string title))
                {
                    title = sectionId;
                }

                WebDashboardConfigSectionDto section = new()
                {
                    Id = sectionId,
                    Title = title,
                };

                ModConfigRegistry.TryGetFeatureToggleKey(sectionId, out string featureToggleKey);

                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                    {
                        continue;
                    }

                    bool allowSaveOverride = ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key);
                    if (saveScope && !allowSaveOverride)
                    {
                        continue;
                    }

                    if (!includeGlobalOnly && !allowSaveOverride)
                    {
                        continue;
                    }

                    string globalValue = ModConfigRegistry.TryGetGlobalRawValue(sectionId, key, out string globalRaw)
                        ? globalRaw
                        : ModConfigRegistry.FormatEntryDefaultValue(entry);

                    bool isOverridden = saveScope
                        && SaveSlotConfigStore.IsOverridden(saveSlotId, sectionId, key);

                    WebDashboardConfigEntryDto entryDto = new()
                    {
                        Key = entry.Identifier,
                        Title = entry.DisplayName ?? entry.Identifier,
                        Description = entry.Description ?? "",
                        Type = entry.GetReflectedType()?.Name ?? "Unknown",
                        Value = saveScope
                            ? ModConfigRegistry.FormatEntryValue(entry)
                            : ModConfigRegistry.FormatEntryValue(entry),
                        DefaultValue = ModConfigRegistry.FormatEntryDefaultValue(entry),
                        GlobalValue = globalValue,
                        IsOverridden = isOverridden,
                        IsHidden = entry.IsHidden,
                    };

                    if (!string.IsNullOrEmpty(featureToggleKey)
                        && string.Equals(key, featureToggleKey, StringComparison.Ordinal))
                    {
                        section.FeatureToggle = entryDto;
                        continue;
                    }

                    section.Entries.Add(entryDto);
                }

                if (section.FeatureToggle != null || section.Entries.Count > 0)
                {
                    sections.Add(section);
                }
            }

            return sections;
        }
    }
}
