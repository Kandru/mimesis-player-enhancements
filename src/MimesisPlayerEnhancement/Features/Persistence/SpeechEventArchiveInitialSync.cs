namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Re-triggers vanilla chunked initial sync after the host injects restored voice events,
    /// so reconnecting clients receive the post-restore pool (vanilla sync runs before our Postfix).
    /// </summary>
    internal static class SpeechEventArchiveInitialSync
    {
        private const string Feature = "Persistence";

        // game@0.3.1 Assembly-CSharp/Mimic.Voice.SpeechSystem/SpeechEventArchive.cs:L413-415
        private static readonly System.Reflection.MethodInfo? RequestInitialSyncMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "ServerRpcRequestInitialSync");

        internal static void RequestAfterRestore(SpeechEventArchive archive, int totalAdded)
        {
            if (totalAdded <= 0 || archive == null || !MimesisSaveManager.IsHost())
            {
                return;
            }

            try
            {
                bool isLocal;
                try
                {
                    isLocal = archive.IsLocal;
                }
                catch
                {
                    return;
                }

                if (isLocal)
                {
                    return;
                }

                bool isOwner;
                try
                {
                    isOwner = archive.IsOwner;
                }
                catch
                {
                    return;
                }

                // Match vanilla OnStartClient: non-owner observers request sync from server.
                if (isOwner)
                {
                    return;
                }

                if (RequestInitialSyncMethod == null)
                {
                    ModLog.Warn(Feature, "ServerRpcRequestInitialSync not found — reconnect client sync skipped.");
                    return;
                }

                _ = RequestInitialSyncMethod.Invoke(archive, null);
                ModLog.Debug(Feature, $"Re-requested initial voice sync — {VoiceEventStats.DescribePlayerBrief(archive)} — events={totalAdded}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Initial sync re-request failed: {ex.Message}");
            }
        }
    }
}
