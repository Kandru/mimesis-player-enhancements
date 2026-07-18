using System.Collections;
using System.Collections.Immutable;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class MapLootBudgetPriceHelper
    {
        private const float MaxFilterPriceRatio = 100f;

        private static readonly FieldInfo SpawnedActorDatasField = LootMultiplicatorFields.SpawnedActorDatasField;

        internal static float GetFilterPriceRatio(DungeonRoom room, DungeonMasterInfo dungeonInfo)
        {
            if (!SceneScopedConfigGate.Loot.AutoScaleMapLootBudgetForFilter
                || !LootItemFilter.IsFilterActive()
                || dungeonInfo.SpawnableItemInfo == null)
            {
                return 1f;
            }

            if (!TryGetVanillaWeightedMeanPrice(dungeonInfo.SpawnableItemInfo.MiscRateDict, out float vanillaMean)
                || vanillaMean <= 0f)
            {
                return 1f;
            }

            if (!TryGetFilteredWeightedMeanPrice(room, out float filteredMean)
                || filteredMean <= 0f)
            {
                return 1f;
            }

            float ratio = filteredMean / vanillaMean;
            if (ratio <= 1f)
            {
                return 1f;
            }

            return ratio > MaxFilterPriceRatio ? MaxFilterPriceRatio : ratio;
        }

        private static bool TryGetVanillaWeightedMeanPrice(
            ImmutableDictionary<int, int> rateByMasterId,
            out float weightedMean)
        {
            weightedMean = 0f;
            if (rateByMasterId.Count == 0)
            {
                return false;
            }

            float weightedSum = 0f;
            int totalWeight = 0;

            foreach (KeyValuePair<int, int> entry in rateByMasterId)
            {
                if (entry.Key <= 0 || entry.Value <= 0)
                {
                    continue;
                }

                if (!ItemTypeLookup.TryGetItem(entry.Key, out ItemMasterInfo info))
                {
                    continue;
                }

                weightedSum += entry.Value * info.GetMeanPrice();
                totalWeight += entry.Value;
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            weightedMean = weightedSum / totalWeight;
            return true;
        }

        private static bool TryGetFilteredWeightedMeanPrice(DungeonRoom room, out float weightedMean)
        {
            weightedMean = 0f;

            if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas)
            {
                return false;
            }

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not RandomSpawnedItemActorData randomSpawn
                    || randomSpawn.Candidates.Count == 0)
                {
                    continue;
                }

                if (TryGetWeightedMeanPrice(randomSpawn.Candidates, out weightedMean))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetWeightedMeanPrice(
            ImmutableDictionary<int, (int rate, int meanPrice)> candidates,
            out float weightedMean)
        {
            weightedMean = 0f;
            List<(int weight, int meanPrice)> entries = ExtractIndividualRates(candidates);
            if (entries.Count == 0)
            {
                return false;
            }

            float weightedSum = 0f;
            int totalWeight = 0;

            foreach ((int weight, int meanPrice) in entries)
            {
                if (weight <= 0 || meanPrice <= 0)
                {
                    continue;
                }

                weightedSum += weight * meanPrice;
                totalWeight += weight;
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            weightedMean = weightedSum / totalWeight;
            return true;
        }

        private static List<(int weight, int meanPrice)> ExtractIndividualRates(
            ImmutableDictionary<int, (int rate, int meanPrice)> candidates)
        {
            List<(int weight, int meanPrice)> entries = [];
            int previousCumulative = 0;

            foreach (KeyValuePair<int, (int rate, int meanPrice)> entry in candidates)
            {
                int weight = entry.Value.rate - previousCumulative;
                previousCumulative = entry.Value.rate;
                if (weight > 0)
                {
                    entries.Add((weight, entry.Value.meanPrice));
                }
            }

            return entries;
        }
    }
}
