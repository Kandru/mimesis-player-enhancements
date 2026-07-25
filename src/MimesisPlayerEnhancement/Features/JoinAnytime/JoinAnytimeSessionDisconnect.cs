namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeSessionDisconnect
    {
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
