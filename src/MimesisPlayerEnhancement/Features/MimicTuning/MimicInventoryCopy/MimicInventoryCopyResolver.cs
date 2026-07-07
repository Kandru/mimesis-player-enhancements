namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicInventoryCopy
{
    internal static class MimicInventoryCopyResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        private static bool _cachedMasterEnabled;
        private static MimicInventoryCopyMode _cachedMode = MimicInventoryCopyMode.Vanilla;
        private static BTTargetPickRule _cachedPickRule = BTTargetPickRule.MinDistance;

        static MimicInventoryCopyResolver()
        {
            ModConfig.Changed += OnConfigChanged;
        }

        internal static bool IsMasterEnabled => _cachedMasterEnabled;

        internal static MimicInventoryCopyMode Mode => _cachedMode;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedMode == MimicInventoryCopyMode.Custom;

        internal static BTTargetPickRule PickRule => _cachedPickRule;

        internal static MimicInventoryCopyMode ParseMode(string? value)
        {
            if (string.Equals(value, nameof(MimicInventoryCopyMode.Custom), StringComparison.OrdinalIgnoreCase))
            {
                return MimicInventoryCopyMode.Custom;
            }

            return MimicInventoryCopyMode.Vanilla;
        }

        internal static BTTargetPickRule ParsePickRule(string? value)
        {
            if (string.Equals(value, nameof(BTTargetPickRule.MaxDistance), StringComparison.OrdinalIgnoreCase))
            {
                return BTTargetPickRule.MaxDistance;
            }

            if (string.Equals(value, nameof(BTTargetPickRule.Random), StringComparison.OrdinalIgnoreCase))
            {
                return BTTargetPickRule.Random;
            }

            return BTTargetPickRule.MinDistance;
        }

        internal static void RefreshFromConfigRegistration() => RefreshConfigCache();

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null
                || ModConfig.MimicInventoryCopyMode == null
                || ModConfig.MimicInventoryCopyPickRule == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedMode = ParseMode(ModConfig.MimicInventoryCopyMode.Value);
            _cachedPickRule = ParsePickRule(ModConfig.MimicInventoryCopyPickRule.Value);
        }

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            if (change.IsFullReload || change.AffectsSection(SectionId))
            {
                RefreshConfigCache();
            }
        }
    }
}
