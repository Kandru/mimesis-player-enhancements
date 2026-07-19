namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonPickResolver
    {
        private const string Feature = "DungeonRandomizer";

        private static HashSet<int>? _cachedAllowlist;
        private static HashSet<int>? _cachedBlocklist;
        private static string _cachedAllowlistRaw = "";
        private static string _cachedBlocklistRaw = "";

        internal static void ClearFilterCache()
        {
            _cachedAllowlist = null;
            _cachedBlocklist = null;
            _cachedAllowlistRaw = "";
            _cachedBlocklistRaw = "";
        }

        internal static int ResolvePick(int vanillaResult, IReadOnlyList<int> excludeDungeonIds)
        {
            DungeonRandomizerSceneConfig config = SceneScopedConfigGate.DungeonRandomizer;
            GetFilters(config, out HashSet<int> allowlist, out HashSet<int> blocklist);
            IReadOnlyList<int> activePool = DungeonDataAccess.GetFilteredActiveDungeonIds(allowlist, blocklist);
            DungeonPickPoolMode mode = DungeonIdListParser.ParsePoolMode(config.DungeonPickPoolMode);

            if (mode == DungeonPickPoolMode.WidenVanilla
                && DungeonPickLogic.IsEligible(vanillaResult, allowlist, blocklist)
                && !DungeonDataAccess.IsExcluded(vanillaResult, excludeDungeonIds))
            {
                ModLog.Debug(Feature, $"Dungeon pick (WidenVanilla): keeping vanilla result {vanillaResult}");
                return vanillaResult;
            }

            int result = DungeonPickLogic.Resolve(
                vanillaResult,
                excludeDungeonIds,
                config,
                allowlist,
                blocklist,
                activePool,
                TryPickFromPool);

            if (result != vanillaResult)
            {
                DungeonRandomizerLog.InfoDungeonPick(vanillaResult, result, mode, activePool.Count);
            }
            else if (mode != DungeonPickPoolMode.WidenVanilla || activePool.Count == 0)
            {
                ModLog.Warn(Feature, $"Dungeon pick pool empty after filters; keeping vanilla result {vanillaResult}");
            }

            return result;
        }

        private static int? TryPickFromPool(IReadOnlyList<int> pool) =>
            DungeonDataAccess.TryPickUniform(pool, out int pick) ? pick : null;

        private static void GetFilters(
            DungeonRandomizerSceneConfig config,
            out HashSet<int> allowlist,
            out HashSet<int> blocklist)
        {
            string allowRaw = config.DungeonAllowlist;
            string blockRaw = config.DungeonBlocklist;

            if (_cachedAllowlist == null || !string.Equals(_cachedAllowlistRaw, allowRaw, StringComparison.Ordinal))
            {
                _cachedAllowlistRaw = allowRaw;
                _cachedAllowlist = DungeonIdListParser.Parse(allowRaw);
            }

            if (_cachedBlocklist == null || !string.Equals(_cachedBlocklistRaw, blockRaw, StringComparison.Ordinal))
            {
                _cachedBlocklistRaw = blockRaw;
                _cachedBlocklist = DungeonIdListParser.Parse(blockRaw);
            }

            allowlist = _cachedAllowlist;
            blocklist = _cachedBlocklist;
        }
    }
}
