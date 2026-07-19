namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonPickLogic
    {
        internal static int Resolve(
            int vanillaResult,
            IReadOnlyList<int> excludeDungeonIds,
            DungeonRandomizerSceneConfig config,
            HashSet<int> allowlist,
            HashSet<int> blocklist,
            IReadOnlyList<int> activePool,
            Func<IReadOnlyList<int>, int?> pickFromPool)
        {
            DungeonPickPoolMode mode = DungeonIdListParser.ParsePoolMode(config.DungeonPickPoolMode);

            if (mode == DungeonPickPoolMode.WidenVanilla
                && IsEligible(vanillaResult, allowlist, blocklist)
                && !DungeonDataAccess.IsExcluded(vanillaResult, excludeDungeonIds))
            {
                return vanillaResult;
            }

            if (TryPickUniformFromActivePool(
                    excludeDungeonIds,
                    activePool,
                    pickFromPool,
                    out int pick))
            {
                return pick;
            }

            return vanillaResult;
        }

        internal static bool IsEligible(int dungeonId, HashSet<int> allowlist, HashSet<int> blocklist) =>
            dungeonId > 0
            && (allowlist.Count <= 0 || allowlist.Contains(dungeonId))
            && !blocklist.Contains(dungeonId);

        private static bool TryPickUniformFromActivePool(
            IReadOnlyList<int> excludeDungeonIds,
            IReadOnlyList<int> activePool,
            Func<IReadOnlyList<int>, int?> pickFromPool,
            out int pick)
        {
            pick = 0;
            List<int> pool = [.. activePool];
            List<int> eligiblePool = DungeonDataAccess.FilterExcluded(pool, excludeDungeonIds);
            if (eligiblePool.Count == 0 && excludeDungeonIds.Count > 0)
            {
                eligiblePool = pool;
            }

            int? picked = pickFromPool(eligiblePool);
            if (!picked.HasValue)
            {
                return false;
            }

            pick = picked.Value;
            return true;
        }
    }
}
