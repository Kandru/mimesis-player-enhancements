using System;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone.Patches
{
    internal static class DeadPlayerPhonePatches
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(DeadPlayerPhonePatches)));

            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        [HarmonyPatch(typeof(CameraManager), "CheckMimicPossessionPosition")]
        internal static class CameraManagerCheckMimicPossessionPositionPostfix
        {
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
            [HarmonyPostfix]
            internal static void Postfix(PhoneLevelObject __instance, int prevState, int currentState)
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

                    if (DeadPlayerPhoneLocalState.HasActiveLocalSession
                        && DeadPlayerPhoneLocalState.PhoneLevelObjectId == phoneId
                        && prevState == (int)PhoneState.Ringing
                        && currentState == (int)PhoneState.OnCall)
                    {
                        float talkSeconds = DeadPlayerPhoneResolver.RollTalkDurationSeconds();
                        DeadPlayerPhoneLocalState.StartTalk(phoneId, talkSeconds);
                        DeadPlayerPhoneVoice.TryStartTalk(__instance);
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
}
