using System.IO;
using ReluProtocol;

namespace MimesisPlayerEnhancement
{
    internal enum SidecarKind
    {
        Speech,
        SpeechMetadata,
        SpeechMapping,
        Overrides,
        Statistics,
        PlayerNames,
        Lobby,
    }

    /// <summary>
    /// Paths for per-save-slot sidecar files beside vanilla saves: MMGameData{N}.mpe-{kind}.sav
    /// Used by persistence, statistics, and per-save config overrides. Matches Steam Auto-Cloud MMGameData*.sav sync pattern.
    /// </summary>
    internal static class SaveSidecarPaths
    {
        private const string SidecarExtension = ".sav";
        private const string SidecarInfix = ".mpe-";

        internal static string? GetSaveFolderPath()
        {
            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return null;
            }

            string baseFolder = platformMgr.GetSaveFileFolderPath();
            return string.IsNullOrEmpty(baseFolder) ? null : baseFolder;
        }

        internal static string? GetSaveFileStem(int slotId)
        {
            if (slotId < 0)
            {
                return null;
            }

            string fileName = MMSaveGameData.GetSaveFileName(slotId);
            return string.IsNullOrEmpty(fileName) ? null : Path.GetFileNameWithoutExtension(fileName);
        }

        internal static string? GetSidecarPath(int slotId, string kind)
        {
            string? saveFolder = GetSaveFolderPath();
            string? stem = GetSaveFileStem(slotId);
            if (string.IsNullOrEmpty(saveFolder) || string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(kind))
            {
                return null;
            }

            return Path.Combine(saveFolder, stem + SidecarInfix + kind + SidecarExtension);
        }

        internal static string? GetSpeechPath(int slotId) => GetSidecarPath(slotId, "speech");

        internal static string? GetSpeechMetadataPath(int slotId) => GetSidecarPath(slotId, "speech-meta");

        internal static string? GetSpeechMappingPath(int slotId) => GetSidecarPath(slotId, "speech-mapping");

        internal static string? GetOverridesPath(int slotId) => GetSidecarPath(slotId, "overrides");

        /// <summary>
        /// Account-level user quick presets (not tied to a save slot). Uses MMGameData*.sav for Steam Auto-Cloud.
        /// </summary>
        internal static string? GetUserQuickPresetsPath()
        {
            string? saveFolder = GetSaveFolderPath();
            if (string.IsNullOrEmpty(saveFolder))
            {
                return null;
            }

            return Path.Combine(saveFolder, "MMGameData.mpe-quick-presets.sav");
        }

        internal static string? GetStatisticsPath(int slotId) => GetSidecarPath(slotId, "stats");

        internal static string? GetPlayerNamesPath(int slotId) => GetSidecarPath(slotId, "names");

        internal static string? GetLobbyPath(int slotId) => GetSidecarPath(slotId, "lobby");

        internal static IEnumerable<string> EnumerateSidecarFiles(int slotId, SidecarKind? filter = null)
        {
            string? saveFolder = GetSaveFolderPath();
            string? stem = GetSaveFileStem(slotId);
            if (string.IsNullOrEmpty(saveFolder) || string.IsNullOrEmpty(stem) || !Directory.Exists(saveFolder))
            {
                yield break;
            }

            string pattern = stem + SidecarInfix + "*";
            foreach (string file in Directory.GetFiles(saveFolder, pattern))
            {
                if (filter == null || MatchesFilter(file, stem, filter.Value))
                {
                    yield return file;
                }
            }
        }

        internal static void DeleteSidecarFile(string filePath, string logFeature = "Persistence")
        {
            AtomicFileIO.Delete(filePath, logFeature);
        }

        internal static void DeleteSidecars(int slotId, params SidecarKind[] kinds)
        {
            foreach (SidecarKind kind in kinds)
            {
                foreach (string file in EnumerateSidecarFiles(slotId, kind))
                {
                    DeleteSidecarFile(file);
                }
            }
        }

        private static bool MatchesFilter(string filePath, string stem, SidecarKind filter)
        {
            string fileName = Path.GetFileName(filePath);
            string prefix = stem + SidecarInfix;
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return filter switch
            {
                SidecarKind.Speech => fileName.Equals(prefix + "speech" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                SidecarKind.SpeechMetadata => fileName.Equals(prefix + "speech-meta" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                SidecarKind.SpeechMapping => fileName.Equals(prefix + "speech-mapping" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                SidecarKind.Overrides => fileName.Equals(prefix + "overrides" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                SidecarKind.Statistics => fileName.Equals(prefix + "stats" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                SidecarKind.PlayerNames => fileName.Equals(prefix + "names" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                SidecarKind.Lobby => fileName.Equals(prefix + "lobby" + SidecarExtension, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }
    }
}
