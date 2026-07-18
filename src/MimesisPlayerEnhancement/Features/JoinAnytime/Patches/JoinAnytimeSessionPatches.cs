using System.Reflection;
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
            // Host status is consumed mod-wide (host-only gating) — always invalidate.
            HostStatusCache.Invalidate();

            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnServerLogin(__instance);
            JoinAnytimeLobbyController.OnSessionRosterChanged();
        }
    }

    [HarmonyPatch(typeof(SessionManager), nameof(SessionManager.Remove))]
    internal static class SessionManagerRemovePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionManager __instance, long sessionID)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (SessionContextAccess.TryGetSessionContextBySessionId(__instance, sessionID, out SessionContext? context)
                && context != null)
            {
                long uid = context.GetPlayerUID();
                if (uid != 0)
                {
                    if (JoinAnytimePlayerRegistration.ShouldDeferRegistration(uid))
                    {
                        ulong steamId = context.PlayerInfoSnapshot?.SteamID ?? 0;
                        JoinAnytimePlayerRegistration.AbandonIncomplete(uid, steamId);
                    }

                    LateJoinManager.OnPlayerDisconnected(uid);
                }
            }
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeLobbyController.OnSessionRosterChanged();
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
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnServerPlayerCreated(__instance);
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))]
    internal static class VPlayerHandleLevelLoadCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnLevelLoadCompleted(__instance);
            JoinAnytimeLobbyController.OnSessionRosterChanged();
        }
    }
}
