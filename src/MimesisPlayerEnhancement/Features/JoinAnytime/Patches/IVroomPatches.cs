using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    /// <summary>
    /// Route after SyncEnterRoom sends AllMemberEnterRoomSig — client needs that flag before
    /// MoveToWaitingRoomSig (CycleCount != 0 waits on EnteringCompleteAll in MaintenanceScene).
    /// </summary>
    // game@0.3.1 Assembly-CSharp/IVroom.cs:L2498-2506
    [HarmonyPatch]
    internal static class IVroomOnAllMemberEnteredLateJoinPatch
    {
        private static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(IVroom), "OnAllMemberEntered");

        [HarmonyPostfix]
        private static void Postfix(IVroom __instance)
        {
            if (__instance is MaintenanceRoom)
            {
                LateJoinManager.OnMaintenanceAllMembersEntered(__instance);
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/IVroom.cs:L3078-3730
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

    // game@0.3.1 Assembly-CSharp/IVroom.cs:L1650-1907
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
}
