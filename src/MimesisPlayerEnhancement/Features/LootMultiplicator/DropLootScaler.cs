using System.Collections.Generic;
using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Features.SpawnScaling;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class DropLootScaler
{
    internal static void ScaleDropList(List<int>? result)
    {
        if (!ModConfig.EnableLootMultiplicator.Value || result == null || result.Count == 0)
            return;

        if (SpawnScalingHost.IsParticipantClient() || !SpawnScalingHost.ShouldApplyScaling())
            return;

        int playerCount = LootPlayerCountHelper.ResolvePlayerCount(null);
        var additions = new List<int>();
        int entriesChanged = 0;

        foreach (int masterId in result)
        {
            if (masterId <= 0)
                continue;

            ItemType itemType = ItemTypeLookup.GetItemType(masterId);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Drop, itemType, playerCount);
            int targetCopies = LootMultiplierResolver.ScaleCount(1, multiplier);
            int extraCopies = targetCopies - 1;

            if (extraCopies > 0)
            {
                entriesChanged++;
                for (int i = 0; i < extraCopies; i++)
                    additions.Add(masterId);

                LootMultiplicatorLog.InfoRuntimeScaled(
                    LootSource.Drop,
                    itemType,
                    masterId,
                    1,
                    targetCopies,
                    multiplier,
                    "GetDropItemList");

                LootMultiplicatorLog.DebugLootScaled(
                    LootSource.Drop,
                    itemType,
                    masterId,
                    1,
                    targetCopies,
                    multiplier,
                    "GetDropItemList");
            }
        }

        if (additions.Count > 0)
            result.AddRange(additions);

        if (entriesChanged > 0 || additions.Count > 0)
            LootMultiplicatorLog.InfoDropTableScaled(entriesChanged, additions.Count, playerCount);
    }
}
