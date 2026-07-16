namespace MimesisPlayerEnhancement.Features.Statistics.Models
{
    public sealed class StatCounters
    {
        public const int MaxLifetimeSamples = 200;

        public long ItemCarryCount;
        public long DamageToFriend;
        public long FriendsKilled;
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
        public long TrainValueDeposited;
        public long TrapDeaths;
        public long KilledByPlayers;
        public long DungeonExitsAlive;
        public long DungeonExitsDead;
        public Dictionary<string, long> MonsterKills = [];
        public Dictionary<string, long> DeathsByMonster = [];
        public Dictionary<string, long> DeathsByTrap = [];
        public List<long> LifetimesOnDeathMs = [];

        internal bool HasAnyRunData()
        {
            return ItemCarryCount != 0
                   || DamageToFriend != 0
                   || FriendsKilled != 0
                   || MimicEncounterCount != 0
                   || TimeInStartingVolumeMs != 0
                   || CurrencyEarned != 0
                   || VoiceEvents != 0
                   || SurvivalDeaths != 0
                   || SurvivalWins != 0
                   || SurvivalLeftBehind != 0
                   || DeathmatchDeaths != 0
                   || DeathmatchWins != 0
                   || Revives != 0
                   || CyclesCompleted != 0
                   || TotalConnectedSeconds != 0
                   || TrainValueDeposited != 0
                   || TrapDeaths != 0
                   || KilledByPlayers != 0
                   || DungeonExitsAlive != 0
                   || DungeonExitsDead != 0
                   || MonsterKills.Count != 0
                   || DeathsByMonster.Count != 0
                   || DeathsByTrap.Count != 0
                   || LifetimesOnDeathMs.Count != 0;
        }

        public void Add(StatCounters other)
        {
            if (other == null)
            {
                return;
            }

            ItemCarryCount += other.ItemCarryCount;
            DamageToFriend += other.DamageToFriend;
            FriendsKilled += other.FriendsKilled;
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
            TrainValueDeposited += other.TrainValueDeposited;
            TrapDeaths += other.TrapDeaths;
            KilledByPlayers += other.KilledByPlayers;
            DungeonExitsAlive += other.DungeonExitsAlive;
            DungeonExitsDead += other.DungeonExitsDead;
            MergeCountDictionary(MonsterKills, other.MonsterKills);
            MergeCountDictionary(DeathsByMonster, other.DeathsByMonster);
            MergeCountDictionary(DeathsByTrap, other.DeathsByTrap);
            AppendLifetimeSamples(LifetimesOnDeathMs, other.LifetimesOnDeathMs);
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

        private static void AppendLifetimeSamples(List<long> target, List<long>? source)
        {
            if (source == null || source.Count == 0)
            {
                return;
            }

            foreach (long sample in source)
            {
                if (target.Count >= MaxLifetimeSamples)
                {
                    target.RemoveAt(0);
                }

                target.Add(sample);
            }
        }

        private static Dictionary<string, long> CloneCountDictionary(Dictionary<string, long>? source)
        {
            return source == null ? [] : new Dictionary<string, long>(source);
        }

        private static List<long> CloneLifetimeSamples(List<long>? source)
        {
            return source == null ? [] : [.. source];
        }

        public StatCounters Clone()
        {
            return new StatCounters
            {
                ItemCarryCount = ItemCarryCount,
                DamageToFriend = DamageToFriend,
                FriendsKilled = FriendsKilled,
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
                TrainValueDeposited = TrainValueDeposited,
                TrapDeaths = TrapDeaths,
                KilledByPlayers = KilledByPlayers,
                DungeonExitsAlive = DungeonExitsAlive,
                DungeonExitsDead = DungeonExitsDead,
                MonsterKills = CloneCountDictionary(MonsterKills),
                DeathsByMonster = CloneCountDictionary(DeathsByMonster),
                DeathsByTrap = CloneCountDictionary(DeathsByTrap),
                LifetimesOnDeathMs = CloneLifetimeSamples(LifetimesOnDeathMs),
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
        public long RunRestarts;
    }

    public sealed class PlayerStatisticsDocument
    {
        public const int CurrentVersion = 5;

        public int Version = CurrentVersion;
        public ulong SteamId;
        public string DisplayName = "";
        public GlobalStats Global = new();
        public RunStats CurrentRun = new();
        public SessionStats? CurrentSession;
        public List<SessionStats> RecentSessions = [];
    }

    public sealed class LeaderboardEntry
    {
        public ulong SteamId;
        public string DisplayName = "";
        public double Score;
        public double AllTimeScore;
        public long ItemCarryCount;
        public long DamageToFriend;
        public long FriendsKilled;
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
        public long TrainValueDeposited;
        public long TrapDeaths;
        public long KilledByPlayers;
        public long DungeonExitsAlive;
        public long DungeonExitsDead;
        public long? MedianLifetimeMs;
        public int SessionsCompleted;
        public long RunRestarts;
        public StatCounters RunCounters = new();
        public StatCounters AllTimeCounters = new();
        public Dictionary<int, StatCounters> ZoneCounters = [];
    }

    public sealed class LeaderboardDocument
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public int SaveSlotId;
        public int CurrentZone;
        public DateTime UpdatedAtUtc;
        public StatCounters ServerTotals = new();
        public List<LeaderboardZoneSummary> ZoneSummaries = [];
        public List<LeaderboardEntry> Entries = [];
    }

    public sealed class SlotStatisticsDocument
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public Dictionary<ulong, PlayerStatisticsDocument> Players = [];
    }
}
