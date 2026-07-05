using System;

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

        internal static int ActiveSlotId => _activeSlotId;

        internal static bool IsApplyingOverrides => _isApplyingOverrides;

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
                _dirty = false;
                ApplyRuntimeDoc(_runtimeDoc, slotId);
                ModConfig.SanitizeFloatEntries();
            }
            finally
            {
                _isApplyingOverrides = false;
                ModConfigChangeTracker.EndBatch();
            }
        }

        internal static SparseTomlConfig.Document LoadOverrides(int slotId)
        {
            if (slotId == _activeSlotId)
            {
                return CloneDocument(_runtimeDoc);
            }

            return ReadOverridesFromDisk(slotId);
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
                error = "Configuration is not initialized.";
                return false;
            }

            if (slotId != _activeSlotId)
            {
                error = "Save slot not loaded.";
                return false;
            }

            if (!ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key))
            {
                error = "This setting cannot be overridden per save slot.";
                return false;
            }

            if (!ModConfigRegistry.TryNormalizeRawValue(sectionId, key, rawValue, out string normalized, out error))
            {
                return false;
            }

            if (!ModConfigRegistry.TryGetGlobalRawValue(sectionId, key, out string globalRaw))
            {
                error = "Unknown setting.";
                return false;
            }

            if (GetOverrideFilePath(slotId) == null)
            {
                error = "Save slot path unavailable.";
                return false;
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

        internal static void PruneMatchingGlobal(int slotId, bool waitForCompletion = false)
        {
            if (slotId != _activeSlotId)
            {
                return;
            }

            bool changed = false;
            List<string> sectionIds = [.. _runtimeDoc.SectionOrder];
            foreach (string sectionId in sectionIds)
            {
                if (!_runtimeDoc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
                {
                    continue;
                }

                List<string> toRemove = [];
                foreach (KeyValuePair<string, string> pair in keys)
                {
                    if (!ModConfigRegistry.TryGetGlobalRawValue(sectionId, pair.Key, out string globalRaw))
                    {
                        toRemove.Add(pair.Key);
                        continue;
                    }

                    if (ModConfigRegistry.RawValuesEqual(sectionId, pair.Key, pair.Value, globalRaw))
                    {
                        toRemove.Add(pair.Key);
                    }
                }

                foreach (string key in toRemove)
                {
                    _ = keys.Remove(key);
                    changed = true;
                }

                if (keys.Count == 0)
                {
                    _ = _runtimeDoc.Sections.Remove(sectionId);
                    _runtimeDoc.SectionOrder.Remove(sectionId);
                }
            }

            if (!changed)
            {
                return;
            }

            _dirty = true;
            if (waitForCompletion)
            {
                FlushToDisk(slotId, waitForCompletion: true);
            }
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

            return _runtimeDoc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys)
                && keys.TryGetValue(key, out rawValue!);
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

        private static void ApplyRuntimeDoc(SparseTomlConfig.Document doc, int slotId)
        {
            foreach (string sectionId in doc.SectionOrder)
            {
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

        private static SparseTomlConfig.Document CloneDocument(SparseTomlConfig.Document source)
        {
            SparseTomlConfig.Document clone = new();
            foreach (string sectionId in source.SectionOrder)
            {
                if (!source.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
                {
                    continue;
                }

                clone.SectionOrder.Add(sectionId);
                clone.Sections[sectionId] = new Dictionary<string, string>(keys, StringComparer.OrdinalIgnoreCase);
            }

            return clone;
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
