using System;
using System.Reflection;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Re-triggers vanilla chunked initial sync after the host injects restored voice events,
    /// so reconnecting clients receive the post-restore pool (vanilla sync runs before our Postfix).
    /// </summary>
    internal static class SpeechEventArchiveInitialSync
    {
        private const string Feature = "Persistence";

        private static MethodInfo? _requestInitialSyncMethod;

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

                _requestInitialSyncMethod ??= typeof(SpeechEventArchive).GetMethod(
                    "ServerRpcRequestInitialSync",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (_requestInitialSyncMethod == null)
                {
                    ModLog.Warn(Feature, "ServerRpcRequestInitialSync not found — reconnect client sync skipped.");
                    return;
                }

                _ = _requestInitialSyncMethod.Invoke(archive, null);
                ModLog.Debug(Feature, $"Re-requested initial voice sync — {VoiceEventStats.DescribePlayerBrief(archive)} — events={totalAdded}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Initial sync re-request failed: {ex.Message}");
            }
        }
    }
}
