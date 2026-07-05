using Mimic.Voice.SpeechSystem;

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
            long playerUid;
            bool isLocal;
            try
            {
                playerUid = archive.PlayerUID;
                isLocal = archive.IsLocal;
            }
            catch
            {
                return 0;
            }

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
    }
}
