using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootItemFilter
    {
        private static HashSet<int>? _cachedAllowlist;
        private static HashSet<int>? _cachedBlocklist;
        private static string _cachedAllowlistRaw = "";
        private static string _cachedBlocklistRaw = "";
        private static LootItemFilterMode _cachedMode = LootItemFilterMode.All;
        private static string _cachedModeRaw = "";

        internal static bool IsEligible(int masterId)
        {
            if (masterId <= 0)
            {
                return false;
            }

            GetFilters(out LootItemFilterMode mode, out HashSet<int> allowlist, out HashSet<int> blocklist);

            return mode switch
            {
                LootItemFilterMode.AllowlistOnly => allowlist.Contains(masterId),
                LootItemFilterMode.BlocklistOnly => !blocklist.Contains(masterId),
                _ => true,
            };
        }

        internal static void GetFilters(
            out LootItemFilterMode mode,
            out HashSet<int> allowlist,
            out HashSet<int> blocklist)
        {
            string modeRaw = ModConfig.LootItemFilterMode.Value ?? "";
            string allowRaw = ModConfig.LootAllowlist.Value ?? "";
            string blockRaw = ModConfig.LootBlocklist.Value ?? "";

            if (_cachedAllowlist == null || !string.Equals(_cachedAllowlistRaw, allowRaw, System.StringComparison.Ordinal))
            {
                _cachedAllowlistRaw = allowRaw;
                _cachedAllowlist = LootItemIdListParser.Parse(allowRaw);
            }

            if (_cachedBlocklist == null || !string.Equals(_cachedBlocklistRaw, blockRaw, System.StringComparison.Ordinal))
            {
                _cachedBlocklistRaw = blockRaw;
                _cachedBlocklist = LootItemIdListParser.Parse(blockRaw);
            }

            if (!string.Equals(_cachedModeRaw, modeRaw, System.StringComparison.Ordinal))
            {
                _cachedModeRaw = modeRaw;
                _cachedMode = LootItemIdListParser.ParseMode(modeRaw);
            }

            mode = _cachedMode;
            allowlist = _cachedAllowlist;
            blocklist = _cachedBlocklist;
        }
    }
}
