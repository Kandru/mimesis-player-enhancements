namespace MimesisPlayerEnhancement.Features.UserInterface.SpectatorVoiceBalance
{
    internal enum SpectatorVoiceBalanceMode
    {
        Vanilla,
        SpeechDucking,
        StaticAttenuation,
    }

    internal enum SpectatorVoiceGroup
    {
        Priority,
        Other,
    }

    internal static class SpectatorVoiceBalanceResolver
    {
        internal const float SpeechContinuityThresholdSeconds = 0.2f;

        private static readonly string[] ValidModes =
        [
            nameof(SpectatorVoiceBalanceMode.Vanilla),
            nameof(SpectatorVoiceBalanceMode.SpeechDucking),
            nameof(SpectatorVoiceBalanceMode.StaticAttenuation),
        ];

        internal static bool TryParseMode(string? raw, out SpectatorVoiceBalanceMode mode)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                mode = SpectatorVoiceBalanceMode.Vanilla;
                return false;
            }

            foreach (string valid in ValidModes)
            {
                if (string.Equals(valid, raw, StringComparison.OrdinalIgnoreCase))
                {
                    mode = Enum.Parse<SpectatorVoiceBalanceMode>(valid);
                    return true;
                }
            }

            mode = SpectatorVoiceBalanceMode.Vanilla;
            return false;
        }

        internal static bool IsFeatureActive(bool isSpectatingDead) => isSpectatingDead;

        internal static SpectatorVoiceGroup ClassifyGroup(bool remoteIsDead) =>
            remoteIsDead ? SpectatorVoiceGroup.Priority : SpectatorVoiceGroup.Other;

        internal static float ResolveTargetMultiplier(
            SpectatorVoiceBalanceMode mode,
            SpectatorVoiceGroup group,
            bool priorityGroupSpeakingContinuously,
            float attenuation,
            float duckLevel)
        {
            if (mode == SpectatorVoiceBalanceMode.Vanilla || group == SpectatorVoiceGroup.Priority)
            {
                return 1f;
            }

            return mode switch
            {
                SpectatorVoiceBalanceMode.StaticAttenuation => Clamp01(attenuation),
                SpectatorVoiceBalanceMode.SpeechDucking =>
                    priorityGroupSpeakingContinuously ? Clamp01(duckLevel) : 1f,
                _ => 1f,
            };
        }

        private static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
