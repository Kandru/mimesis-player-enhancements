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

            if (!TryResolveDungeon(dungeonMasterId, vanillaSeed, out string flowId, out DungeonMasterInfo? info)
                || info == null)
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

            int startIndex = new GameMainBase.SyncRandom(vanillaSeed ^ unchecked((int)flavor)).Next(0, pool.Length);
            bool multiFlow = info.DungenCandidates.Count > 1;
            if (!multiFlow)
            {
                int curatedSeed = pool[startIndex];
                DungeonRandomizerLog.InfoSeedFlavorApplied(flavor, flowId, vanillaSeed, curatedSeed, pool.Length, skipped: 0);
                return curatedSeed;
            }

            int maxRate = info.MaxDungenRate;
            if (maxRate <= 0)
            {
                ModLog.Warn(Feature, $"Seed flavor '{flavor}' skipped — invalid MaxDungenRate for dungeon {dungeonMasterId}");
                return vanillaSeed;
            }

            int skipped = 0;
            for (int offset = 0; offset < pool.Length; offset++)
            {
                int index = (startIndex + offset) % pool.Length;
                int candidate = pool[index];
                string derivedFlowId = GetDerivedFlowId(info, maxRate, candidate);
                if (string.Equals(derivedFlowId, flowId, StringComparison.Ordinal))
                {
                    DungeonRandomizerLog.InfoSeedFlavorApplied(
                        flavor,
                        flowId,
                        vanillaSeed,
                        candidate,
                        pool.Length,
                        skipped);
                    return candidate;
                }

                DungeonRandomizerLog.DebugSeedCandidateSkipped(flowId, index, candidate, derivedFlowId);
                skipped++;
            }

            ModLog.Warn(
                Feature,
                $"Seed flavor '{flavor}' has no flow-consistent seed in pool for '{flowId}' — using vanilla seed");
            return vanillaSeed;
        }

        private static bool TryResolveDungeon(
            int dungeonMasterId,
            int vanillaSeed,
            out string flowId,
            out DungeonMasterInfo? info)
        {
            flowId = string.Empty;
            info = null;
            ExcelDataManager? excel = DungeonDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            info = excel.GetDungeonInfo(dungeonMasterId);
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

                return false;
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

        private static string GetDerivedFlowId(DungeonMasterInfo info, int maxRate, int seed)
        {
            int randVal = new GameMainBase.SyncRandom(seed).Next(0, maxRate);
            return info.GetRandomDungenName(randVal);
        }
    }
}
