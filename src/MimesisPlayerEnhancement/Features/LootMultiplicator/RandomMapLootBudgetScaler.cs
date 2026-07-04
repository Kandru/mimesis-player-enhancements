using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class RandomMapLootBudgetScaler
    {
        private const string Feature = "LootMultiplicator";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo DungeonMasterInfoField =
            typeof(DungeonRoom).GetField("_dungeonMasterInfo", InstanceFlags)
            ?? throw new System.InvalidOperationException("DungeonRoom._dungeonMasterInfo not found");

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
            float multiplier = LootTableRollHelper.GetRateWeightedMultiplier(
                LootSource.Map,
                dungeonInfo.SpawnableItemInfo.MiscRateDict,
                playerCount);

            if (multiplier <= 1f)
            {
                return vanillaBudget;
            }

            int scaled = LootMultiplierResolver.ScaleCount(vanillaBudget, multiplier);
            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Random map loot budget scaled {vanillaBudget} -> {scaled} ({multiplier:0.##}×, players={playerCount})");
            }

            return scaled;
        }
    }
}
