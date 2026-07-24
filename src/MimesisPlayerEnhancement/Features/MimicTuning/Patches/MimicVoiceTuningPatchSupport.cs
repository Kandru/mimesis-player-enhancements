using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicVoiceTuningPatchSupport
    {
        internal static readonly FieldInfo? AsServerField =
            AccessTools.Field(typeof(VoiceManager), "_asServer");

        internal static readonly FieldInfo? LastMimicVoiceTimeField =
            AccessTools.Field(typeof(VoiceManager), "_lastMimicVoiceTime");

        internal static readonly FieldInfo? PreparedVoiceContextField =
            AccessTools.Field(typeof(MimicVoiceSpawner.PreparedMimicVoiceSpawn), "Context");

        internal static readonly FieldInfo? PreparedVoiceMuteField =
            AccessTools.Field(typeof(MimicVoiceSpawner.PreparedMimicVoiceSpawn), "MuteLocalPlayerVoice");

        internal static readonly FieldInfo? MinRequiredSpeechsField =
            AccessTools.Field(typeof(MimicVoiceSpawner), "minRequiredSpeechs");

        internal static readonly FieldInfo? PlayTimeIntervalField =
            AccessTools.Field(typeof(SpeechEventAdditionalGameData), "playTimeInterval");

        internal static readonly FieldInfo? DeathMatchPlayTimeIntervalField =
            AccessTools.Field(typeof(SpeechEventAdditionalGameData), "deathMatchPlayTimeInterval");

        internal static readonly MethodInfo? SpawnMimicVoiceAfterDelayMethod =
            AccessTools.Method(typeof(VoiceManager), "SpawnMimicVoiceAfterDelay", [typeof(float), typeof(Action)]);

        internal static readonly MethodInfo GetClipReuseCooldownSecondsMethod =
            AccessTools.Method(
                typeof(MimicVoiceTuningResolver),
                nameof(MimicVoiceTuningResolver.GetClipReuseCooldownSeconds),
                Type.EmptyTypes);

        internal static readonly MethodInfo GetDeathMatchClipReuseCooldownSecondsMethod =
            AccessTools.Method(
                typeof(MimicVoiceTuningResolver),
                nameof(MimicVoiceTuningResolver.GetDeathMatchClipReuseCooldownSeconds),
                Type.EmptyTypes);

        internal static readonly MethodInfo GetSpeakAudienceRangeMetersMethod =
            AccessTools.Method(
                typeof(MimicVoiceTuningResolver),
                nameof(MimicVoiceTuningResolver.GetSpeakAudienceRangeMeters),
                Type.EmptyTypes);

        internal static readonly MethodInfo ResolveMuteLocalPlayerVoiceMethod =
            AccessTools.Method(
                typeof(MimicVoiceTuningResolver),
                nameof(MimicVoiceTuningResolver.ResolveMuteLocalPlayerVoice),
                [typeof(bool)]);

        internal static bool TryGetLastMimicVoiceTime(VoiceManager instance, out float lastTime)
        {
            lastTime = -1f;
            if (LastMimicVoiceTimeField == null)
            {
                return false;
            }

            lastTime = (float)LastMimicVoiceTimeField.GetValue(instance)!;
            return true;
        }

        internal static void SetLastMimicVoiceTime(VoiceManager instance, float value)
        {
            LastMimicVoiceTimeField?.SetValue(instance, value);
        }

        internal static bool IsServer(VoiceManager instance)
        {
            return AsServerField != null && (bool)AsServerField.GetValue(instance)!;
        }

        internal static bool TryGetPreparedVoiceContext(
            MimicVoiceSpawner.PreparedMimicVoiceSpawn preparedVoice,
            out MimicVoiceSpawner.MimicContext? context)
        {
            context = null;
            if (PreparedVoiceContextField == null)
            {
                return false;
            }

            context = PreparedVoiceContextField.GetValue(preparedVoice) as MimicVoiceSpawner.MimicContext;
            return context != null;
        }

        internal static void ApplyMinRequiredSpeechs(MimicVoiceSpawner spawner)
        {
            if (!MimicVoiceTuningResolver.ShouldApplyCustom || MinRequiredSpeechsField == null)
            {
                return;
            }

            MinRequiredSpeechsField.SetValue(spawner, MimicVoiceTuningResolver.GetMinRequiredSpeechClips());
        }

        internal static bool IsDeathMatchArea(SpeechType_Area speechTypeArea) =>
            speechTypeArea == SpeechType_Area.DeathMatch;
    }
}
