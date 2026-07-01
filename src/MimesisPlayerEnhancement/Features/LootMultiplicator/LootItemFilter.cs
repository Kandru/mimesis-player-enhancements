using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootItemFilter
    {
        private static HashSet<int> _cachedAllowlist = [];
        private static HashSet<int> _cachedBlocklist = [];
        private static LootItemFilterMode _cachedMode = LootItemFilterMode.All;

        static LootItemFilter()
        {
            ModConfig.Changed += OnConfigChanged;
            ReloadFromConfig();
        }

        internal static bool IsEligible(int masterId)
        {
            if (masterId <= 0)
            {
                return false;
            }

            return _cachedMode switch
            {
                LootItemFilterMode.AllowlistOnly => _cachedAllowlist.Contains(masterId),
                LootItemFilterMode.BlocklistOnly => !_cachedBlocklist.Contains(masterId),
                _ => true,
            };
        }

        internal static void GetFilters(
            out LootItemFilterMode mode,
            out HashSet<int> allowlist,
            out HashSet<int> blocklist)
        {
            mode = _cachedMode;
            allowlist = _cachedAllowlist;
            blocklist = _cachedBlocklist;
        }

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            if (change.IsFullReload
                || change.AffectsSection("MimesisPlayerEnhancement_LootMultiplicator"))
            {
                ReloadFromConfig();
            }
        }

        private static void ReloadFromConfig()
        {
            _cachedAllowlist = LootItemIdListParser.Parse(ModConfig.LootAllowlist.Value ?? "");
            _cachedBlocklist = LootItemIdListParser.Parse(ModConfig.LootBlocklist.Value ?? "");
            _cachedMode = LootItemIdListParser.ParseMode(ModConfig.LootItemFilterMode.Value ?? "");
        }
    }
}
