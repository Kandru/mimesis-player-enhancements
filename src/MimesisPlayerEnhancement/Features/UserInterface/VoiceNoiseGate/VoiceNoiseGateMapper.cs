using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.VoiceNoiseGate
{
    internal readonly struct VoiceNoiseGateTargets
    {
        internal VoiceNoiseGateTargets(string vadSensitivityLevelName, string denoiseLevelName)
        {
            VadSensitivityLevelName = vadSensitivityLevelName;
            DenoiseLevelName = denoiseLevelName;
        }

        internal string VadSensitivityLevelName { get; }
        internal string DenoiseLevelName { get; }
    }

    internal static class VoiceNoiseGateMapper
    {
        internal const float DefaultStrength = 0.5f;
        internal const float StrengthThreshold = 0.5f;

        internal static VoiceNoiseGateTargets MapStrength(float strength)
        {
            float clamped = Mathf.Clamp01(strength);
            if (clamped < StrengthThreshold)
            {
                return new VoiceNoiseGateTargets("MediumSensitivity", "High");
            }

            return new VoiceNoiseGateTargets("LowSensitivity", "VeryHigh");
        }

        internal static bool IsVoiceActivationTalkMode(string? modeName)
        {
            return string.Equals(modeName, "VoiceActivation", StringComparison.Ordinal)
                || string.Equals(modeName, "Open", StringComparison.Ordinal);
        }
    }
}
