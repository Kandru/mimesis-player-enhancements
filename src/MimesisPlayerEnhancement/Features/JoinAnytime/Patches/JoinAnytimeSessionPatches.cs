using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))]
    internal static class GameSessionInfoCanEnterSessionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameSessionInfo __instance, ref bool __result)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            switch (__instance.GameSessionState)
            {
                case VGameSessionState.Ready:
                case VGameSessionState.WaitStartSession:
                case VGameSessionState.EndGame:
                    __result = true;
                    return false;
                case VGameSessionState.OnPlaying:
                case VGameSessionState.DeathMatch:
                case VGameSessionState.AfterGame:
                    __result = false;
                    return false;
                default:
                    __result = JoinAnytimeRoomTools.AreJoinsOpen();
                    return false;
            }
        }
    }

    [HarmonyPatch(typeof(SessionContext), nameof(SessionContext.Login))]
    internal static class SessionContextLoginPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SessionContext __instance)
        {
            HostStatusCache.Invalidate();
            JoinAnytimeConnectingTracker.OnServerLogin(__instance);
        }
    }

    [HarmonyPatch]
    internal static class VPlayerCtorPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Constructor(
                typeof(VPlayer),
                [
                    typeof(SessionContext),
                    typeof(int),
                    typeof(int),
                    typeof(bool),
                    typeof(string),
                    typeof(string),
                    typeof(PosWithRot),
                    typeof(bool),
                    typeof(IVroom),
                    typeof(ReasonOfSpawn),
                ]);

        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            JoinAnytimeConnectingTracker.OnServerPlayerCreated(__instance);
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))]
    internal static class VPlayerHandleLevelLoadCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            JoinAnytimeConnectingTracker.OnLevelLoadCompleted(__instance);
            LateJoinManager.OnLevelLoadCompleted(__instance);
        }
    }
}
