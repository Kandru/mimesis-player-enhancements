using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning
{
    internal static class MimicVoiceTuningResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";
        private const float VanillaResponseCooldownSeconds = 3f;
        private const float VanillaResponseDelaySeconds = 0.2f;
        private const float VanillaResponseMaxDistance = 20f;
        private const float MinCooldownSeconds = 0f;
        private const float MaxCooldownSeconds = 120f;
        private const float MinDelaySeconds = 0f;
        private const float MaxDelaySeconds = 30f;
        private const float MinDistanceMeters = 1f;
        private const float MaxDistanceMeters = 200f;
        private const float MinIntervalMultiplier = 0.05f;
        private const float MaxIntervalMultiplier = 10f;

        private static bool _cachedMasterEnabled;
        private static MimicVoiceTuningMode _cachedMode = MimicVoiceTuningMode.Vanilla;
        private static float _cachedIntervalMultiplier = 1f;
        private static int _cachedResponseChancePercent = 100;
        private static float _cachedResponseCooldownSeconds = VanillaResponseCooldownSeconds;
        private static float _cachedResponseDelayMinSeconds = VanillaResponseDelaySeconds;
        private static float _cachedResponseDelayMaxSeconds = VanillaResponseDelaySeconds;
        private static float _cachedResponseMaxDistance = VanillaResponseMaxDistance;

        internal static bool IsMasterEnabled => _cachedMasterEnabled;

        internal static MimicVoiceTuningMode Mode => _cachedMode;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedMode == MimicVoiceTuningMode.Custom;

        internal static float GetResponseCooldownSeconds() => _cachedResponseCooldownSeconds;

        internal static float GetResponseMaxDistance() =>
            GetResponseMaxDistance(ShouldApplyCustom, _cachedResponseMaxDistance);

        internal static float GetResponseMaxDistance(bool shouldApplyCustom, float configuredDistance) =>
            shouldApplyCustom ? configuredDistance : VanillaResponseMaxDistance;

        internal static float ScaleIntervalSeconds(float vanillaSeconds) =>
            ScaleIntervalSeconds(vanillaSeconds, ShouldApplyCustom, _cachedIntervalMultiplier);

        internal static float ScaleIntervalSeconds(
            float vanillaSeconds,
            bool shouldApplyCustom,
            float intervalMultiplier)
        {
            if (!shouldApplyCustom || vanillaSeconds <= 0f)
            {
                return vanillaSeconds;
            }

            return Mathf.Max(0f, vanillaSeconds * intervalMultiplier);
        }

        internal static float RollResponseDelaySeconds() =>
            RollResponseDelaySeconds(
                ShouldApplyCustom,
                _cachedResponseDelayMinSeconds,
                _cachedResponseDelayMaxSeconds);

        internal static float RollResponseDelaySeconds(bool shouldApplyCustom, float minSeconds, float maxSeconds)
        {
            if (!shouldApplyCustom)
            {
                return VanillaResponseDelaySeconds;
            }

            return minSeconds >= maxSeconds ? minSeconds : UnityEngine.Random.Range(minSeconds, maxSeconds);
        }

        internal static bool RollResponseChance() =>
            RollResponseChance(ShouldApplyCustom, _cachedResponseChancePercent);

        internal static bool RollResponseChance(bool shouldApplyCustom, int chancePercent)
        {
            if (!shouldApplyCustom)
            {
                return true;
            }

            int chance = Mathf.Clamp(chancePercent, 0, 100);
            if (chance >= 100)
            {
                return true;
            }

            if (chance <= 0)
            {
                return false;
            }

            return UnityEngine.Random.Range(0, 100) < chance;
        }

        internal static MimicVoiceTuningMode ParseMode(string? value)
        {
            if (string.Equals(value, nameof(MimicVoiceTuningMode.Custom), StringComparison.OrdinalIgnoreCase))
            {
                return MimicVoiceTuningMode.Custom;
            }

            return MimicVoiceTuningMode.Vanilla;
        }

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null
                || ModConfig.MimicVoiceTuningMode == null
                || ModConfig.PeriodicVoiceIntervalMultiplier == null
                || ModConfig.PlayerVoiceResponseChancePercent == null
                || ModConfig.PlayerVoiceResponseCooldownSeconds == null
                || ModConfig.PlayerVoiceResponseDelayMinSeconds == null
                || ModConfig.PlayerVoiceResponseDelayMaxSeconds == null
                || ModConfig.PlayerVoiceResponseMaxDistance == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedMode = ParseMode(ModConfig.MimicVoiceTuningMode.Value);
            _cachedIntervalMultiplier = Mathf.Clamp(
                ModConfig.PeriodicVoiceIntervalMultiplier.Value,
                MinIntervalMultiplier,
                MaxIntervalMultiplier);
            _cachedResponseChancePercent = Mathf.Clamp(ModConfig.PlayerVoiceResponseChancePercent.Value, 0, 100);
            _cachedResponseCooldownSeconds = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseCooldownSeconds.Value,
                MinCooldownSeconds,
                MaxCooldownSeconds);
            _cachedResponseDelayMinSeconds = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseDelayMinSeconds.Value,
                MinDelaySeconds,
                MaxDelaySeconds);
            _cachedResponseDelayMaxSeconds = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseDelayMaxSeconds.Value,
                MinDelaySeconds,
                MaxDelaySeconds);
            if (_cachedResponseDelayMaxSeconds < _cachedResponseDelayMinSeconds)
            {
                (_cachedResponseDelayMinSeconds, _cachedResponseDelayMaxSeconds) =
                    (_cachedResponseDelayMaxSeconds, _cachedResponseDelayMinSeconds);
            }

            _cachedResponseMaxDistance = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseMaxDistance.Value,
                MinDistanceMeters,
                MaxDistanceMeters);
        }
    }
}
