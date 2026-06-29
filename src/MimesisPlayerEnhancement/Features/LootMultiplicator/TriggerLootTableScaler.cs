using System.Collections.Generic;
using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class TriggerLootTableScaler
    {
        internal static void TrySpawnExtraItems(
            IVroom vroom,
            GameActionSpawnItem spawnAction,
            GameActionParamPosition positionParam)
        {
            if (!ShouldScale() || vroom == null || spawnAction == null || positionParam == null)
            {
                return;
            }

            if (spawnAction.Candidates == null || spawnAction.Candidates.Count == 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(vroom);
            float multiplier = LootTableRollHelper.GetRateWeightedMultiplier(
                LootSource.Trigger,
                ToRateEntries(spawnAction.Candidates),
                playerCount);

            int targetCount = LootMultiplierResolver.ScaleCount(1, multiplier);
            int extraNeeded = targetCount - 1;
            if (extraNeeded <= 0)
            {
                return;
            }

            LootSpawnScalingContext.BeginDuplicating();
            try
            {
                int spawned = 0;
                for (int i = 0; i < extraNeeded; i++)
                {
                    int masterId = spawnAction.PickItemMasterID();
                    if (masterId <= 0 || !LootItemFilter.IsEligible(masterId))
                    {
                        continue;
                    }

                    ItemElement? element = vroom.GetNewItemElement(masterId, isFake: false);
                    if (element == null)
                    {
                        continue;
                    }

                    if (vroom.SpawnLootingObject(
                        element,
                        positionParam.Position,
                        positionParam.IsIndoor,
                        ReasonOfSpawn.EventAction,
                        0,
                        0,
                        0L) == 0)
                    {
                        continue;
                    }

                    spawned++;
                }

                if (spawned > 0)
                {
                    ItemType itemType = ItemTypeLookup.GetItemType(spawnAction.Candidates[0].ItemMasterID);
                    LootMultiplicatorLog.InfoRuntimeScaled(
                        LootSource.Trigger,
                        itemType,
                        spawnAction.Candidates[0].ItemMasterID,
                        1,
                        1 + spawned,
                        multiplier,
                        "RunEventActionInternal/SPAWN_ITEM");
                }
            }
            finally
            {
                LootSpawnScalingContext.EndDuplicating();
            }
        }

        private static IEnumerable<KeyValuePair<int, int>> ToRateEntries(
            List<(int ItemMasterID, int Rate)> candidates)
        {
            foreach ((int itemMasterId, int rate) in candidates)
            {
                yield return new KeyValuePair<int, int>(itemMasterId, rate);
            }
        }

        private static bool ShouldScale()
        {
            return ModConfig.EnableLootMultiplicator.Value
                && !HostApplyGate.IsParticipantClient()
                && HostApplyGate.ShouldApplyHostOnlyFeature();
        }
    }
}
