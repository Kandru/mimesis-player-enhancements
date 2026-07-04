using System.Collections.Generic;
using System.Reflection;
using Bifrost.ConstEnum;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using Mimic.Actors;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch(typeof(IVroom), "RunEventActionInternal")]
    internal static class IVroomRunEventActionInternalPatch
    {
        private const string Feature = "JoinAnytime";

        [HarmonyPrefix]
        private static bool Prefix(
            IVroom __instance,
            IGameAction action,
            List<IGameActionParam> paramList,
            ref bool __result)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (action is not GameAction gameAction
                || gameAction.ActionType != DefAction.MOVE_TO_NEXT_ROOM)
            {
                return true;
            }

            if (__instance is not VWaitingRoom waitingRoom || waitingRoom.BackToMaintenance)
            {
                return true;
            }

            WaitingRoomBlockReason reason = JoinAnytimeRoomTools.GetWaitingRoomBlockReason();
            if (reason == WaitingRoomBlockReason.None)
            {
                return true;
            }

            int actorId = GameActionParamHelper.FindParam<GameActionParamActor>(paramList)?.ActorID ?? 0;
            ModLog.Info(Feature, $"Blocked tram lever — reason={reason}");
            JoinAnytimeTramLeverTools.TryResetTramDepartureLever(waitingRoom, actorId);
            JoinAnytimeUserMessages.OnWaitingRoomStartBlocked(__instance, actorId);
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IVroom), nameof(IVroom.HandleLevelObject))]
    internal static class IVroomHandleLevelObjectTramLeverPatch
    {
        private const string Feature = "JoinAnytime";

        [HarmonyPrefix]
        private static bool Prefix(
            IVroom __instance,
            int actorID,
            int levelObjectID,
            int state,
            bool occupy,
            out int prevState,
            ref MsgErrorCode __result)
        {
            prevState = -1;

            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (!JoinAnytimeTramLeverTools.TryGetLevelObject(__instance, levelObjectID, out ILevelObjectInfo? levelObject)
                || levelObject == null)
            {
                return true;
            }

            if (!JoinAnytimeTramLeverTools.ShouldBlockDepartureLeverUse(__instance, levelObject, state))
            {
                return true;
            }

            if (levelObject is StateLevelObjectInfo stateInfo)
            {
                prevState = stateInfo.CurrentState;
            }

            ModLog.Info(Feature, $"Blocked tram lever state change to {state} — reason={JoinAnytimeRoomTools.GetWaitingRoomBlockReason()}");

            JoinAnytimeUserMessages.OnWaitingRoomStartBlocked(__instance, actorID);
            __result = MsgErrorCode.CantAction;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class NewTramLeverLevelObjectIsTriggerablePatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(NewTramLeverLevelObject), "IsTriggerable", [typeof(ProtoActor), typeof(int)]);

        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (!__result || !ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (JoinAnytimeHub.GetPdata()?.main is not InTramWaitingScene)
            {
                return;
            }

            if (!JoinAnytimeRoomTools.ShouldBlockWaitingRoomStartGame())
            {
                return;
            }

            __result = false;
        }
    }

    [HarmonyPatch(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))]
    internal static class NewTramLeverLevelObjectOnChangeLevelObjectStateSigPatch
    {
        [HarmonyPostfix]
        private static void Postfix(int actorId, int prevState, int currentState)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (currentState != (int)NewTramLeverState.Open)
            {
                return;
            }

            JoinAnytimeUserMessages.OnLocalTramLeverOpened(actorId);
        }
    }
}
