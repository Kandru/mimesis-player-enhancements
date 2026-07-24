namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal enum JoinAnytimePublicPresenceKind
    {
        None,
        InLobbyPublic,
        InLobbyPublicWaiting,
    }

    /// <summary>Pure Steam presence selection for public join-anytime lobbies.</summary>
    internal static class JoinAnytimeLobbyPresenceLogic
    {
        internal static JoinAnytimePublicPresenceKind Resolve(
            bool wantsPublic,
            JoinAnytimeSessionPhase phase,
            int sessionCount,
            int waitingThreshold)
        {
            if (!wantsPublic)
            {
                return JoinAnytimePublicPresenceKind.None;
            }

            if (phase == JoinAnytimeSessionPhase.Maintenance && sessionCount >= waitingThreshold)
            {
                return JoinAnytimePublicPresenceKind.InLobbyPublic;
            }

            if (phase == JoinAnytimeSessionPhase.Maintenance)
            {
                return JoinAnytimePublicPresenceKind.InLobbyPublicWaiting;
            }

            return JoinAnytimePublicPresenceKind.InLobbyPublic;
        }
    }
}
