using System.IO;
using MelonLoader;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Direct read/write of the global mod config file (<see cref="ModConfig.FilePath"/>).
    /// Web dashboard global updates use this instead of MelonLoader SaveToFile so values
    /// persist reliably while save-slot overrides may be active in memory.
    /// Runtime changes are kept in a pending in-memory document and flushed on game quit.
    /// </summary>
    internal static class GlobalConfigStore
    {
        private const string Feature = "GlobalConfig";

        private static SparseTomlConfig.Document? _pendingDoc;
        private static bool _pendingDocLoaded;
        private static bool _dirty;

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
            _ = waitForCompletion;
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
            EnsurePendingDoc();
            SparseTomlConfig.Document doc = _pendingDoc!;

            if (ModConfigRegistry.RawValuesEqual(sectionId, key, normalized, defaultValue))
            {
                RemoveKey(doc, sectionId, key);
            }
            else
            {
                EnsureSection(doc, sectionId);
                doc.Sections[sectionId][key] = normalized;
            }

            _dirty = true;
            return true;
        }

        /// <summary>
        /// Rebuild the pending sparse document from all current in-memory preference values.
        /// </summary>
        internal static void RebuildPendingFromMemory()
        {
            if (!ModConfig.IsInitialized || SaveSlotConfigStore.IsApplyingOverrides)
            {
                return;
            }

            SparseTomlConfig.Document doc = new();
            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                    {
                        continue;
                    }

                    string current = ModConfigRegistry.FormatEntryValue(entry);
                    string defaultValue = ModConfigRegistry.FormatEntryDefaultValue(entry);
                    if (ModConfigRegistry.RawValuesEqual(sectionId, key, current, defaultValue))
                    {
                        continue;
                    }

                    EnsureSection(doc, sectionId);
                    doc.Sections[sectionId][key] = current;
                }
            }

            _pendingDoc = doc;
            _pendingDocLoaded = true;
            _dirty = true;
        }

        internal static void FlushToDisk(bool waitForCompletion = true)
        {
            _ = waitForCompletion;

            if (!_dirty || !ModConfig.IsInitialized || string.IsNullOrEmpty(ModConfig.FilePath))
            {
                return;
            }

            if (!_pendingDocLoaded || _pendingDoc == null)
            {
                _dirty = false;
                return;
            }

            try
            {
                string? directory = Path.GetDirectoryName(ModConfig.FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                if (SparseTomlConfig.IsEmpty(_pendingDoc) && File.Exists(ModConfig.FilePath))
                {
                    AtomicFileIO.Delete(ModConfig.FilePath, Feature);
                }
                else if (!SparseTomlConfig.IsEmpty(_pendingDoc))
                {
                    AtomicFileIO.WriteText(
                        ModConfig.FilePath,
                        SparseTomlConfig.Serialize(_pendingDoc),
                        Feature);
                }

                _dirty = false;
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"FlushToDisk: {ex.Message}");
            }
        }

        private static void EnsurePendingDoc()
        {
            if (_pendingDocLoaded)
            {
                return;
            }

            _pendingDoc = Load();
            _pendingDocLoaded = true;
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
