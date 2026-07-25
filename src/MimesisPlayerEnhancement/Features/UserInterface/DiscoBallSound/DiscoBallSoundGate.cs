using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundGate
    {
        private static readonly FieldInfo? PartyMasterAudioKeyField =
            AccessTools.Field(typeof(PartyButtonLevelObject), "partyMasterAudioKey");

        internal static bool ShouldReplaceParty(PartyButtonLevelObject button)
        {
            if (!DiscoBallSoundResolver.ShouldApplyReplacement())
            {
                return false;
            }

            if (!HasPartyAudioKey(button))
            {
                return false;
            }

            string? variant = DiscoBallSoundSession.ResolveVariantFileName();
            if (string.IsNullOrWhiteSpace(variant))
            {
                return false;
            }

            if (DiscoBallSoundRuntime.TryGetCachedClip(variant) == null)
            {
                ModLog.Warn(
                    DiscoBallSoundConstants.Feature,
                    $"Disco ball sound replacement skipped — clip not preloaded ({variant})");
                return false;
            }

            return true;
        }

        private static bool HasPartyAudioKey(PartyButtonLevelObject button)
        {
            if (PartyMasterAudioKeyField == null)
            {
                return false;
            }

            string? key = PartyMasterAudioKeyField.GetValue(button) as string;
            return !string.IsNullOrWhiteSpace(key);
        }
    }
}
