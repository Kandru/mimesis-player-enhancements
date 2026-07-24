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
}
