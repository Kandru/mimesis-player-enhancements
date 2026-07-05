using System;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.Patches
{
    [HarmonyPatch(typeof(CameraManager), "CheckMimicPossessionPosition")]
    internal static class CameraManagerCheckMimicPossessionPositionPostfix
    {
        private const string Feature = "DeadPlayerFeatures";

        [HarmonyPostfix]
        internal static void Postfix(CameraManager __instance)
        {
            try
            {
                DeadPlayerPhoneClient.UpdateAfterMimicCheck(__instance.AvailableMimic);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"CheckMimicPossessionPosition postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), "SendRequestStartPossession")]
    internal static class CameraManagerSendRequestStartPossessionPrefix
    {
        private const string Feature = "DeadPlayerFeatures";

        [HarmonyPrefix]
        internal static bool Prefix()
        {
            try
            {
                if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled)
                {
                    return true;
                }

                if (DeadPlayerPhoneLocalState.HasActiveLocalSession)
                {
                    DeadPlayerPhoneClient.TryEndInteraction();
                    return false;
                }

                if (DeadPlayerPhoneClient.PreferredAction != PreferredDeadPlayerAction.Phone)
                {
                    return true;
                }

                return !DeadPlayerPhoneClient.TrySendRingRequest();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SendRequestStartPossession prefix failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), "ChangeSpectatorCameraTarget", typeof(int))]
    internal static class CameraManagerChangeSpectatorCameraTargetIntPrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref bool __result)
        {
            if (DeadPlayerPhoneCamera.IsLocked)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.ChangeSpectatorCameraTarget), typeof(string))]
    internal static class CameraManagerChangeSpectatorCameraTargetStringPrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actorName)
        {
            return !DeadPlayerPhoneCamera.IsLocked;
        }
    }

    [HarmonyPatch(typeof(CameraManager), "ChangeSpectatorTargetToNextPlayerAfterDelay")]
    internal static class CameraManagerChangeSpectatorTargetToNextPlayerAfterDelayPrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix()
        {
            return !DeadPlayerPhoneCamera.IsLocked;
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleLevelObject))]
    internal static class VPlayerHandleLevelObjectPrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(
            VPlayer __instance,
            int levelObjectID,
            int state,
            bool occupy,
            int hashCode)
        {
            if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled)
            {
                return true;
            }

            if (!DeadPlayerPhoneServer.TryInterceptHandleLevelObject(
                    __instance,
                    levelObjectID,
                    state,
                    occupy,
                    out MsgErrorCode result,
                    out int fromState,
                    out int toState))
            {
                return true;
            }

            UseLevelObjectRes response = new UseLevelObjectRes(hashCode)
            {
                errorCode = result,
                fromState = fromState,
                toState = toState,
            };
            __instance.SendToMe(response);
            return false;
        }
    }

    [HarmonyPatch(typeof(IVroom), nameof(IVroom.HandleLevelObject))]
    internal static class IVroomHandleLevelObjectPostfix
    {
        private const string Feature = "DeadPlayerFeatures";

        [HarmonyPostfix]
        internal static void Postfix(
            IVroom __instance,
            int actorID,
            int levelObjectID,
            int state,
            MsgErrorCode __result,
            ref int prevState)
        {
            if (__result != MsgErrorCode.Success
                || !DeadPlayerPhoneResolver.IsPhoneRingEnabled
                || !DeadPlayerPhoneResolver.ShouldApplyHost)
            {
                return;
            }

            try
            {
                DeadPlayerPhoneServer.OnPhoneStateChanged(
                    __instance,
                    levelObjectID,
                    prevState,
                    state,
                    actorID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"HandleLevelObject postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PhoneLevelObject), nameof(PhoneLevelObject.OnChangeLevelObjectStateSig))]
    internal static class PhoneLevelObjectOnChangeLevelObjectStateSigPostfix
    {
        private const string Feature = "DeadPlayerFeatures";

        [HarmonyPostfix]
        internal static void Postfix(
            PhoneLevelObject __instance,
            int actorId,
            int occupiedActorID,
            int prevState,
            int currentState)
        {
            try
            {
                if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled)
                {
                    return;
                }

                int phoneId = DeadPlayerPhoneClient.GetLevelObjectId(__instance);
                if (phoneId <= 0)
                {
                    return;
                }

                if (prevState == (int)PhoneState.Idle && currentState == (int)PhoneState.Ringing)
                {
                    DeadPlayerPhoneVoiceSession.SetRingInitiator(phoneId, actorId);
                }

                if (prevState == (int)PhoneState.Ringing
                    && currentState == (int)PhoneState.OnCall
                    && DeadPlayerPhoneVoiceSession.TryGetRingInitiator(phoneId, out int ringInitiatorActorId))
                {
                    GameMainBase? main = DeadPlayerPhoneGameAccess.TryGetMain();
                    ProtoActor? ringInitiator = main?.GetActorByActorID(ringInitiatorActorId);
                    int answererActorId = occupiedActorID > 0 ? occupiedActorID : __instance.OccupiedActorID;
                    if (ringInitiator != null && ringInitiator.dead && answererActorId > 0)
                    {
                        DeadPlayerPhoneVoiceSession.BeginTalk(__instance, ringInitiatorActorId, answererActorId);
                    }
                }

                if (prevState == (int)PhoneState.Ringing && currentState == (int)PhoneState.OnCall)
                {
                    DeadPlayerPhoneVoiceSession.ClearRingInitiator(phoneId);
                }

                if (DeadPlayerPhoneLocalState.HasActiveLocalSession
                    && DeadPlayerPhoneLocalState.PhoneLevelObjectId == phoneId
                    && prevState == (int)PhoneState.Ringing
                    && currentState == (int)PhoneState.OnCall)
                {
                    float talkSeconds = DeadPlayerPhoneResolver.RollTalkDurationSeconds();
                    DeadPlayerPhoneLocalState.StartTalk(phoneId, talkSeconds);
                    DeadPlayerPhoneVoiceSession.StartDeadCallerVoice(__instance);
                }

                if (DeadPlayerPhoneVoiceSession.IsModCallActive
                    && DeadPlayerPhoneVoiceSession.PhoneLevelObjectId == phoneId
                    && currentState is (int)PhoneState.Idle or (int)PhoneState.Busy or (int)PhoneState.BusyWait)
                {
                    DeadPlayerPhoneVoiceSession.End();
                }

                if (DeadPlayerPhoneLocalState.HasActiveLocalSession
                    && DeadPlayerPhoneLocalState.PhoneLevelObjectId == phoneId
                    && currentState is (int)PhoneState.Idle or (int)PhoneState.Busy)
                {
                    DeadPlayerPhoneLocalSession.Clear(endVoice: true);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnChangeLevelObjectStateSig postfix failed — {ex.Message}");
            }
        }
    }
}
