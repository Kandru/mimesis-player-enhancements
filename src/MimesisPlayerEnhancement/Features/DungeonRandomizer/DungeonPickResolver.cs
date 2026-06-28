using System.Collections.Generic;

using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonPickResolver
{
    private static HashSet<int>? _cachedAllowlist;
    private static HashSet<int>? _cachedBlocklist;
    private static string _cachedAllowlistRaw = "";
    private static string _cachedBlocklistRaw = "";

    internal static bool ShouldIgnoreDungeonExcludeList()
    {
        if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableDungeonRandomizer.Value) || !ModConfig.RandomizeDungeonPick.Value)
            return false;

        return DungeonIdListParser.ParsePoolMode(ModConfig.DungeonPickPoolMode.Value) == DungeonPickPoolMode.WidenVanilla
               && ModConfig.IgnoreDungeonExcludeList.Value;
    }

    internal static int ResolvePick(int vanillaResult, IReadOnlyList<int> excludeDungeonIds)
    {
        GetFilters(out HashSet<int> allowlist, out HashSet<int> blocklist);
        DungeonPickPoolMode mode = DungeonIdListParser.ParsePoolMode(ModConfig.DungeonPickPoolMode.Value);

        if (mode == DungeonPickPoolMode.AllActiveUniform)
        {
            List<int> pool = DungeonDataAccess.GetFilteredActiveDungeonIds(allowlist, blocklist);
            List<int> eligiblePool = DungeonDataAccess.FilterExcluded(pool, excludeDungeonIds);
            if (eligiblePool.Count == 0 && excludeDungeonIds.Count > 0)
            {
                DungeonRandomizerLog.Warn(
                    "AllActiveUniform pool empty after tram excludes; falling back to full filtered pool.");
                eligiblePool = pool;
            }

            if (DungeonDataAccess.TryPickUniform(eligiblePool, out int uniformPick))
            {
                DungeonRandomizerLog.Debug(
                    $"Dungeon pick (AllActiveUniform): {uniformPick} from pool of {eligiblePool.Count}");
                return uniformPick;
            }

            DungeonRandomizerLog.Warn("AllActiveUniform pool empty after filters; keeping vanilla pick.");
            return vanillaResult;
        }

        if (IsEligible(vanillaResult, allowlist, blocklist)
            && !DungeonDataAccess.IsExcluded(vanillaResult, excludeDungeonIds))
        {
            DungeonRandomizerLog.Debug($"Dungeon pick (WidenVanilla): keeping vanilla result {vanillaResult}");
            return vanillaResult;
        }

        List<int> fallbackPool = DungeonDataAccess.GetFilteredActiveDungeonIds(allowlist, blocklist);
        List<int> eligibleFallback = DungeonDataAccess.FilterExcluded(fallbackPool, excludeDungeonIds);
        if (eligibleFallback.Count == 0 && excludeDungeonIds.Count > 0)
        {
            DungeonRandomizerLog.Warn(
                "WidenVanilla fallback pool empty after tram excludes; falling back to full filtered pool.");
            eligibleFallback = fallbackPool;
        }

        if (DungeonDataAccess.TryPickUniform(eligibleFallback, out int fallbackPick))
        {
            DungeonRandomizerLog.Debug(
                $"Dungeon pick (WidenVanilla): vanilla {vanillaResult} filtered out; fallback {fallbackPick}");
            return fallbackPick;
        }

        DungeonRandomizerLog.Warn($"Dungeon pick fallback pool empty; keeping vanilla result {vanillaResult}");
        return vanillaResult;
    }

    private static bool IsEligible(int dungeonId, HashSet<int> allowlist, HashSet<int> blocklist)
    {
        if (dungeonId <= 0)
            return false;

        if (allowlist.Count > 0 && !allowlist.Contains(dungeonId))
            return false;

        return !blocklist.Contains(dungeonId);
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
