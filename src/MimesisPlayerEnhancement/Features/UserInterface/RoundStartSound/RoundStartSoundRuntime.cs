using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundRuntime
    {
        private static readonly EmbeddedAudioClipCache ClipCache = new(
            RoundStartSoundConstants.AssetFolder,
            RoundStartSoundConstants.Feature,
            RoundStartSoundConstants.AssetFolder);

        private static string? _preloadedFingerprint;

        internal static AudioClip? TryGetCachedClip(string fileName) => ClipCache.TryGetCachedClip(fileName);

        internal static void RefreshFromConfig()
        {
            if (!RoundStartSoundResolver.ShouldApplyReplacement())
            {
                ClearPreload();
                return;
            }

            string fingerprint = BuildPreloadFingerprint();
            if (!string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal))
            {
                // Keep already-decoded clips — clearing + re-decoding on the main thread freezes the game.
                PreloadVariants();
                _preloadedFingerprint = fingerprint;
                return;
            }

            EnsurePreloaded();
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

        internal static void OnPlaySceneDestroyed()
        {
            DungeonLandingEntryTracker.End();
        }

        internal static void OnSessionEnded()
        {
            DungeonLandingEntryTracker.End();
            ClearPreload();
        }

        private static void EnsurePreloaded()
        {
            string fingerprint = BuildPreloadFingerprint();
            if (string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal)
                && ClipCache.HasCachedClips)
            {
                return;
            }

            PreloadVariants();
            _preloadedFingerprint = fingerprint;
        }

        private static void ClearPreload()
        {
            ClipCache.Clear();
            _preloadedFingerprint = null;
        }

        private static void PreloadVariants()
        {
            foreach (string fileName in RoundStartSoundResolver.ListVariantFileNames())
            {
                _ = ClipCache.TryPreloadClip(fileName);
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
