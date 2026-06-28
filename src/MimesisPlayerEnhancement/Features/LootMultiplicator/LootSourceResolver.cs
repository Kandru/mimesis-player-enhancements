using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class LootSourceResolver
{
    internal static bool ShouldScaleSpawn(ReasonOfSpawn reasonOfSpawn, bool isRestored) =>
        !isRestored && TryResolveLootSource(reasonOfSpawn, 0, out _);

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

        if (spawnPointIndex != 0)
        {
            source = LootSource.Map;
            return true;
        }

        if (reasonOfSpawn.Equals(ReasonOfSpawn.Spawn)
            || reasonOfSpawn.Equals(ReasonOfSpawn.ItemSpawn))
        {
            source = LootSource.Map;
            return true;
        }

        if (reasonOfSpawn.Equals(ReasonOfSpawn.ActorDying))
        {
            source = LootSource.Drop;
            return true;
        }

        if (reasonOfSpawn.Equals(ReasonOfSpawn.EventAction)
            || reasonOfSpawn.Equals(ReasonOfSpawn.Reinforce)
            || reasonOfSpawn.Equals(ReasonOfSpawn.Gamble)
            || reasonOfSpawn.Equals(ReasonOfSpawn.Linked))
        {
            source = LootSource.Trigger;
            return true;
        }

        source = default;
        return false;
    }
}
