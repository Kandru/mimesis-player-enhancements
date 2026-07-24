using System.Threading;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Util.Players
{
    internal sealed class PlayerRecord
    {
        internal ulong SteamId;
        internal string DisplayName = "";
        internal string VoiceId = "";
        internal bool IsOnline;
        internal DateTime? ConnectedSinceUtc;
        internal PlayerStatisticsDocument Statistics = new();
    }

    internal static class PlayerRegistry
    {
        private const string Feature = "Players";

        private static readonly Dictionary<ulong, PlayerRecord> Records = [];

        private static int _loadedSlotId = -999;
        private static int _revision;

        internal static int LoadedSlotId => _loadedSlotId;

        internal static int Revision => Volatile.Read(ref _revision);

        internal static void BumpRevision()
        {
            _ = Interlocked.Increment(ref _revision);
        }

        internal static void LoadForSlot(int slotId, bool forceReload = false)
        {
            if (!MimesisSaveManager.IsHost() || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (!forceReload && slotId == _loadedSlotId)
            {
                StatisticsWriteQueue.Configure(slotId, GetStatisticsDictionary);
                return;
            }

            try
            {
                _loadedSlotId = slotId;
                Records.Clear();

                Dictionary<ulong, PlayerStatisticsDocument> loadedStats = [];
                StatisticsStore.LoadAllPlayersForSlot(slotId, loadedStats);
                foreach (KeyValuePair<ulong, PlayerStatisticsDocument> kvp in loadedStats)
                {
                    if (kvp.Key == 0 || kvp.Value == null)
                    {
                        continue;
                    }

                    PlayerRecord record = CreateRecord(kvp.Key);
                    record.Statistics = kvp.Value;
                    record.DisplayName = kvp.Value.DisplayName ?? "";
                    Records[kvp.Key] = record;
                }

                MergeRosterFromDocument();
                StatisticsWriteQueue.Configure(slotId, GetStatisticsDictionary);
                BumpRevision();
                ModLog.Info(Feature, $"Loaded player registry for save slot {slotId} ({Records.Count} players).");
            }
            catch (Exception ex)
            {
                _loadedSlotId = -999;
                Records.Clear();
                ModLog.Warn(Feature, $"LoadForSlot({slotId}) failed — {ex.Message}");
            }
        }

        internal static void ResetSessionRuntimeState()
        {
            foreach (PlayerRecord record in Records.Values)
            {
                record.VoiceId = "";
                record.IsOnline = false;
                record.ConnectedSinceUtc = null;
            }

            _loadedSlotId = -999;
            BumpRevision();
        }

        internal static PlayerRecord GetOrCreate(ulong steamId)
        {
            if (Records.TryGetValue(steamId, out PlayerRecord? existing))
            {
                return existing;
            }

            PlayerRecord record = CreateRecord(steamId);
            Records[steamId] = record;
            return record;
        }

        internal static bool TryGetRecord(ulong steamId, out PlayerRecord record)
        {
            return Records.TryGetValue(steamId, out record!);
        }

        internal static bool TryGetStatistics(ulong steamId, out PlayerStatisticsDocument document)
        {
            if (Records.TryGetValue(steamId, out PlayerRecord? record))
            {
                document = record.Statistics;
                return true;
            }

            document = null!;
            return false;
        }

        internal static IReadOnlyCollection<ulong> GetConnectedSteamIds()
        {
            List<ulong> connected = [];
            foreach (PlayerRecord record in Records.Values)
            {
                if (record.IsOnline)
                {
                    connected.Add(record.SteamId);
                }
            }

            return connected;
        }

        internal static bool HasAnyConnected()
        {
            foreach (PlayerRecord record in Records.Values)
            {
                if (record.IsOnline)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void ForEachConnected(Action<ulong> action)
        {
            foreach (PlayerRecord record in Records.Values)
            {
                if (record.IsOnline)
                {
                    action(record.SteamId);
                }
            }
        }

        internal static IReadOnlyDictionary<ulong, PlayerStatisticsDocument> GetStatisticsDictionary()
        {
            return SnapshotStatistics();
        }

        internal static IReadOnlyList<PlayerStatisticsDocument> GetAllStatistics()
        {
            List<PlayerStatisticsDocument> documents = [];
            foreach (PlayerRecord record in Records.Values)
            {
                documents.Add(record.Statistics);
            }

            return documents;
        }

        internal static bool UpdateDisplayName(ulong steamId, string displayName)
        {
            if (steamId == 0 || !SaveSlotDocumentStore.IsUsableName(displayName, steamId))
            {
                return false;
            }

            PlayerRecord record = GetOrCreate(steamId);
            bool changed = record.DisplayName != displayName;
            record.DisplayName = displayName;
            record.Statistics.DisplayName = displayName;
            record.Statistics.SteamId = steamId;
            if (changed)
            {
                BumpRevision();
            }

            return changed;
        }

        internal static string ApplyResolvedDisplayName(ulong steamId, string fallback)
        {
            if (steamId == 0)
            {
                return string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;
            }

            string displayName = StatisticsDisplayNameResolver.Resolve(steamId, fallback);
            SyncDisplayNameFields(steamId, displayName);
            return displayName;
        }

        internal static bool UpdateVoiceId(ulong steamId, string voiceId)
        {
            if (steamId == 0 || string.IsNullOrWhiteSpace(voiceId))
            {
                return false;
            }

            PlayerRecord record = GetOrCreate(steamId);
            if (record.VoiceId == voiceId)
            {
                return false;
            }

            record.VoiceId = voiceId;
            BumpRevision();
            return true;
        }

        internal static bool TryGetVoiceId(ulong steamId, out string voiceId)
        {
            if (Records.TryGetValue(steamId, out PlayerRecord? record) && !string.IsNullOrEmpty(record.VoiceId))
            {
                voiceId = record.VoiceId;
                return true;
            }

            voiceId = "";
            return false;
        }

        internal static IReadOnlyDictionary<ulong, string> GetVoiceMappings()
        {
            Dictionary<ulong, string> mappings = [];
            foreach (KeyValuePair<ulong, PlayerRecord> kvp in Records)
            {
                if (!string.IsNullOrEmpty(kvp.Value.VoiceId))
                {
                    mappings[kvp.Key] = kvp.Value.VoiceId;
                }
            }

            return mappings;
        }

        internal static void MarkDisconnected(ulong steamId)
        {
            if (steamId == 0 || !Records.TryGetValue(steamId, out PlayerRecord? record))
            {
                return;
            }

            bool changed = record.IsOnline || record.ConnectedSinceUtc.HasValue;
            record.IsOnline = false;
            record.ConnectedSinceUtc = null;
            if (changed)
            {
                BumpRevision();
            }
        }

        internal static bool IsConnected(ulong steamId)
        {
            return Records.TryGetValue(steamId, out PlayerRecord? record) && record.IsOnline;
        }

        internal static bool TryGetConnectedSince(ulong steamId, out DateTime since)
        {
            if (Records.TryGetValue(steamId, out PlayerRecord? record)
                && record.IsOnline
                && record.ConnectedSinceUtc.HasValue)
            {
                since = record.ConnectedSinceUtc.Value;
                return true;
            }

            since = default;
            return false;
        }

        internal static void SetConnectedSince(ulong steamId, DateTime since)
        {
            if (steamId == 0)
            {
                return;
            }

            PlayerRecord record = GetOrCreate(steamId);
            bool changed = !record.IsOnline || record.ConnectedSinceUtc != since;
            record.ConnectedSinceUtc = since;
            record.IsOnline = true;
            if (changed)
            {
                BumpRevision();
            }
        }

        internal static void MergeLiveSessionRoster()
        {
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    VPlayer? player = WebDashboardSessionAccess.GetVPlayer(context);
                    if (player == null)
                    {
                        continue;
                    }

                    ulong steamId = player.SteamID != 0
                        ? player.SteamID
                        : GameSessionAccess.ResolveSteamId(player.UID, player.IsHost);
                    if (steamId == 0)
                    {
                        continue;
                    }

                    string displayName = StatisticsDisplayNameResolver.Resolve(steamId, player.ActorName);
                    UpdateDisplayName(steamId, displayName);
                    if (SpeechEventPoolManager.TryResolveVoiceIdForSteam(steamId, out string? voiceId)
                        && !string.IsNullOrWhiteSpace(voiceId))
                    {
                        UpdateVoiceId(steamId, voiceId);
                    }
                }
            }

            GameSessionInfo? sessionInfo = GameSessionAccess.TryGetGameSessionInfo();
            if (sessionInfo?.TotalPlayerSteamIDs == null)
            {
                return;
            }

            foreach (ulong steamId in sessionInfo.TotalPlayerSteamIDs.Keys)
            {
                if (steamId == 0)
                {
                    continue;
                }

                string displayName = StatisticsDisplayNameResolver.Resolve(steamId, string.Empty);
                UpdateDisplayName(steamId, displayName);
            }
        }

        internal static void SyncRosterToDocument()
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            foreach (PlayerRecord record in Records.Values)
            {
                bool hasUsableName = SaveSlotDocumentStore.IsUsableName(record.DisplayName, record.SteamId);
                bool hasVoiceId = !string.IsNullOrWhiteSpace(record.VoiceId);
                if (!hasUsableName && !hasVoiceId)
                {
                    continue;
                }

                SaveSlotDocumentStore.UpsertPlayer(record.SteamId, record.DisplayName, record.VoiceId);
            }
        }

        internal static void PersistStatistics(bool waitForCompletion)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            StatisticsWriteQueue.SaveLoadedSlot(waitForCompletion);
        }

        internal static bool RemovePlayer(ulong steamId, bool waitForCompletion = true)
        {
            if (steamId == 0 || !Records.Remove(steamId))
            {
                return false;
            }

            BumpRevision();
            StatisticsStore.SaveSlot(_loadedSlotId, SnapshotStatistics(), waitForCompletion);
            SaveSlotDocumentStore.RemovePlayer(steamId);
            SyncRosterToDocument();
            return true;
        }

        internal static bool RemoveIfNeverConnected(ulong steamId)
        {
            if (steamId == 0 || IsConnected(steamId) || !Records.Remove(steamId))
            {
                return false;
            }

            BumpRevision();
            return true;
        }

        internal static bool TryGetLoadedSlotId(out int slotId)
        {
            slotId = _loadedSlotId;
            return _loadedSlotId >= 0;
        }

        private static PlayerRecord CreateRecord(ulong steamId)
        {
            return new PlayerRecord
            {
                SteamId = steamId,
                Statistics = new PlayerStatisticsDocument { SteamId = steamId },
            };
        }

        private static Dictionary<ulong, PlayerStatisticsDocument> SnapshotStatistics()
        {
            Dictionary<ulong, PlayerStatisticsDocument> snapshot = [];
            foreach (KeyValuePair<ulong, PlayerRecord> kvp in Records)
            {
                snapshot[kvp.Key] = kvp.Value.Statistics;
            }

            return snapshot;
        }

        private static void MergeRosterFromDocument()
        {
            SaveSlotDocumentStore.ApplyPlayerEntries((steamId, entry) =>
            {
                if (SaveSlotDocumentStore.IsUsableName(entry.DisplayName, steamId))
                {
                    UpdateDisplayName(steamId, entry.DisplayName);
                }

                if (!string.IsNullOrWhiteSpace(entry.VoiceId))
                {
                    UpdateVoiceId(steamId, entry.VoiceId);
                }
            });
        }

        private static void SyncDisplayNameFields(ulong steamId, string displayName)
        {
            PlayerRecord record = GetOrCreate(steamId);
            bool changed = record.Statistics.DisplayName != displayName;
            record.Statistics.DisplayName = displayName;
            record.Statistics.SteamId = steamId;

            if (SaveSlotDocumentStore.IsUsableName(displayName, steamId))
            {
                changed |= record.DisplayName != displayName;
                record.DisplayName = displayName;
            }

            if (changed)
            {
                BumpRevision();
            }
        }
    }
}
