namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonSeedFlavorResolver
    {
        private const string Feature = "DungeonRandomizer";

        internal static int ResolveSeed(int vanillaSeed, int dungeonMasterId)
        {
            DungeonSeedFlavor flavor = SceneScopedConfigGate.DungeonRandomizer.SeedFlavor;
            if (flavor == DungeonSeedFlavor.Vanilla)
            {
                return vanillaSeed;
            }

            if (!TryResolveFlowId(dungeonMasterId, vanillaSeed, out string flowId))
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

        private static bool TryResolveFlowId(int dungeonMasterId, int vanillaSeed, out string flowId)
        {
            flowId = string.Empty;
            ExcelDataManager? excel = DungeonDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            DungeonMasterInfo? info = excel.GetDungeonInfo(dungeonMasterId);
            if (info == null || info.DungenCandidates.Count == 0)
            {
                return false;
            }

            if (info.DungenCandidates.Count == 1)
            {
                foreach (KeyValuePair<string, int> candidate in info.DungenCandidates)
                {
                    flowId = candidate.Key;
                    return !string.IsNullOrEmpty(flowId);
                }
            }

            int maxRate = info.MaxDungenRate;
            if (maxRate <= 0)
            {
                return false;
            }

            int randVal = new GameMainBase.SyncRandom(vanillaSeed).Next(0, maxRate);
            flowId = info.GetRandomDungenName(randVal);
            return !string.IsNullOrEmpty(flowId);
        }
    }
}
