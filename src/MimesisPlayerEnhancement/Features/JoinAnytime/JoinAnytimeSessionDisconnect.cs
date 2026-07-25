namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeSessionDisconnect
    {
        internal static void OnSessionLeaving(SessionManager sessionManager, long sessionId, bool abandonIfDeferred)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || !SessionContextAccess.TryGetSessionContextBySessionId(sessionManager, sessionId, out SessionContext? context)
                || context == null)
            {
                return;
            }

            OnSessionLeaving(context, abandonIfDeferred);
        }

        internal static void OnSessionLeaving(SessionContext context, bool abandonIfDeferred)
        {
            if (!ModConfig.EnableJoinAnytime.Value || context == null)
            {
                return;
            }

            long uid = context.GetPlayerUID();
            if (uid == 0)
            {
                return;
            }

            ulong steamId = context.PlayerInfoSnapshot?.SteamID ?? context.SteamID;
            OnSessionLeaving(uid, steamId, abandonIfDeferred);
        }

        internal static void OnSessionLeaving(long uid, ulong steamId, bool abandonIfDeferred)
        {
            if (!ModConfig.EnableJoinAnytime.Value || uid == 0)
            {
                return;
            }

            LateJoinManager.OnPlayerDisconnected(uid);

            if (abandonIfDeferred && JoinAnytimePlayerRegistration.ShouldDeferRegistration(uid))
            {
                JoinAnytimePlayerRegistration.AbandonIncomplete(uid, steamId);
            }

            JoinAnytimeLobbyController.OnSessionRosterChanged();
        }
    }
}
