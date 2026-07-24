namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundRuntime
    {
        private static string? _preloadedFingerprint;

        internal static void RefreshFromConfig()
        {
            if (!RoundStartSoundResolver.ShouldApplyReplacement())
            {
                ClearPreload();
                return;
            }

            string fingerprint = BuildPreloadFingerprint();
            if (string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal)
                && RoundStartSoundClipCache.HasCachedClips)
            {
                return;
            }

            RoundStartSoundClipCache.Clear();
            PreloadVariants();
            _preloadedFingerprint = fingerprint;
        }

        internal static void OnDungeonEntryBegin()
        {
            DungeonLandingEntryTracker.Begin();
            if (RoundStartSoundResolver.ShouldApplyReplacement())
            {
                EnsurePreloaded();
            }
        }

        internal static void OnDungeonEntryEnterGame()
        {
            DungeonLandingEntryTracker.ScheduleCloseAfterEnterGame();
        }

        internal static void OnSessionEnded()
        {
            DungeonLandingEntryTracker.End();
            ClearPreload();
        }

        internal static void Shutdown()
        {
            OnSessionEnded();
            RoundStartSoundPlayer.Shutdown();
        }

        private static void EnsurePreloaded()
        {
            string fingerprint = BuildPreloadFingerprint();
            if (string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal)
                && RoundStartSoundClipCache.HasCachedClips)
            {
                return;
            }

            PreloadVariants();
            _preloadedFingerprint = fingerprint;
        }

        private static void ClearPreload()
        {
            RoundStartSoundClipCache.Clear();
            _preloadedFingerprint = null;
        }

        private static void PreloadVariants()
        {
            foreach (string fileName in RoundStartSoundResolver.ListVariantFileNames())
            {
                _ = RoundStartSoundClipCache.TryPreloadClip(fileName);
            }
        }

        private static string BuildPreloadFingerprint()
        {
            if (!ModConfig.IsInitialized)
            {
                return "uninit";
            }

            return string.Join(
                "|",
                ModConfig.RoundStartSoundMode.Value ?? "",
                ModConfig.RoundStartSoundVariant.Value ?? "",
                ModConfig.RoundStartSoundRandomPool.Value ?? "");
        }
    }
}
