namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonSeedFlavorResolver
    {
        private const string Feature = "DungeonRandomizer";

        internal static int ResolveSeed(int vanillaSeed, int dungeonMasterId)
        {
            DungeonSeedFlavor flavor = SceneScopedConfigGate.DungeonRandomizer.ParsedDungeonSeedFlavor;
            if (flavor == DungeonSeedFlavor.Vanilla)
            {
                return vanillaSeed;
            }

            if (!DungeonSeedFlowResolver.TryResolveFlowId(dungeonMasterId, vanillaSeed, out string flowId))
            {
                ModLog.Warn(Feature, $"Seed flavor '{flavor}' skipped — could not resolve flow for dungeon {dungeonMasterId}");
                return vanillaSeed;
            }

            ReadOnlySpan<int> pool = DungeonSeedPools.GetPool(flowId, flavor);
            if (pool.Length == 0)
            {
                ModLog.Warn(Feature, $"Seed flavor '{flavor}' has no pool for flow '{flowId}' — using vanilla seed");
                return vanillaSeed;
            }

            int index = new GameMainBase.SyncRandom(vanillaSeed ^ unchecked((int)flavor)).Next(0, pool.Length);
            int curatedSeed = pool[index];
            DungeonRandomizerLog.InfoSeedFlavorApplied(flavor, flowId, vanillaSeed, curatedSeed, pool.Length);
            return curatedSeed;
        }
    }
}
