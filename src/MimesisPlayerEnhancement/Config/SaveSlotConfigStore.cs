using System;
using System.Collections.Generic;
using System.IO;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Sparse per-save-slot config overrides stored beside other Mimesis slot data.
    /// Keys matching global values are omitted automatically.
    /// </summary>
    internal static class SaveSlotConfigStore
    {
        private const string Feature = "SaveSlotConfig";
        private const string OverrideFileName = "MimesisPlayerEnhancement.overrides.cfg";

        private static int _activeSlotId = -1;
        private static bool _isApplyingOverrides;

        internal static int ActiveSlotId => _activeSlotId;

        internal static bool IsApplyingOverrides => _isApplyingOverrides;

        internal static string? GetOverrideFilePath(int slotId)
        {
            string? slotPath = MimesisSaveManager.GetMimesisSlotPath(slotId);
            return string.IsNullOrEmpty(slotPath) ? null : Path.Combine(slotPath, OverrideFileName);
        }

        internal static SparseTomlConfig.Document LoadOverrides(int slotId)
        {
            string? filePath = GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return new SparseTomlConfig.Document();
            }

            string? text = AtomicFileIO.ReadText(filePath, Feature);
            return SparseTomlConfig.Load(text);
        }

        internal static bool TrySetOverride(int slotId, string sectionId, string key, string rawValue, out string? error)
        {
            error = null;

            if (!ModConfig.IsInitialized)
            {
                error = "Configuration is not initialized.";
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

            string? filePath = GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                error = "Save slot path unavailable.";
                return false;
            }

            SparseTomlConfig.Document doc = LoadOverrides(slotId);
            EnsureSection(doc, sectionId);

            if (ModConfigRegistry.RawValuesEqual(sectionId, key, normalized, globalRaw))
            {
                RemoveKey(doc, sectionId, key);
            }
            else
            {
                doc.Sections[sectionId][key] = normalized;
            }

            if (!SaveDocument(slotId, filePath, doc))
            {
                error = "Failed to save override file.";
                return false;
            }

            if (!ModConfigRegistry.TrySetEntryValue(sectionId, key, normalized, out error))
            {
                return false;
            }

            ModConfig.SanitizeFloatEntries();
            ModConfig.NotifyRuntimeChanged();
            return true;
        }

        internal static void ClearOverrideKey(int slotId, string sectionId, string key)
        {
            string? filePath = GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            SparseTomlConfig.Document doc = LoadOverrides(slotId);
            if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys) || !keys.ContainsKey(key))
            {
                return;
            }

            RemoveKey(doc, sectionId, key);
            _ = SaveDocument(slotId, filePath, doc);
        }

        internal static void PruneMatchingGlobal(int slotId)
        {
            string? filePath = GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            SparseTomlConfig.Document doc = LoadOverrides(slotId);
            bool changed = false;

            List<string> sectionIds = [.. doc.SectionOrder];
            foreach (string sectionId in sectionIds)
            {
                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
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
                    _ = doc.Sections.Remove(sectionId);
                    doc.SectionOrder.Remove(sectionId);
                }
            }

            if (!changed)
            {
                return;
            }

            _ = SaveDocument(slotId, filePath, doc);
            if (_activeSlotId == slotId)
            {
                ApplyOverridesToRuntime(slotId);
            }
        }

        internal static void ApplyOverridesToRuntime(int slotId)
        {
            if (!ModConfig.IsInitialized || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            _isApplyingOverrides = true;
            try
            {
                ModConfig.ReloadGlobalFromFile();
                _activeSlotId = slotId;

                SparseTomlConfig.Document doc = LoadOverrides(slotId);
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

                ModConfig.SanitizeFloatEntries();
                ModConfig.NotifyRuntimeChanged();
            }
            finally
            {
                _isApplyingOverrides = false;
            }
        }

        internal static void ClearRuntimeToGlobal()
        {
            _activeSlotId = -1;
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            ModConfig.ReloadGlobalFromFile();
            ModConfig.NotifyRuntimeChanged();
        }

        internal static bool IsOverridden(int slotId, string sectionId, string key)
        {
            SparseTomlConfig.Document doc = LoadOverrides(slotId);
            return doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys)
                && keys.ContainsKey(key);
        }

        private static bool SaveDocument(int slotId, string filePath, SparseTomlConfig.Document doc)
        {
            try
            {
                string? slotPath = MimesisSaveManager.GetMimesisSlotPath(slotId);
                if (!string.IsNullOrEmpty(slotPath))
                {
                    _ = Directory.CreateDirectory(slotPath);
                }

                if (SparseTomlConfig.IsEmpty(doc))
                {
                    BackgroundFileWriteQueue.EnqueueDelete(filePath, Feature);
                    return true;
                }

                BackgroundFileWriteQueue.EnqueueText(filePath, SparseTomlConfig.Serialize(doc), Feature);
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
