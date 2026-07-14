using MelonLoader;
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

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

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
                Sections = BuildSections(WebDashboardConfigScope.Global, saveSlotId: -1),
            };
        }

        internal static WebDashboardSettingsDto BuildSaveSettings(int slotId)
        {
            if (MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            }

            string? documentPath = SaveSlotDocumentStore.GetDocumentPath(slotId);
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardSettingsDto
                {
                    ConfigPath = documentPath ?? "",
                    ConfigVersion = ModConfig.Version,
                    SaveSlotId = slotId,
                    Scope = "save",
                };
            }

            return new WebDashboardSettingsDto
            {
                ConfigPath = documentPath ?? "",
                ConfigVersion = ModConfig.Version,
                SaveSlotId = slotId,
                Scope = "save",
                Sections = BuildSections(WebDashboardConfigScope.Save, saveSlotId: slotId),
                Profile = WebDashboardQuickSettingsBridge.ToProfileDto(
                    slotId == SaveSlotConfigStore.ActiveSlotId
                        ? SaveSlotConfigStore.ActiveProfile
                        : SaveSlotConfigProfile.TryReadProfileForSlot(slotId)),
            };
        }

        internal static WebDashboardConfigUpdateResult ApplyGlobalUpdate(string sectionId, string key, string value)
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = L("config_not_initialized"),
                };
            }

            if (ModConfigRegistry.IsWebDashboardSection(sectionId))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = L("web_dashboard_readonly"),
                };
            }

            if (!WebDashboardGameState.CanEditGlobalSetting(sectionId, key))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = L("host_only"),
                };
            }

            if (!ModConfigRegistry.TryNormalizeRawValue(sectionId, key, value, out string normalized, out string? error))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = error ?? L("invalid_value"),
                };
            }

            ModConfigChangeTracker.BeginBatch();
            try
            {
                if (!ModConfigRegistry.TryApplyNormalizedEntry(sectionId, key, normalized, out string effectiveValue, out error))
                {
                    return new WebDashboardConfigUpdateResult
                    {
                        Success = false,
                        Message = error ?? L("failed_apply"),
                    };
                }

                if (!GlobalConfigStore.TryWriteValue(sectionId, key, effectiveValue, out error, waitForCompletion: false))
                {
                    return new WebDashboardConfigUpdateResult
                    {
                        Success = false,
                        Message = error ?? L("failed_save_global"),
                    };
                }

                int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
                if (activeSlotId < 0 && MimesisSaveManager.TryGetActiveSaveSlotId(out int resolvedSlotId))
                {
                    activeSlotId = resolvedSlotId;
                }

                if (activeSlotId >= 0)
                {
                    ReconcileActiveSaveOverride(activeSlotId, sectionId, key, effectiveValue, out error);
                }

                WebDashboardSettingsCache.Invalidate();
                return BuildUpdateResult(sectionId, key, effectiveValue, L("saved_global"));
            }
            finally
            {
                ModConfigChangeTracker.EndBatch();
            }
        }

        internal static WebDashboardConfigUpdateResult ApplyReset(
            WebDashboardConfigScope scope,
            int slotId,
            string sectionId,
            string? key)
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = L("config_not_initialized"),
                };
            }

            if (ModConfigRegistry.IsWebDashboardSection(sectionId))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = scope == WebDashboardConfigScope.Save
                        ? L("web_dashboard_no_save_override")
                        : L("web_dashboard_readonly"),
                };
            }

            if (!TryCollectResetKeys(scope, slotId, sectionId, key, out List<string> keys, out string? collectError))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = collectError ?? L("invalid_value"),
                    SectionId = sectionId,
                    Key = key ?? "",
                };
            }

            if (scope == WebDashboardConfigScope.Save && MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            }

            int resetCount = 0;
            string? lastError = null;

            ModConfigChangeTracker.BeginBatch();
            try
            {
                foreach (string entryKey in keys)
                {
                    if (scope == WebDashboardConfigScope.Global)
                    {
                        if (!WebDashboardGameState.CanEditGlobalSetting(sectionId, entryKey))
                        {
                            continue;
                        }

                        if (!TryResetGlobalKey(sectionId, entryKey, out lastError))
                        {
                            if (!string.IsNullOrEmpty(lastError))
                            {
                                return new WebDashboardConfigUpdateResult
                                {
                                    Success = false,
                                    Message = lastError,
                                    SectionId = sectionId,
                                    Key = entryKey,
                                };
                            }

                            continue;
                        }

                        resetCount++;
                        continue;
                    }

                    if (!TryResetSaveKey(slotId, sectionId, entryKey, out lastError))
                    {
                        if (!string.IsNullOrEmpty(lastError))
                        {
                            return new WebDashboardConfigUpdateResult
                            {
                                Success = false,
                                Message = lastError,
                                SectionId = sectionId,
                                Key = entryKey,
                            };
                        }

                        continue;
                    }

                    resetCount++;
                }
            }
            finally
            {
                ModConfigChangeTracker.EndBatch();
            }

            if (scope == WebDashboardConfigScope.Global && resetCount > 0)
            {
                WebDashboardSettingsCache.Invalidate();
            }

            return new WebDashboardConfigUpdateResult
            {
                Success = true,
                Message = scope == WebDashboardConfigScope.Save ? L("reset_global") : L("reset_defaults"),
                ConfigVersion = ModConfig.Version,
                SectionId = sectionId,
                Key = key ?? "",
                ResetCount = resetCount,
            };
        }

        internal static WebDashboardConfigUpdateResult ApplySaveUpdate(int slotId, string sectionId, string key, string value)
        {
            if (ModConfigRegistry.IsWebDashboardSection(sectionId))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = L("web_dashboard_no_save_override"),
                };
            }

            if (MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            }

            if (!SaveSlotConfigStore.TrySetOverride(slotId, sectionId, key, value, out string? error, waitForCompletion: false))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = error ?? L("invalid_value"),
                };
            }

            if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = L("saved_save"),
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
                Message = L("saved_save"),
                ConfigVersion = ModConfig.Version,
                SectionId = sectionId,
                Key = key,
                Value = effectiveValue,
                Type = entry.GetReflectedType()?.Name ?? "Unknown",
                IsOverridden = isOverridden,
            };
        }

        private static List<WebDashboardConfigSectionDto> BuildSections(
            WebDashboardConfigScope scope,
            int saveSlotId)
        {
            bool saveScope = scope == WebDashboardConfigScope.Save;
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

                title = WebDashboardL10n.GetConfigSectionTitle(sectionId) ?? title;

                WebDashboardConfigSectionDto section = new()
                {
                    Id = sectionId,
                    Title = title,
                    Description = WebDashboardL10n.GetConfigSectionDescription(sectionId) ?? "",
                };

                ModConfigRegistry.TryGetFeatureToggleKey(sectionId, out string featureToggleKey);

                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                    {
                        continue;
                    }

                    if (saveScope && !ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
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
                        Title = WebDashboardL10n.GetConfigEntryTitle(sectionId, key) ?? entry.DisplayName ?? entry.Identifier,
                        Description = WebDashboardL10n.GetConfigEntryDescription(sectionId, key) ?? entry.Description ?? "",
                        Type = entry.GetReflectedType()?.Name ?? "Unknown",
                        Value = ModConfigRegistry.FormatEntryValue(entry),
                        DefaultValue = ModConfigRegistry.FormatEntryDefaultValue(entry),
                        GlobalValue = globalValue,
                        IsOverridden = isOverridden,
                        IsHidden = entry.IsHidden,
                    };

                    if (ModConfigEntryBounds.TryGet(sectionId, key, out ModConfigEntryBound bounds))
                    {
                        entryDto.MinValue = bounds.MinValue;
                        entryDto.MaxValue = bounds.MaxValue;
                    }

                    ModConfigEntryUiHints.ApplyToEntry(entryDto, sectionId, key);
                    ModConfigEntryDependencies.ApplyToEntry(section, entryDto);
                    entryDto.HasLocalEffect = ModConfigEntryLocalEffect.HasLocalEffect(sectionId, key);

                    if (!string.IsNullOrEmpty(featureToggleKey)
                        && string.Equals(key, featureToggleKey, StringComparison.Ordinal))
                    {
                        section.FeatureToggle = entryDto;
                        continue;
                    }

                    section.Entries.Add(entryDto);
                }

                ModConfigEntryUiHints.AssignEntryGroups(section);

                if (section.FeatureToggle != null || section.Entries.Count > 0)
                {
                    sections.Add(section);
                }
            }

            return sections;
        }

        private static bool TryCollectResetKeys(
            WebDashboardConfigScope scope,
            int slotId,
            string sectionId,
            string? key,
            out List<string> keys,
            out string? error)
        {
            keys = [];
            error = null;

            if (!ModConfigRegistry.TryGetSectionTitle(sectionId, out _))
            {
                error = L("unknown_setting");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? singleEntry) || singleEntry == null)
                {
                    error = L("unknown_setting");
                    return false;
                }

                if (scope == WebDashboardConfigScope.Save && !ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
                {
                    error = L("save_override_not_allowed");
                    return false;
                }

                keys.Add(key);
                return true;
            }

            foreach (string entryKey in ModConfigRegistry.GetEntryOrder(sectionId))
            {
                if (!ModConfigRegistry.TryGetEntry(sectionId, entryKey, out MelonPreferences_Entry? entry) || entry == null)
                {
                    continue;
                }

                if (scope == WebDashboardConfigScope.Save && !ModConfigRegistry.IsSaveOverrideAllowed(sectionId, entryKey))
                {
                    continue;
                }

                if (scope == WebDashboardConfigScope.Save)
                {
                    if (!SaveSlotConfigStore.IsOverridden(slotId, sectionId, entryKey))
                    {
                        continue;
                    }
                }

                keys.Add(entryKey);
            }

            return true;
        }

        private static bool TryResetGlobalKey(string sectionId, string key, out string? error)
        {
            error = null;

            if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                error = L("unknown_setting");
                return false;
            }

            string defaultValue = ModConfigRegistry.FormatEntryDefaultValue(entry);
            string currentValue = ModConfigRegistry.FormatEntryValue(entry);
            if (ModConfigRegistry.RawValuesEqual(sectionId, key, currentValue, defaultValue))
            {
                return false;
            }

            if (!ModConfigRegistry.TryNormalizeRawValue(sectionId, key, defaultValue, out string normalized, out error))
            {
                return false;
            }

            if (!ModConfigRegistry.TryApplyNormalizedEntry(sectionId, key, normalized, out string effectiveValue, out error))
            {
                return false;
            }

            if (!GlobalConfigStore.TryWriteValue(sectionId, key, effectiveValue, out error, waitForCompletion: false))
            {
                return false;
            }

            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId < 0 && MimesisSaveManager.TryGetActiveSaveSlotId(out int resolvedSlotId))
            {
                activeSlotId = resolvedSlotId;
            }

            if (activeSlotId >= 0)
            {
                ReconcileActiveSaveOverride(activeSlotId, sectionId, key, effectiveValue, out error);
            }

            return true;
        }

        private static bool TryResetSaveKey(int slotId, string sectionId, string key, out string? error)
        {
            error = null;

            if (!SaveSlotConfigStore.IsOverridden(slotId, sectionId, key))
            {
                return false;
            }

            if (!ModConfigRegistry.TryGetGlobalRawValue(sectionId, key, out string globalRaw))
            {
                error = L("unknown_setting");
                return false;
            }

            if (!SaveSlotConfigStore.TrySetOverride(slotId, sectionId, key, globalRaw, out error, waitForCompletion: false))
            {
                return false;
            }

            return true;
        }
    }
}
