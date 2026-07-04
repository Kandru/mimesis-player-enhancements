using System;
using System.IO;
using MelonLoader;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Direct read/write of the global mod config file (<see cref="ModConfig.FilePath"/>).
    /// Web dashboard global updates use this instead of MelonLoader SaveToFile so values
    /// persist reliably while save-slot overrides may be active in memory.
    /// </summary>
    internal static class GlobalConfigStore
    {
        private const string Feature = "GlobalConfig";

        internal static SparseTomlConfig.Document Load()
        {
            if (string.IsNullOrEmpty(ModConfig.FilePath))
            {
                return new SparseTomlConfig.Document();
            }

            string? text = AtomicFileIO.ReadText(ModConfig.FilePath, Feature);
            return SparseTomlConfig.Load(text);
        }

        internal static bool TryWriteValue(
            string sectionId,
            string key,
            string normalized,
            out string? error,
            bool waitForCompletion = false)
        {
            error = null;

            if (!ModConfig.IsInitialized)
            {
                error = "Configuration is not initialized.";
                return false;
            }

            if (string.IsNullOrEmpty(ModConfig.FilePath))
            {
                error = "Global config path unavailable.";
                return false;
            }

            if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                error = "Unknown setting.";
                return false;
            }

            string defaultValue = ModConfigRegistry.FormatEntryDefaultValue(entry);
            SparseTomlConfig.Document doc = Load();

            if (ModConfigRegistry.RawValuesEqual(sectionId, key, normalized, defaultValue))
            {
                RemoveKey(doc, sectionId, key);
            }
            else
            {
                EnsureSection(doc, sectionId);
                doc.Sections[sectionId][key] = normalized;
            }

            return SaveDocument(doc, out error, waitForCompletion);
        }

        private static bool SaveDocument(
            SparseTomlConfig.Document doc,
            out string? error,
            bool waitForCompletion = false)
        {
            error = null;

            try
            {
                string? directory = Path.GetDirectoryName(ModConfig.FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                if (SparseTomlConfig.IsEmpty(doc) && File.Exists(ModConfig.FilePath))
                {
                    BackgroundFileWriteQueue.EnqueueDelete(ModConfig.FilePath, Feature, waitForCompletion);
                    return true;
                }

                if (SparseTomlConfig.IsEmpty(doc))
                {
                    return true;
                }

                BackgroundFileWriteQueue.EnqueueText(
                    ModConfig.FilePath,
                    SparseTomlConfig.Serialize(doc),
                    Feature,
                    waitForCompletion);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                ModLog.Error(Feature, $"SaveDocument: {ex.Message}");
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
