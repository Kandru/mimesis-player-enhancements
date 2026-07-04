using System;

namespace MimesisPlayerEnhancement.Features.Statistics.Models
{
    public sealed class StatCounters
    {
        public long ItemCarryCount;
        public long DamageToAlly;
        public long MimicEncounterCount;
        public long TimeInStartingVolumeMs;
        public long CurrencyEarned;
        public long VoiceEvents;
        public long SurvivalDeaths;
        public long SurvivalWins;
        public long SurvivalLeftBehind;
        public long DeathmatchDeaths;
        public long DeathmatchWins;
        public long Revives;
        public int CyclesCompleted;
        public long TotalConnectedSeconds;
        public Dictionary<string, long> MonsterKillsByMasterId = [];
        public Dictionary<string, long> DeathsByTrapType = [];

        public void Add(StatCounters other)
        {
            if (other == null)
            {
                return;
            }

            ItemCarryCount += other.ItemCarryCount;
            DamageToAlly += other.DamageToAlly;
            MimicEncounterCount += other.MimicEncounterCount;
            TimeInStartingVolumeMs += other.TimeInStartingVolumeMs;
            CurrencyEarned += other.CurrencyEarned;
            VoiceEvents += other.VoiceEvents;
            SurvivalDeaths += other.SurvivalDeaths;
            SurvivalWins += other.SurvivalWins;
            SurvivalLeftBehind += other.SurvivalLeftBehind;
            DeathmatchDeaths += other.DeathmatchDeaths;
            DeathmatchWins += other.DeathmatchWins;
            Revives += other.Revives;
            CyclesCompleted += other.CyclesCompleted;
            TotalConnectedSeconds += other.TotalConnectedSeconds;
            MergeCountDictionary(MonsterKillsByMasterId, other.MonsterKillsByMasterId);
            MergeCountDictionary(DeathsByTrapType, other.DeathsByTrapType);
        }

        private static void MergeCountDictionary(Dictionary<string, long> target, Dictionary<string, long>? source)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, long> kvp in source)
            {
                _ = target.TryGetValue(kvp.Key, out long current);
                target[kvp.Key] = current + kvp.Value;
            }
        }

        private static Dictionary<string, long> CloneCountDictionary(Dictionary<string, long>? source)
        {
            return source == null ? [] : new Dictionary<string, long>(source);
        }

        public StatCounters Clone()
        {
            return new StatCounters
            {
                ItemCarryCount = ItemCarryCount,
                DamageToAlly = DamageToAlly,
                MimicEncounterCount = MimicEncounterCount,
                TimeInStartingVolumeMs = TimeInStartingVolumeMs,
                CurrencyEarned = CurrencyEarned,
                VoiceEvents = VoiceEvents,
                SurvivalDeaths = SurvivalDeaths,
                SurvivalWins = SurvivalWins,
                SurvivalLeftBehind = SurvivalLeftBehind,
                DeathmatchDeaths = DeathmatchDeaths,
                DeathmatchWins = DeathmatchWins,
                Revives = Revives,
                CyclesCompleted = CyclesCompleted,
                TotalConnectedSeconds = TotalConnectedSeconds,
                MonsterKillsByMasterId = CloneCountDictionary(MonsterKillsByMasterId),
                DeathsByTrapType = CloneCountDictionary(DeathsByTrapType),
            };
        }
    }

    public sealed class SessionStats
    {
        public string SessionId = "";
        public DateTime StartedAtUtc;
        public DateTime LastConnectedAtUtc;
        public DateTime? LastDisconnectedAtUtc;
        public int ReconnectCount;
        public bool IsOpen = true;
        public StatCounters Counters = new();
    }

    public sealed class GlobalStats
    {
        public StatCounters Counters = new();
        public int SessionsCompleted;
    }

    public sealed class PlayerStatisticsDocument
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public ulong SteamId;
        public string DisplayName = "";
        public GlobalStats Global = new();
        public SessionStats? CurrentSession;
        public List<SessionStats> RecentSessions = [];
    }

    public sealed class LeaderboardEntry
    {
        public ulong SteamId;
        public string DisplayName = "";
        public long ItemCarryCount;
        public long DamageToAlly;
        public long MimicEncounterCount;
        public long TimeInStartingVolumeMs;
        public long CurrencyEarned;
        public long VoiceEvents;
        public long SurvivalDeaths;
        public long SurvivalWins;
        public long SurvivalLeftBehind;
        public long DeathmatchDeaths;
        public long DeathmatchWins;
        public long Revives;
        public long TotalConnectedSeconds;
        public int SessionsCompleted;
    }

    public sealed class LeaderboardDocument
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public int SaveSlotId;
        public DateTime UpdatedAtUtc;
        public List<LeaderboardEntry> Entries = [];
    }

    public sealed class SlotStatisticsDocument
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public Dictionary<ulong, PlayerStatisticsDocument> Players = [];
    }
}
