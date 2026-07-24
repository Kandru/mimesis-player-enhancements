using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal enum MimicTuningValueMode
    {
        Vanilla,
        Fixed,
        Random,
    }

    internal enum MimicTuningIntervalMode
    {
        Vanilla,
        Random,
    }

    internal enum MimicTuningPostReplyIntervalMode
    {
        Vanilla,
        Fixed,
        Random,
    }

    internal enum HearOwnVoiceFromMimicMode
    {
        Vanilla,
        AlwaysHear,
        OnlyWhenSingleplayer,
    }

    internal enum PossessionBtGateMode
    {
        Vanilla,
        Always,
    }

    internal static class MimicTuningModeHelpers
    {
        internal static MimicTuningValueMode ParseValueMode(string? value)
        {
            if (string.Equals(value, nameof(MimicTuningValueMode.Fixed), StringComparison.OrdinalIgnoreCase))
            {
                return MimicTuningValueMode.Fixed;
            }

            if (string.Equals(value, nameof(MimicTuningValueMode.Random), StringComparison.OrdinalIgnoreCase))
            {
                return MimicTuningValueMode.Random;
            }

            return MimicTuningValueMode.Vanilla;
        }

        internal static MimicTuningIntervalMode ParseIntervalMode(string? value)
        {
            if (string.Equals(value, nameof(MimicTuningIntervalMode.Random), StringComparison.OrdinalIgnoreCase))
            {
                return MimicTuningIntervalMode.Random;
            }

            return MimicTuningIntervalMode.Vanilla;
        }

        internal static MimicTuningPostReplyIntervalMode ParsePostReplyIntervalMode(string? value)
        {
            if (string.Equals(value, nameof(MimicTuningPostReplyIntervalMode.Fixed), StringComparison.OrdinalIgnoreCase))
            {
                return MimicTuningPostReplyIntervalMode.Fixed;
            }

            if (string.Equals(value, nameof(MimicTuningPostReplyIntervalMode.Random), StringComparison.OrdinalIgnoreCase))
            {
                return MimicTuningPostReplyIntervalMode.Random;
            }

            return MimicTuningPostReplyIntervalMode.Vanilla;
        }

        internal static HearOwnVoiceFromMimicMode ParseHearOwnVoiceMode(string? value)
        {
            if (string.Equals(value, nameof(HearOwnVoiceFromMimicMode.AlwaysHear), StringComparison.OrdinalIgnoreCase))
            {
                return HearOwnVoiceFromMimicMode.AlwaysHear;
            }

            if (string.Equals(value, nameof(HearOwnVoiceFromMimicMode.OnlyWhenSingleplayer), StringComparison.OrdinalIgnoreCase))
            {
                return HearOwnVoiceFromMimicMode.OnlyWhenSingleplayer;
            }

            return HearOwnVoiceFromMimicMode.Vanilla;
        }

        internal static PossessionBtGateMode ParsePossessionBtGateMode(string? value)
        {
            if (string.Equals(value, nameof(PossessionBtGateMode.Always), StringComparison.OrdinalIgnoreCase))
            {
                return PossessionBtGateMode.Always;
            }

            return PossessionBtGateMode.Vanilla;
        }

        internal static float RollSeconds(float minSeconds, float maxSeconds) =>
            minSeconds >= maxSeconds ? minSeconds : UnityEngine.Random.Range(minSeconds, maxSeconds);

        internal static float ResolveTrustScore(
            MimicTuningValueMode mode,
            float vanillaValue,
            float fixedValue,
            float randomMin,
            float randomMax)
        {
            return mode switch
            {
                MimicTuningValueMode.Fixed => Mathf.Clamp(fixedValue, 0f, 100f),
                MimicTuningValueMode.Random => Mathf.Clamp(RollSeconds(randomMin, randomMax), 0f, 100f),
                _ => vanillaValue,
            };
        }

        internal static float PercentToProbability(int percent) =>
            Mathf.Clamp01(percent / 100f);
    }
}
