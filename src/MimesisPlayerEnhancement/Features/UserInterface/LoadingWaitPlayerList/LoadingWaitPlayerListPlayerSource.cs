namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListPlayerSource
    {
        internal static List<LoadingWaitPlayerEntry> CollectPlayers()
        {
            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return [];
            }

            List<LoadingWaitPlayerEntry> players = [];
            foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
            {
                LoadingWaitPlayerEntry? entry = TryBuildEntry(context);
                if (entry != null)
                {
                    players.Add(entry);
                }
            }

            return players;
        }

        private static LoadingWaitPlayerEntry? TryBuildEntry(SessionContext context)
        {
            ulong steamId;
            try
            {
                steamId = context.SteamID;
            }
            catch
            {
                return null;
            }

            if (steamId == 0)
            {
                return null;
            }

            long playerUid = 0;
            try
            {
                playerUid = context.GetPlayerUID();
            }
            catch
            {
                /* player may still be spawning */
            }

            VPlayer? vPlayer = SessionContextAccess.GetVPlayer(context);
            bool loaded = vPlayer != null && vPlayer.LevelLoadCompleted;
            string? nickName = null;
            try
            {
                nickName = context.NickName;
            }
            catch
            {
                /* mid-setup */
            }

            string displayName = LoadingWaitPlayerListDisplayNames.Resolve(nickName, steamId);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = steamId.ToString();
            }

            bool speaking = LoadingWaitPlayerListVoice.IsSpeaking(steamId, playerUid);
            return new LoadingWaitPlayerEntry
            {
                PlayerUid = playerUid,
                SteamId = steamId,
                DisplayName = displayName,
                Loaded = loaded,
                Speaking = speaking,
            };
        }

    }
}
