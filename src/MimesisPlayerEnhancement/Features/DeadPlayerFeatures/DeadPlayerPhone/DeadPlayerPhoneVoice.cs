using System;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneVoice
    {
        private const string Feature = "DeadPlayerFeatures";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type? DirectChannelType =
            typeof(VoiceManager).GetNestedType("DirectChannelType", BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly MethodInfo? SetPlayerChannelMethod =
            typeof(VoiceManager).GetMethod("SetPlayerChannel", InstanceFlags, null, [typeof(bool), typeof(bool)], null);

        private static readonly MethodInfo? SetObserverChannelMethod =
            typeof(VoiceManager).GetMethod("SetObserverChannel", InstanceFlags, null, [typeof(bool), typeof(bool)], null);

        private static readonly MethodInfo? ConnectVoiceRelayToPhoneMethod =
            DirectChannelType == null
                ? null
                : typeof(VoiceManager).GetMethod(
                    "ConnectVoiceRelayToPhone",
                    InstanceFlags,
                    null,
                    [DirectChannelType, typeof(PhoneLevelObject)],
                    null);

        private static readonly MethodInfo? DisconnectVoiceRelayFromPhoneMethod =
            DirectChannelType == null
                ? null
                : typeof(VoiceManager).GetMethod(
                    "DisconnectVoiceRelayFromPhone",
                    InstanceFlags,
                    null,
                    [DirectChannelType],
                    null);

        private static readonly PropertyInfo? SpeechEventRecorderProperty =
            typeof(VoiceManager).GetProperty("speechEventRecorder", InstanceFlags);

        private static readonly object? PhoneChannelType = ResolvePhoneChannelType(DirectChannelType);

        private static bool _voiceActive;

        internal static bool IsVoiceActive => _voiceActive;

        internal static void TryStartTalk(PhoneLevelObject phone)
        {
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (_voiceActive || voiceman == null || GameSessionAccess.TryGetPdata() == null)
            {
                return;
            }

            try
            {
                SetPlayerChannelMethod?.Invoke(voiceman, [true, true]);
                SetObserverChannelMethod?.Invoke(voiceman, [false, false]);
                InvokeSpeechRecorderMethod(voiceman, "StartRecording");
                if (InvokeConnectPhone(voiceman, phone))
                {
                    _voiceActive = true;
                    ModLog.Info(Feature, "Dead-player phone talk voice started");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Start talk voice failed — {ex.Message}");
                EndTalk();
            }
        }

        internal static void EndTalk()
        {
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (!_voiceActive && voiceman == null)
            {
                return;
            }

            try
            {
                if (voiceman != null)
                {
                    InvokeSpeechRecorderMethod(voiceman, "StopRecording");
                    DisconnectVoiceRelayFromPhoneMethod?.Invoke(voiceman, [PhoneChannelType]);
                    SetPlayerChannelMethod?.Invoke(voiceman, [false, true]);
                    SetObserverChannelMethod?.Invoke(voiceman, [true, true]);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"End talk voice failed — {ex.Message}");
            }
            finally
            {
                _voiceActive = false;
            }
        }

        private static bool InvokeConnectPhone(VoiceManager voiceman, PhoneLevelObject phone)
        {
            if (ConnectVoiceRelayToPhoneMethod == null || PhoneChannelType == null)
            {
                return false;
            }

            return ConnectVoiceRelayToPhoneMethod.Invoke(voiceman, [PhoneChannelType, phone]) is true;
        }

        private static void InvokeSpeechRecorderMethod(VoiceManager voiceman, string methodName)
        {
            object? recorder = SpeechEventRecorderProperty?.GetValue(voiceman);
            if (recorder == null)
            {
                return;
            }

            MethodInfo? method = recorder.GetType().GetMethod(
                methodName,
                InstanceFlags,
                null,
                Type.EmptyTypes,
                null);
            method?.Invoke(recorder, null);
        }

        private static object? ResolvePhoneChannelType(Type? channelType)
        {
            if (channelType == null)
            {
                return null;
            }

            try
            {
                return Enum.Parse(channelType, "Phone");
            }
            catch
            {
                return null;
            }
        }
    }
}
