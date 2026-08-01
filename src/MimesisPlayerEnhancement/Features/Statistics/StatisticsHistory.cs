using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal readonly struct DungeonRunIdentity
    {
        internal readonly int Zone;
        internal readonly int Cycle;
        internal readonly int Seed;
        internal readonly int DungeonMasterId;
        internal readonly int MapId;

        internal DungeonRunIdentity(int zone, int cycle, int seed, int dungeonMasterId, int mapId)
        {
            Zone = zone;
            Cycle = cycle;
            Seed = seed;
            DungeonMasterId = dungeonMasterId;
            MapId = mapId;
        }
    }

    internal static class StatisticsHistory
    {
        private const string Feature = "Statistics";
        internal const int MaxRunsPerZone = 60;
        internal const int MaxZonesRetained = 40;
        private const int MaxZoneGapFill = 5;

        private static SlotStatisticsDocument _document = new();
        private static int _revision;
        private static DungeonRunRecord? _openRun;
        private static DungeonRunRecord? _lastClosedRun;
        private static long _lastClosedRunMs;

        internal static int Revision => _revision;

        internal static SlotStatisticsDocument Document => _document;

        internal static int CurrentZone =>
            _document.History.CurrentZone > 0 ? _document.History.CurrentZone : 1;

        internal static void Load(SlotStatisticsDocument document)
        {
            _document = document ?? new SlotStatisticsDocument();
            NormalizeDocument(_document);
            _openRun = FindOpenRun();
            _lastClosedRun = null;
            BumpRevision();
        }

        internal static SlotStatisticsDocument CloneDocument()
        {
            return CloneSlot(_document);
        }

        internal static void BumpRevision()
        {
            _revision++;
            _document.UpdatedAtUtc = DateTime.UtcNow;
        }

        internal static PlayerGlobalStats EnsureGlobal(ulong steamId, string? displayName = null)
        {
            if (!_document.Globals.TryGetValue(steamId, out PlayerGlobalStats? global))
            {
                DateTime now = DateTime.UtcNow;
                global = new PlayerGlobalStats
                {
                    SteamId = steamId,
                    DisplayName = displayName ?? "",
                    FirstSeenUtc = now,
                    LastSeenUtc = now,
                };
                _document.Globals[steamId] = global;
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                global.DisplayName = displayName;
            }

            global.LastSeenUtc = DateTime.UtcNow;
            StatCounters.EnsureDictionaries(global.Counters);
            return global;
        }

        internal static StatCounters GetPlayerCounters(ulong steamId, CounterScope scope)
        {
            List<StatCounters> targets = [];
            CollectTargets(steamId, scope, targets);
            return targets.Count > 0 ? targets[0] : new StatCounters();
        }

        internal static void Apply(ulong steamId, Action<StatCounters> apply, CounterScope scope = CounterScope.All)
        {
            if (steamId == 0 || apply == null)
            {
                return;
            }

            List<StatCounters> targets = [];
            CollectTargets(steamId, scope, targets);
            foreach (StatCounters counters in targets)
            {
                apply(counters);
            }
        }

        internal static void OpenRun(DungeonRunIdentity identity)
        {
            string runId = BuildRunId(identity);
            if (_openRun != null)
            {
                if (_openRun.RunId == runId)
                {
                    return;
                }

                CloseRun(DungeonRunOutcome.Abandoned, notify: false);
            }

            ZoneRecord zone = EnsureZone(identity.Zone);
            StatisticsMapNames.Resolve(identity.MapId, out string mapKey, out string mapName);
            _openRun = new DungeonRunRecord
            {
                RunId = EnsureUniqueRunId(zone, runId),
                Zone = identity.Zone,
                Cycle = identity.Cycle,
                Seed = identity.Seed,
                DungeonMasterId = identity.DungeonMasterId,
                MapId = identity.MapId,
                MapKey = mapKey,
                MapName = mapName,
                StartedAtUtc = DateTime.UtcNow,
                Outcome = DungeonRunOutcome.InProgress,
            };
            zone.Runs.Add(_openRun);
            TrimRuns(zone);
            BumpRevision();
        }

        internal static void CloseRun(DungeonRunOutcome outcome, bool notify = true)
        {
            if (_openRun == null)
            {
                return;
            }

            _openRun.Outcome = outcome;
            _openRun.EndedAtUtc = DateTime.UtcNow;
            foreach (ulong steamId in _openRun.Players.Keys)
            {
                if (steamId == 0)
                {
                    continue;
                }

                PlayerGlobalStats global = EnsureGlobal(steamId);
                global.DungeonRunsPlayed++;
            }

            _lastClosedRun = _openRun;
            _lastClosedRunMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            _openRun = null;
            BumpRevision();

            if (notify)
            {
                StatisticsCounterWriter.NotifyChanged();
            }
        }

        internal static void OnZoneAdvanced(int newZone)
        {
            if (newZone <= 0 || newZone == CurrentZone)
            {
                return;
            }

            AdvanceTo(newZone);

            foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
            {
                PlayerGlobalStats global = EnsureGlobal(steamId);
                if (newZone > global.HighestZoneReached)
                {
                    global.HighestZoneReached = newZone;
                }
            }

            BumpRevision();
        }

        /// <summary>
        /// Reconciles recorded zone state with the live game StageCount. Returns true when state changed.
        /// </summary>
        internal static bool SyncCurrentZone(int gameZone)
        {
            if (gameZone <= 0 || gameZone == CurrentZone)
            {
                return false;
            }

            if (_document.History.Zones.Count == 0)
            {
                // Nothing recorded yet — adopt the game's zone instead of inventing history for zones 1..N-1.
                _document.History.CurrentZone = gameZone;
                BumpRevision();
                return true;
            }

            AdvanceTo(gameZone);
            BumpRevision();
            return true;
        }

        internal static void OnRunRestart()
        {
            CloseRun(DungeonRunOutcome.Abandoned, notify: false);

            bool hadHistory = _document.History.Zones.Count > 0
                              || _document.Globals.Values.Any(static g => g.Counters.HasAny());

            if (hadHistory)
            {
                foreach (PlayerGlobalStats global in _document.Globals.Values)
                {
                    global.RunRestarts++;
                }
            }

            _document.History = new ZoneHistory { CurrentZone = 1 };
            _openRun = null;
            _lastClosedRun = null;
            BumpRevision();

            if (hadHistory)
            {
                ModLog.Info(Feature, "Run statistics reset — zone restart recorded.");
            }
        }

        internal static bool HasHistoryData()
        {
            if (_document.History.Zones.Count > 0)
            {
                return true;
            }

            foreach (PlayerGlobalStats global in _document.Globals.Values)
            {
                if (global.Counters.HasAny())
                {
                    return true;
                }
            }

            return false;
        }

        private static void CollectTargets(ulong steamId, CounterScope scope, List<StatCounters> targets)
        {
            _ = EnsureGlobal(steamId);

            if ((scope & CounterScope.Session) != 0)
            {
                SessionStats? session = StatisticsRuntime.GetCurrentSession(steamId);
                if (session != null)
                {
                    StatCounters.EnsureDictionaries(session.Counters);
                    targets.Add(session.Counters);
                }
            }

            if ((scope & CounterScope.Global) != 0)
            {
                targets.Add(_document.Globals[steamId].Counters);
            }

            if ((scope & CounterScope.Zone) != 0)
            {
                ZoneRecord zone = EnsureZone(CurrentZone);
                targets.Add(GetOrCreatePlayerCounters(zone.Players, steamId));
            }

            if ((scope & CounterScope.Run) != 0)
            {
                DungeonRunRecord? run = ResolveRunTarget();
                if (run != null)
                {
                    targets.Add(GetOrCreatePlayerCounters(run.Players, steamId));
                }
            }
        }

        private static DungeonRunRecord? ResolveRunTarget()
        {
            if (_openRun != null)
            {
                return _openRun;
            }

            long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (_lastClosedRun != null && now - _lastClosedRunMs <= 2000)
            {
                return _lastClosedRun;
            }

            return null;
        }

        private static DungeonRunRecord? FindOpenRun()
        {
            foreach (ZoneRecord zone in _document.History.Zones)
            {
                foreach (DungeonRunRecord run in zone.Runs)
                {
                    if (run.Outcome == DungeonRunOutcome.InProgress)
                    {
                        return run;
                    }
                }
            }

            return null;
        }

        private static void AdvanceTo(int newZone)
        {
            int previousZone = CurrentZone;
            CloseRun(DungeonRunOutcome.Abandoned, notify: false);
            CloseZone(previousZone);

            int gap = newZone - previousZone - 1;
            if (gap > 0 && gap <= MaxZoneGapFill)
            {
                for (int zone = previousZone + 1; zone < newZone; zone++)
                {
                    ZoneRecord skipped = EnsureZone(zone);
                    skipped.EndedAtUtc ??= DateTime.UtcNow;
                }
            }
            else if (gap > MaxZoneGapFill)
            {
                ModLog.Warn(Feature, $"Zone jumped {previousZone} to {newZone} — intermediate zones not recorded.");
            }

            _document.History.CurrentZone = newZone;
            ZoneRecord current = EnsureZone(newZone);
            current.EndedAtUtc = null;
            TrimZones();
        }

        private static ZoneRecord EnsureZone(int zone)
        {
            foreach (ZoneRecord record in _document.History.Zones)
            {
                if (record.Zone == zone)
                {
                    return record;
                }
            }

            ZoneRecord created = new()
            {
                Zone = zone,
                StartedAtUtc = DateTime.UtcNow,
            };
            _document.History.Zones.Add(created);
            _document.History.Zones.Sort(static (a, b) => a.Zone.CompareTo(b.Zone));
            return created;
        }

        private static void CloseZone(int zone)
        {
            foreach (ZoneRecord record in _document.History.Zones)
            {
                if (record.Zone == zone && !record.EndedAtUtc.HasValue)
                {
                    record.EndedAtUtc = DateTime.UtcNow;
                    return;
                }
            }
        }

        private static string BuildRunId(DungeonRunIdentity identity)
        {
            return $"z{identity.Zone}-c{identity.Cycle}-s{identity.Seed}";
        }

        private static string EnsureUniqueRunId(ZoneRecord zone, string baseId)
        {
            if (!zone.Runs.Any(run => run.RunId == baseId))
            {
                return baseId;
            }

            int suffix = 2;
            while (zone.Runs.Any(run => run.RunId == $"{baseId}-{suffix}"))
            {
                suffix++;
            }

            return $"{baseId}-{suffix}";
        }

        private static void TrimRuns(ZoneRecord zone)
        {
            while (zone.Runs.Count > MaxRunsPerZone)
            {
                zone.Runs.RemoveAt(0);
                zone.TrimmedRunCount++;
            }
        }

        private static void TrimZones()
        {
            while (_document.History.Zones.Count > MaxZonesRetained)
            {
                _document.History.Zones.RemoveAt(0);
                _document.History.TrimmedZoneCount++;
            }
        }

        private static StatCounters GetOrCreatePlayerCounters(Dictionary<ulong, StatCounters> players, ulong steamId)
        {
            if (!players.TryGetValue(steamId, out StatCounters? counters))
            {
                counters = new StatCounters();
                players[steamId] = counters;
            }

            StatCounters.EnsureDictionaries(counters);
            return counters;
        }

        private static void NormalizeDocument(SlotStatisticsDocument document)
        {
            document.Globals ??= [];
            document.History ??= new ZoneHistory();
            document.History.Zones ??= [];
            if (document.History.CurrentZone <= 0)
            {
                document.History.CurrentZone = 1;
            }

            foreach (PlayerGlobalStats global in document.Globals.Values)
            {
                StatCounters.EnsureDictionaries(global.Counters);
                if (global.HighestZoneReached <= 0)
                {
                    global.HighestZoneReached = 1;
                }
            }

            foreach (ZoneRecord zone in document.History.Zones)
            {
                zone.Players ??= [];
                zone.Runs ??= [];
                foreach (StatCounters counters in zone.Players.Values)
                {
                    StatCounters.EnsureDictionaries(counters);
                }

                foreach (DungeonRunRecord run in zone.Runs)
                {
                    run.Players ??= [];
                    foreach (StatCounters counters in run.Players.Values)
                    {
                        StatCounters.EnsureDictionaries(counters);
                    }
                }
            }
        }

        internal static SlotStatisticsDocument CloneSlot(SlotStatisticsDocument source)
        {
            SlotStatisticsDocument clone = new()
            {
                Version = source.Version,
                UpdatedAtUtc = source.UpdatedAtUtc,
                History = new ZoneHistory
                {
                    CurrentZone = source.History.CurrentZone,
                    TrimmedZoneCount = source.History.TrimmedZoneCount,
                },
            };

            foreach (KeyValuePair<ulong, PlayerGlobalStats> pair in source.Globals)
            {
                clone.Globals[pair.Key] = CloneGlobal(pair.Value);
            }

            foreach (ZoneRecord zone in source.History.Zones)
            {
                clone.History.Zones.Add(CloneZone(zone));
            }

            return clone;
        }

        private static PlayerGlobalStats CloneGlobal(PlayerGlobalStats source)
        {
            return new PlayerGlobalStats
            {
                SteamId = source.SteamId,
                DisplayName = source.DisplayName,
                HighestZoneReached = source.HighestZoneReached,
                RunRestarts = source.RunRestarts,
                SessionsCompleted = source.SessionsCompleted,
                DungeonRunsPlayed = source.DungeonRunsPlayed,
                VoiceEvents = source.VoiceEvents,
                FirstSeenUtc = source.FirstSeenUtc,
                LastSeenUtc = source.LastSeenUtc,
                Counters = source.Counters.Clone(),
            };
        }

        private static ZoneRecord CloneZone(ZoneRecord source)
        {
            ZoneRecord clone = new()
            {
                Zone = source.Zone,
                StartedAtUtc = source.StartedAtUtc,
                EndedAtUtc = source.EndedAtUtc,
                TrimmedRunCount = source.TrimmedRunCount,
            };

            foreach (KeyValuePair<ulong, StatCounters> pair in source.Players)
            {
                clone.Players[pair.Key] = pair.Value.Clone();
            }

            foreach (DungeonRunRecord run in source.Runs)
            {
                clone.Runs.Add(CloneRun(run));
            }

            return clone;
        }

        private static DungeonRunRecord CloneRun(DungeonRunRecord source)
        {
            DungeonRunRecord clone = new()
            {
                RunId = source.RunId,
                Zone = source.Zone,
                Cycle = source.Cycle,
                Seed = source.Seed,
                DungeonMasterId = source.DungeonMasterId,
                MapId = source.MapId,
                MapKey = source.MapKey,
                MapName = source.MapName,
                StartedAtUtc = source.StartedAtUtc,
                EndedAtUtc = source.EndedAtUtc,
                Outcome = source.Outcome,
            };

            foreach (KeyValuePair<ulong, StatCounters> pair in source.Players)
            {
                clone.Players[pair.Key] = pair.Value.Clone();
            }

            return clone;
        }
    }

    [Flags]
    internal enum CounterScope
    {
        Session = 1,
        Global = 2,
        Zone = 4,
        Run = 8,
        All = Session | Global | Zone | Run,
        Persisted = Global | Zone | Run,
    }
}
