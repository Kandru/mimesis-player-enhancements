using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsWriteQueue
    {
        private const string Feature = "Statistics";
        private const float DebounceSeconds = 3f;
        private const int DirtyFlushThreshold = 8;

        private static readonly HashSet<ulong> DirtyPlayers = [];
        private static readonly HashSet<int> DirtyLeaderboards = [];
        private static readonly ConcurrentDictionary<string, PendingWrite> InFlight = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object DirtyLock = new();

        private static float _nextFlushTime;
        private static int _loadedSlotId = -999;
        private static Func<ulong, PlayerStatisticsDocument?>? _documentResolver;
        private static Func<int, LeaderboardDocument>? _leaderboardResolver;

        private sealed class PendingWrite
        {
            internal string? LatestJson;
            internal Task? WriteTask;
        }

        internal static void Configure(
            int slotId,
            Func<ulong, PlayerStatisticsDocument?> documentResolver,
            Func<int, LeaderboardDocument> leaderboardResolver)
        {
            _loadedSlotId = slotId;
            _documentResolver = documentResolver;
            _leaderboardResolver = leaderboardResolver;
        }

        internal static void Clear()
        {
            lock (DirtyLock)
            {
                DirtyPlayers.Clear();
                DirtyLeaderboards.Clear();
            }

            _nextFlushTime = 0;
            _loadedSlotId = -999;
            _documentResolver = null;
            _leaderboardResolver = null;
        }

        internal static void MarkDirty(ulong steamId)
        {
            lock (DirtyLock)
            {
                _ = DirtyPlayers.Add(steamId);
                if (_nextFlushTime <= UnityEngine.Time.time)
                {
                    _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
                }
            }
        }

        internal static void MarkLeaderboardDirty(int slotId)
        {
            lock (DirtyLock)
            {
                _ = DirtyLeaderboards.Add(slotId);
                if (_nextFlushTime <= UnityEngine.Time.time)
                {
                    _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
                }
            }
        }

        internal static void SavePlayerImmediate(int slotId, PlayerStatisticsDocument doc)
        {
            lock (DirtyLock)
            {
                _ = DirtyPlayers.Remove(doc.SteamId);
            }

            StatisticsStore.SavePlayer(slotId, doc);
        }

        internal static void SaveLeaderboardImmediate(int slotId, LeaderboardDocument leaderboard)
        {
            lock (DirtyLock)
            {
                _ = DirtyLeaderboards.Remove(slotId);
            }

            StatisticsStore.SaveLeaderboard(slotId, leaderboard);
        }

        internal static void ProcessDebounced()
        {
            int dirtyCount;
            lock (DirtyLock)
            {
                dirtyCount = DirtyPlayers.Count + DirtyLeaderboards.Count;
            }

            if (dirtyCount == 0)
            {
                return;
            }

            if (dirtyCount < DirtyFlushThreshold && UnityEngine.Time.time < _nextFlushTime)
            {
                return;
            }

            FlushDirty(async: true);
            _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
            WebDashboardSnapshotCache.MarkDirty();
        }

        internal static void FlushAllSync()
        {
            FlushDirty(async: false);
            WaitForInFlightWrites();
        }

        private static void FlushDirty(bool async)
        {
            ulong[] players;
            int[] leaderboards;
            lock (DirtyLock)
            {
                players = new ulong[DirtyPlayers.Count];
                DirtyPlayers.CopyTo(players);
                DirtyPlayers.Clear();

                leaderboards = new int[DirtyLeaderboards.Count];
                DirtyLeaderboards.CopyTo(leaderboards);
                DirtyLeaderboards.Clear();
            }

            int slotId = _loadedSlotId;
            if (slotId < 0 || _documentResolver == null)
            {
                return;
            }

            foreach (ulong steamId in players)
            {
                if (_documentResolver(steamId) is not { } doc)
                {
                    continue;
                }

                string? path = StatisticsStore.GetPlayerFilePath(slotId, steamId);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                string json = StatisticsStore.SerializePreparedPlayer(doc);
                EnqueueWrite(path, json, waitForCompletion: !async);
            }

            if (_leaderboardResolver == null)
            {
                return;
            }

            foreach (int leaderboardSlot in leaderboards)
            {
                LeaderboardDocument leaderboard = _leaderboardResolver(leaderboardSlot);
                string? path = StatisticsStore.GetLeaderboardFilePath(leaderboardSlot);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                leaderboard.Version = LeaderboardDocument.CurrentVersion;
                leaderboard.SaveSlotId = leaderboardSlot;
                leaderboard.UpdatedAtUtc = DateTime.UtcNow;
                string json = StatisticsJson.SerializeLeaderboard(leaderboard);
                EnqueueWrite(path, json, waitForCompletion: !async);
            }
        }

        private static void EnqueueWrite(string path, string json, bool waitForCompletion)
        {
            PendingWrite pending = InFlight.GetOrAdd(path, static _ => new PendingWrite());
            lock (pending)
            {
                pending.LatestJson = json;
                if (pending.WriteTask is { IsCompleted: false })
                {
                    if (!waitForCompletion)
                    {
                        return;
                    }

                    WaitForTask(pending.WriteTask);
                }

                string snapshot = json;
                pending.WriteTask = Task.Run(() => WriteToDisk(path, snapshot));
                if (waitForCompletion)
                {
                    WaitForTask(pending.WriteTask);
                }
            }
        }

        private static void WriteToDisk(string path, string json)
        {
            try
            {
                StatisticsStore.SafeWriteText(path, json);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Background statistics write failed ({System.IO.Path.GetFileName(path)}): {ex.Message}");
            }

            CompleteWrite(path, json);
        }

        private static void CompleteWrite(string path, string json)
        {
            if (!InFlight.TryGetValue(path, out PendingWrite? pending))
            {
                return;
            }

            lock (pending)
            {
                if (pending.LatestJson != null && pending.LatestJson != json)
                {
                    string newer = pending.LatestJson;
                    pending.LatestJson = null;
                    pending.WriteTask = Task.Run(() => WriteToDisk(path, newer));
                    return;
                }

                pending.LatestJson = null;
                _ = InFlight.TryRemove(path, out _);
            }
        }

        private static void WaitForInFlightWrites()
        {
            foreach (KeyValuePair<string, PendingWrite> kvp in InFlight)
            {
                Task? task;
                lock (kvp.Value)
                {
                    task = kvp.Value.WriteTask;
                }

                WaitForTask(task);
            }
        }

        private static void WaitForTask(Task? task)
        {
            if (task == null || task.IsCompleted)
            {
                return;
            }

            try
            {
                _ = task.Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Statistics write wait failed: {ex.Message}");
            }
        }
    }
}
