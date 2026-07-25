using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/SessionManager.cs:L84-90
    [HarmonyPatch(typeof(SessionManager), nameof(SessionManager.Remove))]
    internal static class SessionManagerRemovePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionManager __instance, long sessionID) =>
            JoinAnytimeSessionDisconnect.OnSessionLeaving(__instance, sessionID, abandonIfDeferred: true);
    }

    // game@0.3.1 Assembly-CSharp/SessionManager.cs:L92-105
    // Transport drops may enter a dormant snapshot (grace reconnect) without calling Remove.
    [HarmonyPatch(typeof(SessionManager), nameof(SessionManager.HandleTransportDrop))]
    internal static class SessionManagerHandleTransportDropPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SessionManager __instance, long sessionID, DisconnectReason reason)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || !SessionContextAccess.TryGetSessionContextBySessionId(__instance, sessionID, out SessionContext? context)
                || context == null)
            {
                return;
            }

            bool abandonIfDeferred = !JoinAnytimeReconnectLogic.IsGraceEligible(context, reason);
            JoinAnytimeSessionDisconnect.OnSessionLeaving(context, abandonIfDeferred);
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
            if (snap == null)
            {
                return;
            }

            JoinAnytimeSessionDisconnect.OnSessionLeaving(snap.PlayerUID, snap.SteamID, abandonIfDeferred: true);
        }
    }
}
