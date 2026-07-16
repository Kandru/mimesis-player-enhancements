using System.IO;
using ReluReplay;
using ReluReplay.Data;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayLibrary
    {
        private const string Feature = "Replays";
        private const string LibraryFolderName = "ModLibrary";

        internal static string LibraryDirectory =>
            Path.Combine(ReplaySharedData.GetBaseReplaySaveFilePath(), LibraryFolderName);

        internal static void TryPreserveFromRecording(ReplayData replayData)
        {
            if (!ReplaysRuntime.ShouldKeepLocalReplays())
            {
                return;
            }

            if (!HostStatusCache.IsHostFast())
            {
                return;
            }

            if (replayData == null)
            {
                return;
            }

            string playPath = replayData.PlayFilePath;
            string sndPath = replayData.SndFilePath;
            if (!File.Exists(playPath) || !File.Exists(sndPath))
            {
                ModLog.Warn(Feature, "Recording finished but replay files are missing — nothing preserved.");
                return;
            }

            try
            {
                EnsureDirectory();
                ReplayManager? manager = ReplayGameAccess.TryGetReplayManager();
                if (manager == null)
                {
                    ModLog.Warn(Feature, "ReplayManager unavailable — cannot preserve recording.");
                    return;
                }

                manager.UpdateStorageFileParams();
                string destPlayName = manager.GetReplayFileName(ReplayManager.E_REPLAY_FILE_TYPE.STORAGE_PLAY);
                string destSndName = manager.GetReplayFileName(ReplayManager.E_REPLAY_FILE_TYPE.STORAGE_SND);
                if (destPlayName == "NoReplay" || destSndName == "NoReplay")
                {
                    ModLog.Warn(Feature, "Could not build replay library filenames — recording not preserved.");
                    return;
                }

                string destPlayPath = Path.Combine(LibraryDirectory, destPlayName);
                string destSndPath = Path.Combine(LibraryDirectory, destSndName);
                File.Copy(playPath, destPlayPath, overwrite: true);
                File.Copy(sndPath, destSndPath, overwrite: true);
                PruneOldestIfNeeded();
                ModLog.Info(Feature, $"Replay preserved — {destPlayName}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to preserve replay — {ex.Message}");
            }
        }

        internal static IReadOnlyList<ReplayLibraryEntry> ListEntries()
        {
            List<ReplayLibraryEntry> entries = [];
            if (!Directory.Exists(LibraryDirectory))
            {
                return entries;
            }

            foreach (string playPath in Directory.GetFiles(LibraryDirectory, $"{ReplayData.ReplayFilePrefix}_*.replay"))
            {
                string fileName = Path.GetFileName(playPath);
                if (fileName.Contains($"_{ReplayData.ReplayVoiceFilePostfix}.", StringComparison.Ordinal))
                {
                    continue;
                }

                IReplayHeader? header = ReplayData.LoadReplayHeaderData(playPath);
                if (header == null)
                {
                    ModLog.Debug(Feature, $"Skipping unreadable replay header — {fileName}");
                    continue;
                }

                long size = new FileInfo(playPath).Length;
                entries.Add(new ReplayLibraryEntry(playPath, fileName, size, header));
            }

            entries.Sort((a, b) =>
            {
                long timeA = a.Header.GetReplayRecordStartTime();
                long timeB = b.Header.GetReplayRecordStartTime();
                int cmp = timeB.CompareTo(timeA);
                return cmp != 0 ? cmp : string.Compare(b.FileName, a.FileName, StringComparison.Ordinal);
            });
            return entries;
        }

        internal static bool TryDelete(ReplayLibraryEntry entry)
        {
            try
            {
                if (File.Exists(entry.PlayFilePath))
                {
                    File.Delete(entry.PlayFilePath);
                }

                if (File.Exists(entry.SndFilePath))
                {
                    File.Delete(entry.SndFilePath);
                }

                ModLog.Info(Feature, $"Deleted replay — {entry.FileName}");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to delete replay — {ex.Message}");
                return false;
            }
        }

        internal static string GetSndPathForPlayFile(string playFilePath)
        {
            string directory = Path.GetDirectoryName(playFilePath) ?? LibraryDirectory;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(playFilePath);
            string sndName = $"{fileNameWithoutExtension}_{ReplayData.ReplayVoiceFilePostfix}.{ReplayData.ReplayFileExt}";
            return Path.Combine(directory, sndName);
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(LibraryDirectory))
            {
                Directory.CreateDirectory(LibraryDirectory);
            }
        }

        private static void PruneOldestIfNeeded()
        {
            int max = ModConfig.MaxStoredReplays.Value;
            if (max <= 0)
            {
                return;
            }

            IReadOnlyList<ReplayLibraryEntry> entries = ListEntries();
            for (int i = entries.Count - 1; i >= max; i--)
            {
                TryDelete(entries[i]);
            }
        }
    }
}
