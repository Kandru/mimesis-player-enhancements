using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicSocial
{
    internal static class MimicSocialResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        internal const float VanillaRunawayChance = 0.5f;
        internal const float VanillaJumpCopyChance = 0.8f;
        internal const float VanillaSlotFollowChangeChance = 0.8f;

        private static bool _cachedMasterEnabled;
        private static bool _cachedCustom;
        private static float _cachedRunawayChance = VanillaRunawayChance;
        private static float _cachedJumpCopyChance = VanillaJumpCopyChance;
        private static float _cachedSlotFollowChangeChance = VanillaSlotFollowChangeChance;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedCustom;

        internal static float RunawayChance => _cachedRunawayChance;
        internal static float JumpCopyChance => _cachedJumpCopyChance;
        internal static float SlotFollowChangeChance => _cachedSlotFollowChangeChance;

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null || ModConfig.MimicSocialMode == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedCustom = string.Equals(
                ModConfig.MimicSocialMode.Value,
                "Custom",
                StringComparison.OrdinalIgnoreCase);
            _cachedRunawayChance = Mathf.Clamp(
                ModConfig.MimicRunawayChance?.Value ?? VanillaRunawayChance,
                0f,
                1f);
            _cachedJumpCopyChance = MimicTuningModeHelpers.PercentToProbability(
                ModConfig.JumpCopyChancePercent?.Value ?? (int)(VanillaJumpCopyChance * 100f));
            _cachedSlotFollowChangeChance = MimicTuningModeHelpers.PercentToProbability(
                ModConfig.SlotFollowChangeChancePercent?.Value ?? (int)(VanillaSlotFollowChangeChance * 100f));
        }
    }
}
