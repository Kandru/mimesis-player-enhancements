using System.IO;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Retires a statistics file whose schema no longer matches. Keeps one archive copy per legacy
    /// version and removes the atomic siblings so the next read does not recover the stale document.
    /// </summary>
    internal static class StatisticsLegacyFileCleanup
    {
        internal static void Retire(string path, int version, string logFeature)
        {
            try
            {
                string archive = $"{path}.legacy-v{version}.bak";
                if (File.Exists(path))
                {
                    if (File.Exists(archive))
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        File.Move(path, archive);
                    }
                }
                else if (!File.Exists(archive) && File.Exists(path + AtomicFileIO.BackupSuffix))
                {
                    File.Move(path + AtomicFileIO.BackupSuffix, archive);
                }

                AtomicFileIO.DeleteVolatileSiblings(path, logFeature);
            }
            catch (Exception ex)
            {
                ModLog.Warn(logFeature, $"Failed to retire legacy statistics file — {ex.Message}");
            }
        }
    }
}
