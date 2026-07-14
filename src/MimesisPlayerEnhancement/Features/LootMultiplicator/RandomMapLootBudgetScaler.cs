using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class RandomMapLootBudgetScaler
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo DungeonMasterInfoField =
            typeof(DungeonRoom).GetField("_dungeonMasterInfo", InstanceFlags)
            ?? throw new InvalidOperationException("DungeonRoom._dungeonMasterInfo not found");

        internal static int RollScaledBudget(DungeonRoom room, int minVal, int maxVal)
        {
            int vanillaBudget = SimpleRandUtil.Next(minVal, maxVal);
            if (!LootScalingGate.ShouldScale(room))
            {
                return vanillaBudget;
            }

            if (DungeonMasterInfoField.GetValue(room) is not DungeonMasterInfo dungeonInfo
                || dungeonInfo.SpawnableItemInfo == null
                || dungeonInfo.SpawnableItemInfo.MiscRateDict.Count == 0)
            {
                return vanillaBudget;
            }

            int playerCount = room.GetMemberCount();
            float mapMultiplier = LootTableRollHelper.GetRateWeightedMultiplier(
                LootSource.Map,
                dungeonInfo.SpawnableItemInfo.MiscRateDict,
                playerCount);

            float filterRatio = MapLootBudgetPriceHelper.GetFilterPriceRatio(room, dungeonInfo);
            float combined = mapMultiplier * filterRatio;
            if (combined <= 1f)
            {
                return vanillaBudget;
            }

            int scaled = ScalingMath.ScaleCount(vanillaBudget, combined);
            LootMultiplicatorLog.DebugRandomPoolBudget(
                vanillaBudget,
                scaled,
                mapMultiplier,
                filterRatio,
                playerCount);
            return scaled;
        }
    }
}
