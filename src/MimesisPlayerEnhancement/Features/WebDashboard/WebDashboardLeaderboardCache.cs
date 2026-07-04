using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardLeaderboardCache
    {
        private const string Feature = "WebDashboard";

        private static readonly object SerializeLock = new();

        private static string? _cachedJson;
        private static int _cachedSlotId = -1;
        private static long _cachedContentHash;
        private static int _serializeInFlight;

        internal static string? GetOrSchedule(
            int saveSlotId,
            LeaderboardDocument? doc,
            IReadOnlyList<ulong> connectedSteamIds)
        {
            if (doc == null)
            {
                return _cachedSlotId == saveSlotId ? _cachedJson : null;
            }

            long contentHash = ComputeHash(doc, connectedSteamIds);
            if (_cachedSlotId == saveSlotId
                && _cachedContentHash == contentHash
                && !string.IsNullOrEmpty(_cachedJson))
            {
                return _cachedJson;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _serializeInFlight, 1, 0) == 0)
            {
                LeaderboardDocument snapshot = CloneDocument(doc);
                List<ulong> connectedIds = [.. connectedSteamIds];
                _ = Task.Run(() => SerializeBackground(saveSlotId, snapshot, connectedIds, contentHash));
            }

            return _cachedSlotId == saveSlotId ? _cachedJson : null;
        }

        internal static void Clear()
        {
            lock (SerializeLock)
            {
                _cachedJson = null;
                _cachedSlotId = -1;
                _cachedContentHash = 0;
            }
        }

        private static void SerializeBackground(
            int saveSlotId,
            LeaderboardDocument doc,
            List<ulong> connectedSteamIds,
            long contentHash)
        {
            try
            {
                string json = WebDashboardJson.SerializeLeaderboardResponse(doc, connectedSteamIds);
                lock (SerializeLock)
                {
                    _cachedJson = json;
                    _cachedSlotId = saveSlotId;
                    _cachedContentHash = contentHash;
                }

                WebDashboardSnapshotCache.MarkDirty();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background leaderboard serialize failed: {ex.Message}");
            }
            finally
            {
                _ = System.Threading.Interlocked.Exchange(ref _serializeInFlight, 0);
            }
        }

        private static long ComputeHash(LeaderboardDocument doc, IReadOnlyList<ulong> connectedSteamIds)
        {
            unchecked
            {
                long hash = doc.SaveSlotId;
                hash = (hash * 397) ^ doc.UpdatedAtUtc.Ticks;
                hash = (hash * 397) ^ doc.Entries.Count;
                foreach (LeaderboardEntry entry in doc.Entries)
                {
                    hash = (hash * 397) ^ (long)entry.SteamId;
                    hash = (hash * 397) ^ entry.CurrencyEarned;
                    hash = (hash * 397) ^ entry.VoiceEvents;
                }

                foreach (ulong steamId in connectedSteamIds)
                {
                    hash = (hash * 397) ^ (long)steamId;
                }

                return hash;
            }
        }

        private static LeaderboardDocument CloneDocument(LeaderboardDocument source)
        {
            List<LeaderboardEntry> entries = [];
            foreach (LeaderboardEntry entry in source.Entries)
            {
                entries.Add(new LeaderboardEntry
                {
                    SteamId = entry.SteamId,
                    DisplayName = entry.DisplayName,
                    ItemCarryCount = entry.ItemCarryCount,
                    DamageToAlly = entry.DamageToAlly,
                    MimicEncounterCount = entry.MimicEncounterCount,
                    TimeInStartingVolumeMs = entry.TimeInStartingVolumeMs,
                    CurrencyEarned = entry.CurrencyEarned,
                    VoiceEvents = entry.VoiceEvents,
                    SurvivalDeaths = entry.SurvivalDeaths,
                    SurvivalWins = entry.SurvivalWins,
                    SurvivalLeftBehind = entry.SurvivalLeftBehind,
                    DeathmatchDeaths = entry.DeathmatchDeaths,
                    DeathmatchWins = entry.DeathmatchWins,
                    Revives = entry.Revives,
                    TotalConnectedSeconds = entry.TotalConnectedSeconds,
                    SessionsCompleted = entry.SessionsCompleted,
                });
            }

            return new LeaderboardDocument
            {
                Version = source.Version,
                SaveSlotId = source.SaveSlotId,
                UpdatedAtUtc = source.UpdatedAtUtc,
                Entries = entries,
            };
        }
    }
}
