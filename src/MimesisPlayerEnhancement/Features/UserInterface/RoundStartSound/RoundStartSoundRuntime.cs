namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundRuntime
    {
        internal static void RefreshFromConfig()
        {
            RoundStartSoundClipCache.Clear();
            if (RoundStartSoundResolver.ShouldApplyReplacement())
            {
                PreloadVariants();
            }
        }

        internal static void OnDungeonEntryBegin()
        {
            DungeonLandingEntryTracker.Begin();
            if (RoundStartSoundResolver.ShouldApplyReplacement())
            {
                PreloadVariants();
            }
        }

        internal static void OnDungeonEntryEnterGame()
        {
            DungeonLandingEntryTracker.ScheduleCloseAfterEnterGame();
        }

        internal static void OnSessionEnded()
        {
            DungeonLandingEntryTracker.End();
            RoundStartSoundClipCache.Clear();
        }

        internal static void Shutdown()
        {
            OnSessionEnded();
            RoundStartSoundPlayer.Shutdown();
        }

        private static void PreloadVariants()
        {
            foreach (string fileName in RoundStartSoundResolver.ListVariantFileNames())
            {
                _ = RoundStartSoundClipCache.TryPreloadClip(fileName);
            }
        }
    }
}
