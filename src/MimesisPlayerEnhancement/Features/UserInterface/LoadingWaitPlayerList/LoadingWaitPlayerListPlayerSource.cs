using MimesisPlayerEnhancement.Features.MoreVoices;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListPlayerSource
    {
        internal static List<LoadingWaitPlayerEntry> CollectPlayers()
        {
            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager != null)
            {
                List<LoadingWaitPlayerEntry> players = [];
                foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
                {
                    LoadingWaitPlayerEntry? entry = TryBuildEntryFromSession(context);
                    if (entry != null)
                    {
                        players.Add(entry);
                    }
                }

                if (players.Count > 0)
                {
                    return players;
                }
            }

            return CollectFromVoicePlayers();
        }

        private static List<LoadingWaitPlayerEntry> CollectFromVoicePlayers()
        {
            List<FishNetDissonancePlayer>? voicePlayers = MoreVoicesVoiceAccess.TryGetVoiceManager()?.Players;
            if (voicePlayers == null || voicePlayers.Count == 0)
            {
                return [];
            }

            GameMainBase? main = GameSessionAccess.TryGetPdata()?.main;
            List<LoadingWaitPlayerEntry> players = [];

            foreach (FishNetDissonancePlayer player in voicePlayers)
            {
                LoadingWaitPlayerEntry? entry = TryBuildEntryFromVoice(player, main);
                if (entry != null)
                {
                    players.Add(entry);
                }
            }

            return players;
        }

        private static LoadingWaitPlayerEntry? TryBuildEntryFromSession(SessionContext context)
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

            return BuildEntry(steamId, playerUid, nickName, loaded);
        }

        private static LoadingWaitPlayerEntry? TryBuildEntryFromVoice(
            FishNetDissonancePlayer player,
            GameMainBase? main)
        {
            if (player == null)
            {
                return null;
            }

            long playerUid = player.PlayerUID;
            ulong steamId = ResolveSteamId(main, playerUid);
            if (steamId == 0)
            {
                return null;
            }

            bool loaded = true;
            return BuildEntry(steamId, playerUid, nickName: null, loaded);
        }

        private static ulong ResolveSteamId(GameMainBase? main, long playerUid)
        {
            if (playerUid != 0 && main != null)
            {
                try
                {
                    string resolved = main.ResolveSteamID(playerUid);
                    if (ulong.TryParse(resolved, out ulong steamId) && steamId != 0)
                    {
                        return steamId;
                    }
                }
                catch
                {
                    /* Hub may be tearing down */
                }
            }

            return GameSessionAccess.ResolveSteamId(playerUid, playerUid == 0);
        }

        private static LoadingWaitPlayerEntry BuildEntry(
            ulong steamId,
            long playerUid,
            string? nickName,
            bool loaded)
        {
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
