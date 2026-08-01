using Newtonsoft.Json;

namespace MimesisPlayerEnhancement.Features.Statistics.Models
{
    public sealed class StatCounters
    {
        public const int MaxLifetimeSamples = 100;

        public long TrainValueDeposited;
        public long ItemsDeposited;
        public long ItemsCarried;
        public long DamageToFriend;
        public long FriendsKilled;
        public long MimicEncounters;
        public long Deaths;
        public long KilledByFriends;
        public long Revives;
        public long SurvivalWins;
        public long SurvivalLeftBehind;
        public long DeathmatchDeaths;
        public long DeathmatchWins;
        public long DungeonExitsAlive;
        public long DungeonExitsDead;
        public long ConnectedSeconds;
        public Dictionary<string, long> MonsterKills = [];
        public Dictionary<string, long> DeathsByMonster = [];
        public Dictionary<string, long> DeathsByTrap = [];
        public List<long> LifetimesOnDeathMs = [];

        [JsonIgnore]
        public long MonsterKillTotal => SumValues(MonsterKills);

        [JsonIgnore]
        public long TrapDeathTotal => SumValues(DeathsByTrap);

        [JsonIgnore]
        public long MonsterDeathTotal => SumValues(DeathsByMonster);

        public bool HasAny()
        {
            return TrainValueDeposited != 0
                   || ItemsDeposited != 0
                   || ItemsCarried != 0
                   || DamageToFriend != 0
                   || FriendsKilled != 0
                   || MimicEncounters != 0
                   || Deaths != 0
                   || KilledByFriends != 0
                   || Revives != 0
                   || SurvivalWins != 0
                   || SurvivalLeftBehind != 0
                   || DeathmatchDeaths != 0
                   || DeathmatchWins != 0
                   || DungeonExitsAlive != 0
                   || DungeonExitsDead != 0
                   || ConnectedSeconds != 0
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

            TrainValueDeposited += other.TrainValueDeposited;
            ItemsDeposited += other.ItemsDeposited;
            ItemsCarried += other.ItemsCarried;
            DamageToFriend += other.DamageToFriend;
            FriendsKilled += other.FriendsKilled;
            MimicEncounters += other.MimicEncounters;
            Deaths += other.Deaths;
            KilledByFriends += other.KilledByFriends;
            Revives += other.Revives;
            SurvivalWins += other.SurvivalWins;
            SurvivalLeftBehind += other.SurvivalLeftBehind;
            DeathmatchDeaths += other.DeathmatchDeaths;
            DeathmatchWins += other.DeathmatchWins;
            DungeonExitsAlive += other.DungeonExitsAlive;
            DungeonExitsDead += other.DungeonExitsDead;
            ConnectedSeconds += other.ConnectedSeconds;
            MergeCountDictionary(MonsterKills, other.MonsterKills);
            MergeCountDictionary(DeathsByMonster, other.DeathsByMonster);
            MergeCountDictionary(DeathsByTrap, other.DeathsByTrap);
            AppendLifetimeSamples(LifetimesOnDeathMs, other.LifetimesOnDeathMs);
        }

        public StatCounters Clone()
        {
            return new StatCounters
            {
                TrainValueDeposited = TrainValueDeposited,
                ItemsDeposited = ItemsDeposited,
                ItemsCarried = ItemsCarried,
                DamageToFriend = DamageToFriend,
                FriendsKilled = FriendsKilled,
                MimicEncounters = MimicEncounters,
                Deaths = Deaths,
                KilledByFriends = KilledByFriends,
                Revives = Revives,
                SurvivalWins = SurvivalWins,
                SurvivalLeftBehind = SurvivalLeftBehind,
                DeathmatchDeaths = DeathmatchDeaths,
                DeathmatchWins = DeathmatchWins,
                DungeonExitsAlive = DungeonExitsAlive,
                DungeonExitsDead = DungeonExitsDead,
                ConnectedSeconds = ConnectedSeconds,
                MonsterKills = CloneCountDictionary(MonsterKills),
                DeathsByMonster = CloneCountDictionary(DeathsByMonster),
                DeathsByTrap = CloneCountDictionary(DeathsByTrap),
                LifetimesOnDeathMs = CloneLifetimeSamples(LifetimesOnDeathMs),
            };
        }

        internal static void EnsureDictionaries(StatCounters counters)
        {
            counters.MonsterKills ??= [];
            counters.DeathsByMonster ??= [];
            counters.DeathsByTrap ??= [];
            counters.LifetimesOnDeathMs ??= [];
        }

        private static long SumValues(Dictionary<string, long>? values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            long total = 0;
            foreach (long value in values.Values)
            {
                total += value;
            }

            return total;
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
    }
}
