using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Tracks per-player voice event counts and baselines so only deltas since the
    /// last flush are credited to statistics.
    /// </summary>
    internal static class StatisticsVoiceCounter
    {
        private static readonly Dictionary<ulong, int> _baselines = [];
        private static Dictionary<ulong, int>? _countCache;

        internal static void Clear()
        {
            _baselines.Clear();
            _countCache = null;
        }

        internal static void RemoveBaseline(ulong steamId)
        {
            _ = _baselines.Remove(steamId);
        }

        internal static void EnsureBaseline(ulong steamId)
        {
            if (_baselines.ContainsKey(steamId))
            {
                return;
            }

            _baselines[steamId] = CountForSteamId(steamId);
        }

        internal static void SetBaselineToCurrent(ulong steamId)
        {
            _baselines[steamId] = CountForSteamId(steamId);
        }

        internal static int GetDeltaSinceBaseline(ulong steamId, Dictionary<ulong, int> voiceCounts)
        {
            int current = voiceCounts.TryGetValue(steamId, out int count) ? count : 0;
            int baseline = _baselines.TryGetValue(steamId, out int b) ? b : current;
            return System.Math.Max(0, current - baseline);
        }

        internal static void UpdateBaselines(IEnumerable<ulong> steamIds, Dictionary<ulong, int> voiceCounts)
        {
            foreach (ulong steamId in steamIds)
            {
                _baselines[steamId] = voiceCounts.TryGetValue(steamId, out int count) ? count : 0;
            }
        }

        internal static Dictionary<ulong, int> GetVoiceCountCache()
        {
            _countCache ??= BuildVoiceCountCache();
            return _countCache;
        }

        internal static void InvalidateVoiceCountCache()
        {
            _countCache = null;
        }

        private static int CountForSteamId(ulong steamId)
        {
            Dictionary<ulong, int> cache = GetVoiceCountCache();
            return cache.TryGetValue(steamId, out int count) ? count : 0;
        }

        private static Dictionary<ulong, int> BuildVoiceCountCache()
        {
            Dictionary<ulong, int> cache = [];
            try
            {
                IEnumerable<SpeechEventArchive> archives = SpeechEventArchiveRegistry.EnumerateActive();
                foreach (SpeechEventArchive archive in archives)
                {
                    if (archive == null)
                    {
                        continue;
                    }

                    long playerUid = 0;
                    bool isLocal = false;
                    try
                    {
                        playerUid = archive.PlayerUID;
                        isLocal = archive.IsLocal;
                    }
                    catch
                    {
                        /* not ready */
                    }

                    ulong archiveSteam = GameSessionAccess.ResolveSteamId(playerUid, isLocal);
                    if (archiveSteam == 0)
                    {
                        continue;
                    }

                    _ = cache.TryGetValue(archiveSteam, out int current);
                    cache[archiveSteam] = current + VoiceEventStats.GetVoiceLineCount(archive);
                }
            }
            catch
            {
                /* ignore */
            }

            return cache;
        }
    }
}
