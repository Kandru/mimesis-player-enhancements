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

            players.Sort(static (left, right) =>
            {
                int loadedCompare = left.Loaded.CompareTo(right.Loaded);
                if (loadedCompare != 0)
                {
                    // Loaded players first (false < true when comparing Loaded flag inverted:
                    // we want Loaded=true first, so compare right to left on bool as int).
                    return right.Loaded.CompareTo(left.Loaded);
                }

                int nameCompare = string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison.OrdinalIgnoreCase);
                if (nameCompare != 0)
                {
                    return nameCompare;
                }

                return left.PlayerUid.CompareTo(right.PlayerUid);
            });

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
            string displayName = ResolveDisplayName(context, steamId);
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

        private static string ResolveDisplayName(SessionContext context, ulong steamId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(context.NickName))
                {
                    return context.NickName;
                }
            }
            catch
            {
                /* mid-setup */
            }

            if (PlayerRegistry.TryGetRecord(steamId, out PlayerRecord? record)
                && !string.IsNullOrWhiteSpace(record.DisplayName))
            {
                return record.DisplayName;
            }

            return string.Empty;
        }
    }
}
