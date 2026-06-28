using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class RuntimeLootScaler
{
    internal static void ScaleSpawnedItem(
        IVroom? room,
        ItemElement element,
        ReasonOfSpawn reasonOfSpawn,
        int spawnPointIndex = 0)
    {
        if (!ModConfig.EnableLootMultiplicator.Value || element == null)
            return;

        if (SpawnScalingHost.IsParticipantClient() || !SpawnScalingHost.ShouldApplyScaling())
            return;

        if (!TryResolveLootSource(reasonOfSpawn, spawnPointIndex, out LootSource source))
            return;

        ItemType itemType = ItemElementStackHelper.GetItemType(element);
        int playerCount = LootPlayerCountHelper.ResolvePlayerCount(room);
        float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(source, itemType, playerCount);

        if (multiplier <= 1f)
            return;

        int before = ItemElementStackHelper.GetStackCount(element);
        int baseCount = before > 0 ? before : 1;
        int after = LootMultiplierResolver.ScaleCount(baseCount, multiplier);
        if (after == before)
            return;

        ItemElementStackHelper.SetStackCount(element, after);
        LootMultiplicatorLog.InfoRuntimeScaled(
            source,
            itemType,
            element.ItemMasterID,
            before,
            after,
            multiplier,
            $"SpawnLootingObject/{reasonOfSpawn}");
        LootMultiplicatorLog.DebugLootScaled(
            source,
            itemType,
            element.ItemMasterID,
            before,
            after,
            multiplier,
            $"SpawnLootingObject/{reasonOfSpawn}");
    }

    internal static bool TryMapReasonToSource(ReasonOfSpawn reasonOfSpawn, out LootSource source) =>
        TryResolveLootSource(reasonOfSpawn, 0, out source);

    internal static bool TryResolveLootSource(
        ReasonOfSpawn reasonOfSpawn,
        int spawnPointIndex,
        out LootSource source)
    {
        if (MapLootSpawnContext.IsActive)
        {
            source = LootSource.Map;
            return true;
        }

        if (reasonOfSpawn.Equals(ReasonOfSpawn.Spawn))
        {
            source = LootSource.Map;
            return true;
        }

        if (reasonOfSpawn.Equals(ReasonOfSpawn.ActorDying))
        {
            source = LootSource.Drop;
            return true;
        }

        if (reasonOfSpawn.Equals(ReasonOfSpawn.EventAction))
        {
            source = LootSource.Trigger;
            return true;
        }

        source = default;
        return false;
    }
}
