using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/SessionManager.cs:L84-90
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

            if (!SessionContextAccess.TryGetSessionContextBySessionId(__instance, sessionID, out SessionContext? context)
                || context == null)
            {
                return;
            }

            long uid = context.GetPlayerUID();
            if (uid == 0)
            {
                return;
            }

            ulong steamId = context.PlayerInfoSnapshot?.SteamID ?? context.SteamID;
            JoinAnytimeSessionDisconnect.OnSessionLeaving(uid, steamId, abandonIfDeferred: true);
        }
    }

    // game@0.3.1 Assembly-CSharp/SessionManager.cs:L92-105
    [HarmonyPatch(typeof(SessionManager), nameof(SessionManager.HandleTransportDrop))]
    internal static class SessionManagerHandleTransportDropPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionManager __instance, long sessionID, DisconnectReason reason)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (!SessionContextAccess.TryGetSessionContextBySessionId(__instance, sessionID, out SessionContext? context)
                || context == null)
            {
                return;
            }

            long uid = context.GetPlayerUID();
            if (uid == 0)
            {
                return;
            }

            ulong steamId = context.PlayerInfoSnapshot?.SteamID ?? context.SteamID;
            bool abandonIfDeferred = !JoinAnytimeReconnectLogic.IsGraceEligible(context, reason);
            JoinAnytimeSessionDisconnect.OnSessionLeaving(uid, steamId, abandonIfDeferred);
        }
    }

    // game@0.3.1 Assembly-CSharp/SessionManager.cs:L189-199
    [HarmonyPatch]
    internal static class SessionManagerFinalizeDormantExpiryPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(SessionManager), "FinalizeDormantExpiry");

        [HarmonyPrefix]
        private static void Prefix(SessionManager.DormantSnapshot snap)
        {
            if (!ModConfig.EnableJoinAnytime.Value || snap == null)
            {
                return;
            }

            long uid = snap.PlayerUID;
            if (uid == 0)
            {
                return;
            }

            JoinAnytimeSessionDisconnect.OnSessionLeaving(uid, snap.SteamID, abandonIfDeferred: true);
        }
    }
}
