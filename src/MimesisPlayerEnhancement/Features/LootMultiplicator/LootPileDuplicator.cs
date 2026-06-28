using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class LootPileDuplicator
{
    internal static void TrySpawnExtraPiles(
        IVroom vroom,
        ItemElement template,
        PosWithRot pos,
        bool isIndoor,
        ReasonOfSpawn reasonOfSpawn,
        int spawnPointIndex,
        int prevProjectileActorId,
        long projectileDropTime,
        bool ignoreNav,
        bool isRestored)
    {
        if (!ModConfig.EnableLootMultiplicator.Value
            || template == null
            || isRestored
            || LootSpawnScalingContext.IsDuplicating)
        {
            return;
        }

        if (HostApplyGate.IsParticipantClient() || !HostApplyGate.ShouldApplyHostOnlyFeature())
            return;

        if (!LootSourceResolver.ShouldScaleSpawn(reasonOfSpawn, isRestored)
            || !LootSourceResolver.TryResolveLootSource(reasonOfSpawn, spawnPointIndex, out LootSource source))
        {
            return;
        }

        if (source.Equals(LootSource.Map))
        {
            // Fixed loot extras are activated on unused markers at room init; random pools spread here.
            if (!MapLootSpawnContext.IsActive)
            {
                MapLootMarkerSpreader.TrySpreadToUnusedMarkers(
                    vroom,
                    template,
                    reasonOfSpawn,
                    prevProjectileActorId,
                    projectileDropTime,
                    ignoreNav,
                    isRestored);
            }

            return;
        }

        ItemType itemType = ItemElementStackHelper.GetItemType(template);
        if (itemType.Equals(ItemType.Consumable))
            return;

        int playerCount = SessionPlayerCountHelper.ResolveFromRoom(vroom);
        float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(source, itemType, playerCount);
        int targetPiles = LootMultiplierResolver.ScaleCount(1, multiplier);
        int extraPiles = targetPiles - 1;
        if (extraPiles <= 0)
            return;

        LootSpawnScalingContext.BeginDuplicating();
        try
        {
            for (int i = 0; i < extraPiles; i++)
            {
                ItemElement? copy = CreateSpawnCopy(vroom, template);
                if (copy == null)
                    continue;

                vroom.SpawnLootingObject(
                    copy,
                    pos,
                    isIndoor,
                    reasonOfSpawn,
                    spawnPointIndex,
                    prevProjectileActorId,
                    projectileDropTime,
                    ignoreNav);
            }
        }
        finally
        {
            LootSpawnScalingContext.EndDuplicating();
        }

        LootMultiplicatorLog.InfoRuntimeScaled(
            source,
            itemType,
            template.ItemMasterID,
            1,
            targetPiles,
            multiplier,
            $"SpawnLootingObject/{reasonOfSpawn}/piles");
    }

    private static ItemElement? CreateSpawnCopy(IVroom vroom, ItemElement template)
    {
        try
        {
            ItemInfo info = template.toItemInfo();
            return vroom.GetNewItemElement(
                info.itemMasterID,
                info.isFake,
                1,
                info.durability,
                info.remainGauge,
                info.price);
        }
        catch
        {
            return null;
        }
    }
}
