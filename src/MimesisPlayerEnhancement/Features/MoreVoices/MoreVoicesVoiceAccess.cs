using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Reflection access to voice recording APIs without referencing Dissonance types.
    /// </summary>
    internal static class MoreVoicesVoiceAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HubVoicemanField =
            typeof(Hub).GetField("voiceman", InstanceFlags);

        private static readonly FieldInfo? HubCameramanField =
            typeof(Hub).GetField("cameraman", InstanceFlags);

        private static readonly PropertyInfo? SpeechEventRecorderProperty =
            AccessTools.Property(typeof(VoiceManager), "speechEventRecorder");

        private static readonly PropertyInfo? VoiceModeProperty =
            AccessTools.Property(typeof(VoiceManager), "voiceMode");

        private static readonly PropertyInfo? CameraModeProperty =
            AccessTools.Property(typeof(CameraManager), "Mode");

        internal static VoiceManager? TryGetVoiceManager()
        {
            if (Hub.s == null || HubVoicemanField == null)
            {
                return null;
            }

            return HubVoicemanField.GetValue(Hub.s) as VoiceManager;
        }

        internal static VoiceMode? TryGetVoiceMode(VoiceManager voiceman)
        {
            if (VoiceModeProperty == null)
            {
                return null;
            }

            return VoiceModeProperty.GetValue(voiceman) is VoiceMode mode ? mode : null;
        }

        internal static bool IsLocalPlayerPossessingMimic()
        {
            if (Hub.s == null || HubCameramanField == null || CameraModeProperty == null)
            {
                return false;
            }

            if (HubCameramanField.GetValue(Hub.s) is not CameraManager cameraman)
            {
                return false;
            }

            return cameraman.Mode == CameraManager.CameraMode.MimicPossession;
        }

        internal static void StartRecording(VoiceManager voiceman)
        {
            object? recorder = SpeechEventRecorderProperty?.GetValue(voiceman);
            if (recorder == null)
            {
                return;
            }

            AccessTools.Method(recorder.GetType(), "StartRecording")?.Invoke(recorder, null);
        }

        internal static void StopRecording(VoiceManager voiceman)
        {
            object? recorder = SpeechEventRecorderProperty?.GetValue(voiceman);
            if (recorder == null)
            {
                return;
            }

            AccessTools.Method(recorder.GetType(), "StopRecording")?.Invoke(recorder, null);
        }
    }
}
