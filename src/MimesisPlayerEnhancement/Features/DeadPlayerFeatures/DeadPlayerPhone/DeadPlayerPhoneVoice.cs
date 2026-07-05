using System;
using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    /// <summary>
    /// Low-level voice channel control and phone relay wiring for mod calls.
    /// </summary>
    internal static class DeadPlayerPhoneVoice
    {
        private const string Feature = "DeadPlayerFeatures";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type? DirectChannelType =
            typeof(VoiceManager).GetNestedType("DirectChannelType", BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly Type? DirectChannelTypeInfoType =
            typeof(VoiceManager).GetNestedType("DirectChannelTypeInfo", BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly MethodInfo? SetPlayerChannelMethod =
            typeof(VoiceManager).GetMethod("SetPlayerChannel", InstanceFlags, null, [typeof(bool), typeof(bool)], null);

        private static readonly MethodInfo? SetObserverChannelMethod =
            typeof(VoiceManager).GetMethod("SetObserverChannel", InstanceFlags, null, [typeof(bool), typeof(bool)], null);

        private static readonly MethodInfo? DisconnectVoiceRelayFromPhoneMethod =
            DirectChannelType == null
                ? null
                : typeof(VoiceManager).GetMethod(
                    "DisconnectVoiceRelayFromPhone",
                    InstanceFlags,
                    null,
                    [DirectChannelType],
                    null);

        private static readonly MethodInfo? GetPlayerVolumeMethod =
            typeof(VoiceManager).GetMethod("GetPlayerVolume", InstanceFlags, null, [typeof(string)], null);

        private static readonly MethodInfo? SetPlayerVolumeMethod =
            typeof(VoiceManager).GetMethod("SetPlayerVolume", InstanceFlags, null, [typeof(string), typeof(float)], null);

        private static readonly FieldInfo? DirectChannelsField =
            typeof(VoiceManager).GetField("_directChannels", InstanceFlags);

        private static readonly FieldInfo? VoicePlayersField =
            typeof(VoiceManager).GetField("players", InstanceFlags);

        private static readonly FieldInfo? MimicVoiceSpawnerField =
            typeof(VoiceManager).GetField("mimicVoiceSpawner", InstanceFlags);

        private static readonly PropertyInfo? SpeechEventRecorderProperty =
            typeof(VoiceManager).GetProperty("speechEventRecorder", InstanceFlags);

        private static readonly object? PhoneChannelType = ResolvePhoneChannelType(DirectChannelType);

        private static bool _deadCallerChannelsActive;

        private static bool _modRelayConnected;

        internal static bool IsModRelayConnected => _modRelayConnected;

        internal static void StartDeadCallerChannels()
        {
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (_deadCallerChannelsActive || voiceman == null || GameSessionAccess.TryGetPdata() == null)
            {
                return;
            }

            try
            {
                SetPlayerChannelMethod?.Invoke(voiceman, [true, true]);
                SetObserverChannelMethod?.Invoke(voiceman, [false, false]);
                InvokeSpeechRecorderMethod(voiceman, "StartRecording");
                _deadCallerChannelsActive = true;
                ModLog.Info(Feature, "Dead-player phone talk channels started");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Start dead caller channels failed — {ex.Message}");
                EndDeadCallerChannels();
            }
        }

        internal static void EndDeadCallerChannels()
        {
            if (!_deadCallerChannelsActive)
            {
                return;
            }

            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            try
            {
                if (voiceman != null)
                {
                    InvokeSpeechRecorderMethod(voiceman, "StopRecording");
                    SetPlayerChannelMethod?.Invoke(voiceman, [false, true]);
                    SetObserverChannelMethod?.Invoke(voiceman, [true, true]);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"End dead caller channels failed — {ex.Message}");
            }
            finally
            {
                _deadCallerChannelsActive = false;
            }
        }

        internal static bool ConnectRelayToPlayerUid(
            PhoneLevelObject phone,
            long targetPlayerUid,
            ProtoActor targetActor)
        {
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (voiceman == null || phone.Receiver == null)
            {
                return false;
            }

            if (!TryResolveDissonancePlayerId(voiceman, targetPlayerUid, out string playerId))
            {
                return false;
            }

            object? playback = TryFindVoicePlayback(playerId);
            if (playback == null)
            {
                return false;
            }

            PropertyInfo? forwarderProperty = playback.GetType().GetProperty("VoiceForwarder", InstanceFlags);
            object? forwarder = forwarderProperty?.GetValue(playback);
            MethodInfo? setReceiverMethod = forwarder?.GetType().GetMethod(
                "SetReceiver",
                InstanceFlags,
                null,
                [typeof(Mimic.Voice.VoiceAudioReceiver)],
                null);
            if (forwarder == null || setReceiverMethod == null
                || PhoneChannelType == null
                || DirectChannelTypeInfoType == null
                || DirectChannelsField?.GetValue(voiceman) is not IDictionary directChannels)
            {
                return false;
            }

            try
            {
                if (_modRelayConnected)
                {
                    DisconnectModRelayIfActive();
                }

                setReceiverMethod.Invoke(forwarder, [phone.Receiver]);
                phone.Receiver.StartVoiceRelay();

                object channelInfo = Activator.CreateInstance(DirectChannelTypeInfoType)!;
                DirectChannelTypeInfoType.GetField("VoiceSpawner")?.SetValue(
                    channelInfo,
                    MimicVoiceSpawnerField?.GetValue(voiceman));
                DirectChannelTypeInfoType.GetField("Forwarder")?.SetValue(channelInfo, forwarder);
                DirectChannelTypeInfoType.GetField("Receiver")?.SetValue(channelInfo, phone.Receiver);
                DirectChannelTypeInfoType.GetField("Actor")?.SetValue(channelInfo, targetActor);
                DirectChannelTypeInfoType.GetField("Phone")?.SetValue(channelInfo, phone);
                directChannels[PhoneChannelType] = channelInfo;
                _modRelayConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Connect relay failed — uid={targetPlayerUid}, {ex.Message}");
                return false;
            }
        }

        internal static void DisconnectModRelayIfActive()
        {
            if (!_modRelayConnected)
            {
                return;
            }

            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            try
            {
                DisconnectVoiceRelayFromPhoneMethod?.Invoke(voiceman, [PhoneChannelType]);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Disconnect mod relay failed — {ex.Message}");
            }
            finally
            {
                _modRelayConnected = false;
            }
        }

        internal static bool TryGetPlayerVolume(VoiceManager voiceman, string playerId, out float volume)
        {
            volume = 1f;
            if (GetPlayerVolumeMethod == null)
            {
                return false;
            }

            object? result = GetPlayerVolumeMethod.Invoke(voiceman, [playerId]);
            if (result is float value)
            {
                volume = value;
                return true;
            }

            return false;
        }

        internal static void SetPlayerVolume(VoiceManager voiceman, string playerId, float volume)
        {
            SetPlayerVolumeMethod?.Invoke(voiceman, [playerId, volume]);
        }

        internal static bool TryResolveDissonancePlayerId(
            VoiceManager voiceman,
            long playerUid,
            out string playerId)
        {
            playerId = string.Empty;
            if (VoicePlayersField?.GetValue(voiceman) is not IEnumerable players)
            {
                return false;
            }

            foreach (object player in players)
            {
                if (player == null)
                {
                    continue;
                }

                PropertyInfo? uidProperty = player.GetType().GetProperty("PlayerUID", InstanceFlags);
                PropertyInfo? idProperty = player.GetType().GetProperty("PlayerId", InstanceFlags);
                if (uidProperty?.GetValue(player) is long uid
                    && uid == playerUid
                    && idProperty?.GetValue(player) is string id
                    && !string.IsNullOrEmpty(id))
                {
                    playerId = id;
                    return true;
                }
            }

            return false;
        }

        internal static void ResetAll()
        {
            EndDeadCallerChannels();
            DisconnectModRelayIfActive();
        }

        private static object? TryFindVoicePlayback(string playerId)
        {
            Type? commsType = Type.GetType("Dissonance.Integrations.FishNet.DissonanceFishNetComms, Assembly-CSharp");
            PropertyInfo? instanceProperty = commsType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object? commsInstance = instanceProperty?.GetValue(null);
            PropertyInfo? commsProperty = commsInstance?.GetType().GetProperty("Comms", InstanceFlags);
            object? comms = commsProperty?.GetValue(commsInstance);
            MethodInfo? findPlayerMethod = comms?.GetType().GetMethod(
                "FindPlayer",
                InstanceFlags,
                null,
                [typeof(string)],
                null);
            object? playerState = findPlayerMethod?.Invoke(comms, [playerId]);
            return playerState?.GetType().GetProperty("Playback", InstanceFlags)?.GetValue(playerState);
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
