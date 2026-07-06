using System;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Sparse per-save-slot config overrides stored as a Steam-cloud sidecar beside vanilla saves.
    /// Keys matching global values are omitted automatically.
    /// </summary>
    internal static class SaveSlotConfigStore
    {
        private const string Feature = "SaveSlotConfig";
        private static int _activeSlotId = -1;
        private static bool _isApplyingOverrides;
        private static bool _dirty;
        private static SparseTomlConfig.Document _runtimeDoc = new();
        private static SaveConfigProfileState _runtimeProfile = new();

        internal static int ActiveSlotId => _activeSlotId;

        internal static bool IsApplyingOverrides => _isApplyingOverrides;

        internal static SaveConfigProfileState ActiveProfile => _runtimeProfile;

        internal static string? GetOverrideFilePath(int slotId)
        {
            return SaveSidecarPaths.GetOverridesPath(slotId);
        }

        internal static void LoadForSlot(int slotId)
        {
            if (!ModConfig.IsInitialized || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            ModConfigChangeTracker.BeginBatch();
            _isApplyingOverrides = true;
            try
            {
                ModConfig.ReloadGlobalFromFile();
                _activeSlotId = slotId;
                _runtimeDoc = ReadOverridesFromDisk(slotId);
                _runtimeProfile = SaveSlotConfigProfile.Parse(_runtimeDoc);
                _dirty = false;
                ApplyActiveProfile(slotId);
                ModConfig.SanitizeFloatEntries();
            }
            finally
            {
                _isApplyingOverrides = false;
                ModConfigChangeTracker.EndBatch();
            }
        }

        internal static bool TrySetProfileMode(
            int slotId,
            SaveConfigProfileMode mode,
            string? presetId,
            out string? error,
            bool waitForCompletion = false)
        {
            error = null;
            if (!EnsureActiveSlot(slotId, out error))
            {
                return false;
            }

            ModConfigChangeTracker.BeginBatch();
            try
            {
                if (mode == SaveConfigProfileMode.Quick)
                {
                    if (string.IsNullOrWhiteSpace(presetId))
                    {
                        error = ModL10n.Get("api.quick_preset_not_found");
                        return false;
                    }

                    if (!QuickSettingsCatalog.TryResolvePreset(presetId, out QuickSettingPreset preset))
                    {
                        error = ModL10n.Get("api.quick_preset_not_found");
                        return false;
                    }

                    ClearGameplayOverrideSections(_runtimeDoc);
                    _runtimeProfile = new SaveConfigProfileState
                    {
                        Mode = SaveConfigProfileMode.Quick,
                        PresetId = preset.Id,
                        PresetRevision = preset.Revision,
                    };
                    SaveSlotConfigProfile.WriteProfileSection(_runtimeDoc, _runtimeProfile);
                    QuickSettingsResolver.ApplyPresetValues(preset.Values);
                }
                else if (mode == SaveConfigProfileMode.Global)
                {
                    ClearGameplayOverrideSections(_runtimeDoc);
                    _runtimeProfile = new SaveConfigProfileState { Mode = SaveConfigProfileMode.Global };
                    SaveSlotConfigProfile.RemoveProfileSection(_runtimeDoc);
                    ModConfig.ReloadGlobalFromFile();
                }
                else
                {
                    if (_runtimeProfile.Mode == SaveConfigProfileMode.Quick)
                    {
                        MaterializeEffectiveConfigToDoc();
                    }

                    _runtimeProfile = new SaveConfigProfileState { Mode = SaveConfigProfileMode.Custom };
                    SaveSlotConfigProfile.WriteProfileSection(_runtimeDoc, _runtimeProfile);
                }

                _dirty = true;
                ModConfig.SanitizeFloatEntries();
            }
            finally
            {
                ModConfigChangeTracker.EndBatch();
            }

            if (waitForCompletion)
            {
                FlushToDisk(slotId, waitForCompletion: true);
            }

            return true;
        }

        internal static bool TryApplyQuickPreset(
            int slotId,
            string presetId,
            out string? error,
            bool waitForCompletion = false)
        {
            return TrySetProfileMode(slotId, SaveConfigProfileMode.Quick, presetId, out error, waitForCompletion);
        }

        internal static void MaterializeEffectiveConfigToDoc()
        {
            ClearGameplayOverrideSections(_runtimeDoc);
            Dictionary<string, Dictionary<string, string>> effective = QuickSettingsValuesBuilder.CollectValuesDifferingFromGlobal();
            MergeValuesIntoDoc(_runtimeDoc, effective);
        }

        internal static bool TrySetOverride(
            int slotId,
            string sectionId,
            string key,
            string rawValue,
            out string? error,
            bool waitForCompletion = false)
        {
            error = null;

            if (!ModConfig.IsInitialized)
            {
                error = ModL10n.Get("api.config_not_initialized");
                return false;
            }

            if (!EnsureActiveSlot(slotId, out error))
            {
                return false;
            }

            if (!ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
            {
                error = ModL10n.Get("api.save_override_not_allowed");
                return false;
            }

            if (!ModConfigRegistry.TryNormalizeRawValue(sectionId, key, rawValue, out string normalized, out error))
            {
                return false;
            }

            if (!ModConfigRegistry.TryGetGlobalRawValue(sectionId, key, out string globalRaw))
            {
                error = ModL10n.Get("api.unknown_setting");
                return false;
            }

            if (GetOverrideFilePath(slotId) == null)
            {
                error = ModL10n.Get("api.save_slot_path_unavailable");
                return false;
            }

            if (_runtimeProfile.Mode == SaveConfigProfileMode.Quick)
            {
                MaterializeEffectiveConfigToDoc();
                _runtimeProfile = new SaveConfigProfileState { Mode = SaveConfigProfileMode.Custom };
                SaveSlotConfigProfile.WriteProfileSection(_runtimeDoc, _runtimeProfile);
            }

            ModConfigChangeTracker.BeginBatch();
            string effectiveValue;
            try
            {
                if (!ModConfigRegistry.TryApplyNormalizedEntry(sectionId, key, normalized, out effectiveValue, out error))
                {
                    return false;
                }
            }
            finally
            {
                ModConfigChangeTracker.EndBatch();
            }

            if (ModConfigRegistry.RawValuesEqual(sectionId, key, effectiveValue, globalRaw))
            {
                RemoveKey(_runtimeDoc, sectionId, key);
            }
            else
            {
                EnsureSection(_runtimeDoc, sectionId);
                _runtimeDoc.Sections[sectionId][key] = effectiveValue;
            }

            if (_runtimeProfile.Mode != SaveConfigProfileMode.Custom)
            {
                _runtimeProfile = new SaveConfigProfileState { Mode = SaveConfigProfileMode.Custom };
                SaveSlotConfigProfile.WriteProfileSection(_runtimeDoc, _runtimeProfile);
            }

            _dirty = true;

            if (waitForCompletion)
            {
                FlushToDisk(slotId, waitForCompletion: true);
            }

            return true;
        }

        internal static void ClearOverrideKey(int slotId, string sectionId, string key)
        {
            if (slotId != _activeSlotId)
            {
                return;
            }

            if (!_runtimeDoc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys) || !keys.ContainsKey(key))
            {
                return;
            }

            RemoveKey(_runtimeDoc, sectionId, key);
            _dirty = true;
        }

        internal static void FlushToDisk(int slotId, bool waitForCompletion = false)
        {
            if (slotId != _activeSlotId || !_dirty)
            {
                return;
            }

            string? filePath = GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (SaveDocument(slotId, filePath, _runtimeDoc, waitForCompletion))
            {
                _dirty = false;
            }
        }

        internal static void ClearRuntimeToGlobal()
        {
            _activeSlotId = -1;
            _runtimeDoc = new SparseTomlConfig.Document();
            _runtimeProfile = new SaveConfigProfileState();
            _dirty = false;
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            ModConfigChangeTracker.BeginBatch();
            try
            {
                ModConfig.ReloadGlobalFromFile();
            }
            finally
            {
                ModConfigChangeTracker.CancelBatch();
            }

            ModConfigChangeTracker.NotifyFullReload();
        }

        internal static bool IsOverridden(int slotId, string sectionId, string key)
        {
            if (slotId != _activeSlotId)
            {
                return false;
            }

            if (_runtimeProfile.Mode == SaveConfigProfileMode.Quick)
            {
                return false;
            }

            if (_runtimeProfile.Mode == SaveConfigProfileMode.Global)
            {
                return false;
            }

            return _runtimeDoc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys)
                && keys.ContainsKey(key);
        }

        internal static bool TryGetOverrideRaw(int slotId, string sectionId, string key, out string rawValue)
        {
            rawValue = "";
            if (slotId != _activeSlotId)
            {
                return false;
            }

            if (_runtimeProfile.Mode == SaveConfigProfileMode.Quick)
            {
                if (!QuickSettingsCatalog.TryResolvePreset(_runtimeProfile.PresetId, out QuickSettingPreset preset))
                {
                    return false;
                }

                return preset.TryGetValue(sectionId, key, out rawValue);
            }

            return _runtimeDoc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys)
                && keys.TryGetValue(key, out rawValue!);
        }

        private static void ApplyActiveProfile(int slotId)
        {
            switch (_runtimeProfile.Mode)
            {
                case SaveConfigProfileMode.Quick:
                    if (!QuickSettingsCatalog.TryResolvePreset(_runtimeProfile.PresetId, out QuickSettingPreset preset))
                    {
                        ModLog.Warn(Feature, $"Quick preset not found for slot {slotId} — id={_runtimeProfile.PresetId}; falling back to global.");
                        _runtimeProfile = new SaveConfigProfileState { Mode = SaveConfigProfileMode.Global };
                        return;
                    }

                    if (_runtimeProfile.PresetRevision != preset.Revision)
                    {
                        ModLog.Info(Feature, $"Re-applying updated quick preset — slot={slotId}, id={preset.Id}, revision={preset.Revision}");
                    }

                    QuickSettingsResolver.ApplyPresetValues(preset.Values);
                    _runtimeProfile.PresetRevision = preset.Revision;
                    break;

                case SaveConfigProfileMode.Custom:
                    ApplyGameplayDoc(_runtimeDoc, slotId);
                    break;

                default:
                    break;
            }
        }

        private static SparseTomlConfig.Document ReadOverridesFromDisk(int slotId)
        {
            string? filePath = GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return new SparseTomlConfig.Document();
            }

            string? text = AtomicFileIO.ReadText(filePath, Feature);
            return SparseTomlConfig.Load(text);
        }

        private static void ApplyGameplayDoc(SparseTomlConfig.Document doc, int slotId)
        {
            foreach (string sectionId in doc.SectionOrder)
            {
                if (SaveSlotConfigProfile.IsProfileSection(sectionId))
                {
                    continue;
                }

                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> pair in keys)
                {
                    if (!ModConfigRegistry.IsSaveOverrideAllowed(sectionId, pair.Key))
                    {
                        continue;
                    }

                    if (ModConfigRegistry.TrySetEntryValue(sectionId, pair.Key, pair.Value, out _))
                    {
                        continue;
                    }

                    ModLog.Warn(Feature, $"Skipped invalid override {sectionId}/{pair.Key} for slot {slotId}.");
                }
            }
        }

        private static bool SaveDocument(
            int slotId,
            string filePath,
            SparseTomlConfig.Document doc,
            bool waitForCompletion = false)
        {
            try
            {
                if (SparseTomlConfig.IsEmpty(doc))
                {
                    BackgroundFileWriteQueue.EnqueueDelete(filePath, Feature, waitForCompletion);
                    return true;
                }

                BackgroundFileWriteQueue.EnqueueText(
                    filePath,
                    SparseTomlConfig.Serialize(doc),
                    Feature,
                    waitForCompletion);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"SaveDocument slot {slotId}: {ex.Message}");
                return false;
            }
        }

        private static bool EnsureActiveSlot(int slotId, out string? error)
        {
            error = null;
            if (slotId != _activeSlotId)
            {
                error = ModL10n.Get("api.save_slot_not_loaded");
                return false;
            }

            return true;
        }

        private static void ClearGameplayOverrideSections(SparseTomlConfig.Document doc)
        {
            List<string> toRemove = [];
            foreach (string sectionId in doc.SectionOrder)
            {
                if (!SaveSlotConfigProfile.IsProfileSection(sectionId))
                {
                    toRemove.Add(sectionId);
                }
            }

            foreach (string sectionId in toRemove)
            {
                _ = doc.Sections.Remove(sectionId);
                doc.SectionOrder.Remove(sectionId);
            }
        }

        private static void MergeValuesIntoDoc(
            SparseTomlConfig.Document doc,
            Dictionary<string, Dictionary<string, string>> values)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> section in values)
            {
                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    EnsureSection(doc, section.Key);
                    doc.Sections[section.Key][pair.Key] = pair.Value;
                }
            }
        }

        private static void EnsureSection(SparseTomlConfig.Document doc, string sectionId)
        {
            if (doc.Sections.ContainsKey(sectionId))
            {
                return;
            }

            doc.SectionOrder.Add(sectionId);
            doc.Sections[sectionId] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static void RemoveKey(SparseTomlConfig.Document doc, string sectionId, string key)
        {
            if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
            {
                return;
            }

            _ = keys.Remove(key);
            if (keys.Count == 0)
            {
                _ = doc.Sections.Remove(sectionId);
                doc.SectionOrder.Remove(sectionId);
            }
        }
    }
}
