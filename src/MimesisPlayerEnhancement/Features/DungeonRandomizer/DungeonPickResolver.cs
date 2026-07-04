namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonPickResolver
    {
        private const string Feature = "DungeonRandomizer";

        private static HashSet<int>? _cachedAllowlist;
        private static HashSet<int>? _cachedBlocklist;
        private static string _cachedAllowlistRaw = "";
        private static string _cachedBlocklistRaw = "";

        internal static int ResolvePick(int vanillaResult, IReadOnlyList<int> excludeDungeonIds)
        {
            GetFilters(out HashSet<int> allowlist, out HashSet<int> blocklist);
            DungeonPickPoolMode mode = DungeonIdListParser.ParsePoolMode(ModConfig.DungeonPickPoolMode.Value);

            if (mode == DungeonPickPoolMode.WidenVanilla
                && IsEligible(vanillaResult, allowlist, blocklist)
                && !DungeonDataAccess.IsExcluded(vanillaResult, excludeDungeonIds))
            {
                ModLog.Debug(Feature, $"Dungeon pick (WidenVanilla): keeping vanilla result {vanillaResult}");
                return vanillaResult;
            }

            if (TryPickUniformFromActivePool(vanillaResult, allowlist, blocklist, excludeDungeonIds, mode, out int pick))
            {
                return pick;
            }

            ModLog.Warn(Feature, $"Dungeon pick pool empty after filters; keeping vanilla result {vanillaResult}");
            return vanillaResult;
        }

        private static bool TryPickUniformFromActivePool(
            int vanillaResult,
            HashSet<int> allowlist,
            HashSet<int> blocklist,
            IReadOnlyList<int> excludeDungeonIds,
            DungeonPickPoolMode mode,
            out int pick)
        {
            pick = 0;
            List<int> pool = DungeonDataAccess.GetFilteredActiveDungeonIds(allowlist, blocklist);
            List<int> eligiblePool = DungeonDataAccess.FilterExcluded(pool, excludeDungeonIds);
            if (eligiblePool.Count == 0 && excludeDungeonIds.Count > 0)
            {
                ModLog.Warn(Feature,
                    $"{mode} pool empty after tram excludes; falling back to full filtered pool.");
                eligiblePool = pool;
            }

            if (!DungeonDataAccess.TryPickUniform(eligiblePool, out pick))
            {
                return false;
            }

            DungeonRandomizerLog.InfoDungeonPick(vanillaResult, pick, mode, eligiblePool.Count);
            return true;
        }

        private static bool IsEligible(int dungeonId, HashSet<int> allowlist, HashSet<int> blocklist)
        {
            return dungeonId > 0
                   && (allowlist.Count <= 0 || allowlist.Contains(dungeonId))
                   && !blocklist.Contains(dungeonId);
        }

        private static void GetFilters(out HashSet<int> allowlist, out HashSet<int> blocklist)
        {
            string allowRaw = ModConfig.DungeonAllowlist.Value ?? "";
            string blockRaw = ModConfig.DungeonBlocklist.Value ?? "";

            if (_cachedAllowlist == null || !string.Equals(_cachedAllowlistRaw, allowRaw, System.StringComparison.Ordinal))
            {
                _cachedAllowlistRaw = allowRaw;
                _cachedAllowlist = DungeonIdListParser.Parse(allowRaw);
            }

            if (_cachedBlocklist == null || !string.Equals(_cachedBlocklistRaw, blockRaw, System.StringComparison.Ordinal))
            {
                _cachedBlocklistRaw = blockRaw;
                _cachedBlocklist = DungeonIdListParser.Parse(blockRaw);
            }

            allowlist = _cachedAllowlist;
            blocklist = _cachedBlocklist;
        }
    }
}
