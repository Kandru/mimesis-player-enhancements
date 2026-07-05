using Bifrost.ConstEnum;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class RuntimeLootScaler
    {
        private const string Feature = "LootMultiplicator";

        internal static void ScaleSpawnedItem(
            IVroom? room,
            ItemElement element,
            ReasonOfSpawn reasonOfSpawn,
            int spawnPointIndex = 0,
            bool isRestored = false)
        {
            if (!LootScalingGate.ShouldScale() || element == null)
            {
                return;
            }

            if (!LootSourceResolver.ShouldScaleSpawn(reasonOfSpawn, isRestored)
                || !LootSourceResolver.TryResolveLootSource(reasonOfSpawn, spawnPointIndex, out LootSource source))
            {
                return;
            }

            int before = ItemElementStackHelper.GetStackCount(element);
            if (!LootItemCountScaler.TryScaleElementStack(room, element, source))
            {
                return;
            }

            int after = ItemElementStackHelper.GetStackCount(element);
            ItemType itemType = ItemElementStackHelper.GetItemType(element);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                source,
                itemType,
                SessionPlayerCountHelper.ResolveFromRoom(room),
                element.ItemMasterID);

            LootMultiplicatorLog.DebugLootScaled(
                source,
                itemType,
                element.ItemMasterID,
                before,
                after,
                multiplier,
                $"SpawnLootingObject/{reasonOfSpawn}");
        }

        internal static bool TryMapReasonToSource(ReasonOfSpawn reasonOfSpawn, out LootSource source)
        {
            return LootSourceResolver.TryResolveLootSource(reasonOfSpawn, 0, out source);
        }
    }
}
