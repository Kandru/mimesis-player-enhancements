using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicTrust
{
    internal static class MimicTrustResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        internal const float VanillaOutdoorMultiplier = 2f;
        internal const float VanillaLookingDelta = -3f;
        internal const float VanillaNotLookingDelta = 3f;
        internal const float VanillaApproachDelta = 0.5f;
        internal const float VanillaMaintainDelta = 0f;
        internal const float VanillaWalkAwayDelta = 5f;
        internal const float VanillaSprintAwayDelta = -11f;
        internal const float VanillaHitDamageMultiplier = -0.5f;
        internal const float VanillaFriendlyThreshold = 90f;
        internal const float VanillaDistrustThreshold = 10f;
        internal const float VanillaInitialTrust = 50f;
        internal const float VanillaBehaviorTrust = 70f;
        internal const float VanillaChaseActivationDistance = 8f;
        internal const float VanillaChaseForceRunDistance = 10f;

        private static bool _cachedMasterEnabled;
        private static bool _cachedCustom;
        private static MimicTuningValueMode _cachedScoreMode = MimicTuningValueMode.Vanilla;
        private static float _cachedOutdoorMultiplier = VanillaOutdoorMultiplier;
        private static float _cachedLookingDelta = VanillaLookingDelta;
        private static float _cachedNotLookingDelta = VanillaNotLookingDelta;
        private static float _cachedApproachDelta = VanillaApproachDelta;
        private static float _cachedMaintainDelta = VanillaMaintainDelta;
        private static float _cachedWalkAwayDelta = VanillaWalkAwayDelta;
        private static float _cachedSprintAwayDelta = VanillaSprintAwayDelta;
        private static float _cachedHitDamageMultiplier = VanillaHitDamageMultiplier;
        private static float _cachedFriendlyThreshold = VanillaFriendlyThreshold;
        private static float _cachedDistrustThreshold = VanillaDistrustThreshold;
        private static float _cachedInitialFixed = VanillaInitialTrust;
        private static float _cachedInitialRandomMin = VanillaInitialTrust;
        private static float _cachedInitialRandomMax = VanillaInitialTrust;
        private static float _cachedBehaviorFixed = VanillaBehaviorTrust;
        private static float _cachedBehaviorRandomMin = VanillaBehaviorTrust;
        private static float _cachedBehaviorRandomMax = VanillaBehaviorTrust;
        private static float _cachedChaseActivationDistance = VanillaChaseActivationDistance;
        private static float _cachedChaseForceRunDistance = VanillaChaseForceRunDistance;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedCustom;

        internal static float OutdoorMultiplier => _cachedOutdoorMultiplier;
        internal static float LookingDelta => _cachedLookingDelta;
        internal static float NotLookingDelta => _cachedNotLookingDelta;
        internal static float ApproachDelta => _cachedApproachDelta;
        internal static float MaintainDelta => _cachedMaintainDelta;
        internal static float WalkAwayDelta => _cachedWalkAwayDelta;
        internal static float SprintAwayDelta => _cachedSprintAwayDelta;
        internal static float HitDamageMultiplier => _cachedHitDamageMultiplier;
        internal static float FriendlyThreshold => _cachedFriendlyThreshold;
        internal static float DistrustThreshold => _cachedDistrustThreshold;
        internal static float ChaseActivationDistance => _cachedChaseActivationDistance;
        internal static float ChaseForceRunDistance => _cachedChaseForceRunDistance;

        internal static float ResolveInitialTrust(float vanillaValue) =>
            MimicTuningModeHelpers.ResolveTrustScore(
                _cachedScoreMode,
                vanillaValue,
                _cachedInitialFixed,
                _cachedInitialRandomMin,
                _cachedInitialRandomMax);

        internal static float ResolveBehaviorTrust(float vanillaValue) =>
            MimicTuningModeHelpers.ResolveTrustScore(
                _cachedScoreMode,
                vanillaValue,
                _cachedBehaviorFixed,
                _cachedBehaviorRandomMin,
                _cachedBehaviorRandomMax);

        internal static bool IsCustomScoreMode =>
            ShouldApplyCustom && _cachedScoreMode != MimicTuningValueMode.Vanilla;

        internal static MimicTuningValueMode ScoreMode => _cachedScoreMode;

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null || ModConfig.MimicTrustMode == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedCustom = string.Equals(
                ModConfig.MimicTrustMode.Value,
                "Custom",
                StringComparison.OrdinalIgnoreCase);
            _cachedScoreMode = MimicTuningModeHelpers.ParseValueMode(ModConfig.TrustScoreValueMode?.Value);
            _cachedOutdoorMultiplier = ReadFloat(ModConfig.TrustOutdoorMultiplier, VanillaOutdoorMultiplier, 0f, 10f);
            _cachedLookingDelta = ReadFloat(ModConfig.TrustLookingDelta, VanillaLookingDelta, -100f, 100f);
            _cachedNotLookingDelta = ReadFloat(ModConfig.TrustNotLookingDelta, VanillaNotLookingDelta, -100f, 100f);
            _cachedApproachDelta = ReadFloat(ModConfig.TrustApproachDelta, VanillaApproachDelta, -100f, 100f);
            _cachedMaintainDelta = ReadFloat(ModConfig.TrustMaintainDelta, VanillaMaintainDelta, -100f, 100f);
            _cachedWalkAwayDelta = ReadFloat(ModConfig.TrustWalkAwayDelta, VanillaWalkAwayDelta, -100f, 100f);
            _cachedSprintAwayDelta = ReadFloat(ModConfig.TrustSprintAwayDelta, VanillaSprintAwayDelta, -100f, 100f);
            _cachedHitDamageMultiplier = ReadFloat(ModConfig.TrustHitDamageMultiplier, VanillaHitDamageMultiplier, -100f, 100f);
            _cachedFriendlyThreshold = ReadFloat(ModConfig.TrustFriendlyThreshold, VanillaFriendlyThreshold, 0f, 100f);
            _cachedDistrustThreshold = ReadFloat(ModConfig.TrustDistrustThreshold, VanillaDistrustThreshold, 0f, 100f);
            _cachedInitialFixed = ReadFloat(ModConfig.TrustInitialFixed, VanillaInitialTrust, 0f, 100f);
            _cachedInitialRandomMin = ReadFloat(ModConfig.TrustInitialRandomMin, VanillaInitialTrust, 0f, 100f);
            _cachedInitialRandomMax = ReadFloat(ModConfig.TrustInitialRandomMax, VanillaInitialTrust, 0f, 100f);
            _cachedBehaviorFixed = ReadFloat(ModConfig.TrustBehaviorFixed, VanillaBehaviorTrust, 0f, 100f);
            _cachedBehaviorRandomMin = ReadFloat(ModConfig.TrustBehaviorRandomMin, VanillaBehaviorTrust, 0f, 100f);
            _cachedBehaviorRandomMax = ReadFloat(ModConfig.TrustBehaviorRandomMax, VanillaBehaviorTrust, 0f, 100f);
            _cachedChaseActivationDistance = ReadFloat(
                ModConfig.ChaseActivationDistanceMeters,
                VanillaChaseActivationDistance,
                0.1f,
                200f);
            _cachedChaseForceRunDistance = ReadFloat(
                ModConfig.ChaseForceRunDistanceMeters,
                VanillaChaseForceRunDistance,
                0.1f,
                200f);

            if (_cachedInitialRandomMax < _cachedInitialRandomMin)
            {
                (_cachedInitialRandomMin, _cachedInitialRandomMax) =
                    (_cachedInitialRandomMax, _cachedInitialRandomMin);
            }

            if (_cachedBehaviorRandomMax < _cachedBehaviorRandomMin)
            {
                (_cachedBehaviorRandomMin, _cachedBehaviorRandomMax) =
                    (_cachedBehaviorRandomMax, _cachedBehaviorRandomMin);
            }
        }

        private static float ReadFloat(
            MelonLoader.MelonPreferences_Entry<float>? entry,
            float fallback,
            float min,
            float max)
        {
            if (entry == null)
            {
                return fallback;
            }

            return Mathf.Clamp(entry.Value, min, max);
        }
    }
}
