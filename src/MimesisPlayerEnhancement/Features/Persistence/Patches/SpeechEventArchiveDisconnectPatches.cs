namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStopClient))]
    internal static class SpeechEventArchiveDisconnectPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPrefix]
        public static void Prefix(SpeechEventArchive __instance)
        {
            SpeechEventArchiveRegistry.Unregister(__instance);
            PlayerLifecycleCoordinator.ClearConnectState(__instance);

            try
            {
                if (!MimesisSaveManager.IsHost())
                {
                    return;
                }

                bool isLocal = false;
                try
                {
                    isLocal = __instance.IsLocal;
                }
                catch
                {
                    /* Player ref may be gone */
                }

                if (isLocal)
                {
                    return;
                }

                ulong steamId = 0;
                long playerUID = 0;
                _ = VoiceEventStats.TryCaptureArchiveIdentity(__instance, out playerUID, out _, out steamId);

                if (JoinAnytime.JoinAnytimePlayerRegistration.ShouldDeferRegistration(playerUID))
                {
                    return;
                }

                int cached = SpeechEventPoolManager.CacheEventsFromArchive(__instance, steamId, playerUID);
                PlayerLifecycleCoordinator.OnArchiveDisconnecting(
                    __instance,
                    new PlayerLifecycleContribution(Feature, $"cached {cached} voice events"),
                    steamId,
                    playerUID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Disconnect cache error: {ex.Message}");
            }
        }
    }
}
