namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Resolves player identity from speech event archives, tolerating archives
    /// whose fields are not yet initialized.
    /// </summary>
    internal static class StatisticsArchiveIdentity
    {
        internal static ulong ResolveSteamIdFromArchive(SpeechEventArchive archive)
        {
            return TryCaptureArchiveIdentity(archive, out _, out _, out ulong steamId)
                ? steamId
                : 0;
        }

        internal static bool IsArchiveIdentityReady(SpeechEventArchive archive)
        {
            long playerUid;
            bool isLocal;
            try
            {
                playerUid = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                return false;
            }

            if (!isLocal && playerUid == 0)
            {
                try
                {
                    return !string.IsNullOrEmpty(archive.PlayerId);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Best-effort identity from a live archive. Enriches uid/steam from session
        /// contexts when archive fields are still zero.
        /// </summary>
        internal static bool TryCaptureArchiveIdentity(
            SpeechEventArchive? archive,
            out long playerUid,
            out bool isLocal,
            out ulong steamId)
        {
            playerUid = 0;
            isLocal = false;
            steamId = 0;

            if (archive == null)
            {
                return false;
            }

            try
            {
                playerUid = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                return false;
            }

            steamId = ResolveSteamIdFromFields(playerUid, isLocal, archive);
            if (steamId == 0 || playerUid == 0)
            {
                EnrichFromSession(ref playerUid, ref steamId);
            }

            return steamId != 0 || playerUid != 0;
        }

        private static ulong ResolveSteamIdFromFields(long playerUid, bool isLocal, SpeechEventArchive archive)
        {
            if (!isLocal && playerUid == 0)
            {
                try
                {
                    if (string.IsNullOrEmpty(archive.PlayerId))
                    {
                        return 0;
                    }
                }
                catch
                {
                    return 0;
                }
            }

            return GameSessionAccess.ResolveSteamId(playerUid, isLocal);
        }

        private static void EnrichFromSession(ref long playerUid, ref ulong steamId)
        {
            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return;
            }

            foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
            {
                try
                {
                    if (playerUid != 0 && context.GetPlayerUID() == playerUid)
                    {
                        if (steamId == 0 && context.SteamID != 0)
                        {
                            steamId = context.SteamID;
                        }

                        return;
                    }

                    if (steamId != 0 && context.SteamID == steamId)
                    {
                        if (playerUid == 0)
                        {
                            long fromSession = context.GetPlayerUID();
                            if (fromSession != 0)
                            {
                                playerUid = fromSession;
                            }
                        }

                        return;
                    }
                }
                catch
                {
                    /* Context may be mid-setup or disposed */
                }
            }
        }
    }
}
