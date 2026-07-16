using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class TeamValueScore
    {
        private const double TrainValueWeight = 1.0;
        private const double MonsterKillWeight = 25.0;
        private const double ReviveWeight = 100.0;
        private const double FriendKillPenalty = 200.0;
        private const double FriendDamagePenalty = 0.5;
        private const double SurvivalDeathPenalty = 50.0;

        internal static double Compute(StatCounters counters)
        {
            if (counters == null)
            {
                return 0;
            }

            long monsterKills = SumDictionary(counters.MonsterKills);
            return counters.TrainValueDeposited * TrainValueWeight
                   + monsterKills * MonsterKillWeight
                   + counters.Revives * ReviveWeight
                   - counters.FriendsKilled * FriendKillPenalty
                   - counters.DamageToFriend * FriendDamagePenalty
                   - counters.SurvivalDeaths * SurvivalDeathPenalty;
        }

        internal static long? ComputeMedianLifetimeMs(IReadOnlyList<long>? lifetimes)
        {
            if (lifetimes == null || lifetimes.Count == 0)
            {
                return null;
            }

            long[] sorted = new long[lifetimes.Count];
            for (int i = 0; i < lifetimes.Count; i++)
            {
                sorted[i] = lifetimes[i];
            }

            Array.Sort(sorted);
            int mid = sorted.Length / 2;
            return sorted.Length % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2
                : sorted[mid];
        }

        private static long SumDictionary(Dictionary<string, long>? values)
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
    }
}
