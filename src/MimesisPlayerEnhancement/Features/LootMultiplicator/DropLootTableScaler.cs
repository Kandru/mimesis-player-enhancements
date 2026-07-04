using System.Collections.Generic;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class DropLootTableScaler
    {
        private const string Feature = "LootMultiplicator";

        internal static void ScaleDropList(ItemDropInfo dropInfo, List<int> dropList)
        {
            if (!LootScalingGate.ShouldScale() || dropInfo == null || dropList == null)
            {
                return;
            }

            if (dropInfo.ItemDropCandidates.Length == 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromSession();
            float multiplier = LootTableRollHelper.GetRateWeightedMultiplier(
                LootSource.Drop,
                dropInfo.ItemDropCandidates,
                playerCount);

            if (multiplier <= 1f)
            {
                return;
            }

            int vanillaCount = dropList.Count;
            int targetCount = LootMultiplierResolver.ScaleCount(vanillaCount, multiplier);
            int extraNeeded = targetCount - vanillaCount;
            if (extraNeeded <= 0)
            {
                return;
            }

            int added = 0;
            for (int i = 0; i < extraNeeded; i++)
            {
                int masterId = LootTableRollHelper.PickWeightedItemMasterId(dropInfo.ItemDropCandidates);
                if (masterId <= 0 || !LootItemFilter.IsEligible(masterId))
                {
                    continue;
                }

                dropList.Add(masterId);
                added++;
            }

            if (added > 0)
            {
                LootMultiplicatorLog.InfoDropTableScaled(vanillaCount, added, playerCount);
            }
        }
    }
}
