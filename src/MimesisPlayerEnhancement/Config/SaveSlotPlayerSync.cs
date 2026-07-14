namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Finalizes connected player roster entries at vanilla save time,
    /// bypassing JoinAnytime deferral so sidecars reflect everyone in the session.
    /// </summary>
    internal static class SaveSlotPlayerSync
    {
        internal static void FinalizeConnectedPlayersForSave(
            int slotId,
            IReadOnlyList<string>? vanillaPlayerNames)
        {
            _ = slotId;
            _ = vanillaPlayerNames;

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
                {
                    VPlayer? player = WebDashboardSessionAccess.GetVPlayer(context);
                    if (player == null)
                    {
                        continue;
                    }

                    ulong steamId = player.SteamID != 0
                        ? player.SteamID
                        : GameSessionAccess.ResolveSteamId(player.UID, player.IsHost);
                    if (steamId == 0)
                    {
                        continue;
                    }

                    string displayName = StatisticsDisplayNameResolver.Resolve(steamId, player.ActorName);
                    string? voiceId = TryResolveVoiceId(steamId, player);
                    _ = SaveSlotDocumentStore.UpsertPlayer(steamId, displayName, voiceId);
                }
            }

            GameSessionInfo? sessionInfo = GameSessionAccess.TryGetGameSessionInfo();
            if (sessionInfo?.TotalPlayerSteamIDs == null)
            {
                return;
            }

            foreach (ulong steamId in sessionInfo.TotalPlayerSteamIDs.Keys)
            {
                if (steamId == 0)
                {
                    continue;
                }

                string displayName = StatisticsDisplayNameResolver.Resolve(steamId, string.Empty);
                if (SaveSlotDocumentStore.IsUsableName(displayName, steamId))
                {
                    _ = SaveSlotDocumentStore.UpsertPlayer(steamId, displayName);
                }
            }
        }

        private static string? TryResolveVoiceId(ulong steamId, VPlayer player)
        {
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    if (string.IsNullOrEmpty(archive.PlayerId))
                    {
                        continue;
                    }

                    ulong archiveSteamId = GameSessionAccess.ResolveSteamId(archive.PlayerUID, archive.IsLocal);
                    if (archiveSteamId == steamId)
                    {
                        return archive.PlayerId;
                    }
                }
                catch
                {
                    /* archive may be tearing down */
                }
            }

            return SaveSlotDocumentStore.TryGetVoiceId(SaveSlotDocumentStore.LoadedSlotId, steamId, out string? voiceId)
                ? voiceId
                : null;
        }
    }
}
