using System;
using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneVoice
    {
        private const string Feature = "DeadPlayerFeatures";

        private const float DefaultRestoredPlayerVolume = 1f;

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

        private static bool _voiceActive;

        private static bool _answererRelayActive;

        private static bool _proximitySuppressed;

        private static string? _suppressedDeadPlayerId;

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
                if (!InvokeConnectPhone(voiceman, phone))
                {
                    ModLog.Warn(Feature, "Dead-player phone talk voice relay to caller failed");
                    EndTalk();
                    return;
                }

                _voiceActive = true;
                ModLog.Info(Feature, "Dead-player phone talk voice started");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Start talk voice failed — {ex.Message}");
                EndTalk();
            }
        }

        internal static void TryConnectAnswererRelay(PhoneLevelObject phone, int deadCallerActorId)
        {
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            if (_answererRelayActive || voiceman == null || main == null || deadCallerActorId <= 0)
            {
                return;
            }

            ProtoActor? deadCaller = main.GetActorByActorID(deadCallerActorId);
            if (deadCaller == null)
            {
                ModLog.Warn(Feature, $"Answerer relay skipped — dead caller actor {deadCallerActorId} not found");
                return;
            }

            if (TryConnectRelayFromPlayerUid(voiceman, deadCaller.UID, phone, deadCaller))
            {
                _answererRelayActive = true;
                ModLog.Info(Feature, $"Answerer phone relay connected — deadCaller={deadCallerActorId}");
            }
        }

        internal static void UpdateProximitySuppression()
        {
            if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled
                || !DeadPlayerPhoneClientTalkState.IsActive)
            {
                RestoreProximitySuppression();
                return;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (pdata == null || main == null || voiceman == null)
            {
                return;
            }

            ProtoActor? myAvatar = main.GetMyAvatar();
            if (myAvatar == null || myAvatar.dead)
            {
                RestoreProximitySuppression();
                return;
            }

            ProtoActor? deadCaller = main.GetActorByActorID(DeadPlayerPhoneClientTalkState.DeadCallerActorId);
            if (deadCaller == null)
            {
                RestoreProximitySuppression();
                return;
            }

            if (!TryResolveDissonancePlayerId(voiceman, deadCaller.UID, out string deadPlayerId))
            {
                return;
            }

            if (!_proximitySuppressed || _suppressedDeadPlayerId != deadPlayerId)
            {
                SetPlayerVolumeMethod?.Invoke(voiceman, [deadPlayerId, 0f]);
                _proximitySuppressed = true;
                _suppressedDeadPlayerId = deadPlayerId;
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

        internal static void EndAnswererRelay()
        {
            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (!_answererRelayActive && voiceman == null)
            {
                return;
            }

            try
            {
                DisconnectVoiceRelayFromPhoneMethod?.Invoke(voiceman, [PhoneChannelType]);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"End answerer relay failed — {ex.Message}");
            }
            finally
            {
                _answererRelayActive = false;
            }
        }

        internal static void ClearAll()
        {
            EndTalk();
            EndAnswererRelay();
            RestoreProximitySuppression(force: true);
        }

        private static void RestoreProximitySuppression(bool force = false)
        {
            if (!_proximitySuppressed && !force)
            {
                return;
            }

            VoiceManager? voiceman = DeadPlayerPhoneGameAccess.TryGetVoiceManager();
            if (voiceman != null && !string.IsNullOrEmpty(_suppressedDeadPlayerId))
            {
                SetPlayerVolumeMethod?.Invoke(voiceman, [_suppressedDeadPlayerId, DefaultRestoredPlayerVolume]);
            }

            _proximitySuppressed = false;
            _suppressedDeadPlayerId = null;
        }

        private static bool InvokeConnectPhone(VoiceManager voiceman, PhoneLevelObject phone)
        {
            if (ConnectVoiceRelayToPhoneMethod == null || PhoneChannelType == null)
            {
                return false;
            }

            return ConnectVoiceRelayToPhoneMethod.Invoke(voiceman, [PhoneChannelType, phone]) is true;
        }

        private static bool TryConnectRelayFromPlayerUid(
            VoiceManager voiceman,
            long playerUid,
            PhoneLevelObject phone,
            ProtoActor actor)
        {
            if (PhoneChannelType == null
                || DirectChannelTypeInfoType == null
                || DirectChannelsField == null
                || phone.Receiver == null)
            {
                return false;
            }

            if (!TryResolveDissonancePlayerId(voiceman, playerUid, out string playerId))
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
            if (forwarder == null || setReceiverMethod == null)
            {
                return false;
            }

            if (DirectChannelsField.GetValue(voiceman) is not IDictionary directChannels)
            {
                return false;
            }

            if (directChannels.Contains(PhoneChannelType))
            {
                ModLog.Debug(Feature, "Replacing existing phone relay before reconnecting answerer relay");
                DisconnectVoiceRelayFromPhoneMethod?.Invoke(voiceman, [PhoneChannelType]);
            }

            setReceiverMethod.Invoke(forwarder, [phone.Receiver]);
            phone.Receiver.StartVoiceRelay();

            object channelInfo = Activator.CreateInstance(DirectChannelTypeInfoType)!;
            DirectChannelTypeInfoType.GetField("VoiceSpawner")?.SetValue(channelInfo, MimicVoiceSpawnerField?.GetValue(voiceman));
            DirectChannelTypeInfoType.GetField("Forwarder")?.SetValue(channelInfo, forwarder);
            DirectChannelTypeInfoType.GetField("Receiver")?.SetValue(channelInfo, phone.Receiver);
            DirectChannelTypeInfoType.GetField("Actor")?.SetValue(channelInfo, actor);
            DirectChannelTypeInfoType.GetField("Phone")?.SetValue(channelInfo, phone);
            directChannels.Add(PhoneChannelType, channelInfo);
            return true;
        }

        private static bool TryResolveDissonancePlayerId(
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
