using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootTableRollHelper
    {
        internal static int PickWeightedItemMasterId(IReadOnlyList<ItemDropCandidateInfo> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            int totalRate = candidates.Sum(candidate => candidate.Rate);
            if (totalRate <= 0)
            {
                return 0;
            }

            int roll = SimpleRandUtil.Next(0, totalRate + 1);
            int cumulative = 0;

            foreach (ItemDropCandidateInfo candidate in candidates.OrderByDescending(c => c.Rate))
            {
                cumulative += candidate.Rate;
                if (roll <= cumulative)
                {
                    return candidate.ItemMasterID;
                }
            }

            return candidates[candidates.Count - 1].ItemMasterID;
        }

        internal static int PickWeightedItemMasterId(IReadOnlyList<(int ItemMasterID, int Rate)> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            int totalRate = 0;
            foreach ((int _, int rate) in candidates)
            {
                totalRate += rate;
            }

            if (totalRate <= 0)
            {
                return 0;
            }

            int roll = SimpleRandUtil.Next(0, totalRate);
            int cumulative = 0;

            foreach ((int itemMasterId, int rate) in candidates)
            {
                cumulative += rate;
                if (roll < cumulative)
                {
                    return itemMasterId;
                }
            }

            return candidates[candidates.Count - 1].ItemMasterID;
        }

        internal static float GetRateWeightedMultiplier(
            LootSource source,
            IEnumerable<KeyValuePair<int, int>> rateByMasterId,
            int playerCount)
        {
            float weightedSum = 0f;
            int totalRate = 0;

            foreach (KeyValuePair<int, int> entry in rateByMasterId)
            {
                if (entry.Key <= 0 || entry.Value <= 0 || !LootItemFilter.IsEligible(entry.Key))
                {
                    continue;
                }

                float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                    source,
                    ItemTypeLookup.GetItemType(entry.Key),
                    playerCount,
                    entry.Key);
                if (multiplier <= 0f)
                {
                    continue;
                }

                weightedSum += entry.Value * multiplier;
                totalRate += entry.Value;
            }

            return totalRate > 0 ? weightedSum / totalRate : 1f;
        }

        internal static float GetRateWeightedMultiplier(
            LootSource source,
            ImmutableArray<ItemDropCandidateInfo> candidates,
            int playerCount)
        {
            List<KeyValuePair<int, int>> entries = [];
            foreach (ItemDropCandidateInfo candidate in candidates)
            {
                entries.Add(new KeyValuePair<int, int>(candidate.ItemMasterID, candidate.Rate));
            }

            return GetRateWeightedMultiplier(source, entries, playerCount);
        }
    }
}
