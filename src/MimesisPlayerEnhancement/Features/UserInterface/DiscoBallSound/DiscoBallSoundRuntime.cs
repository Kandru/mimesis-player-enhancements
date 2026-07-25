using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundRuntime
    {
        private static readonly EmbeddedAudioClipCache ClipCache = new(
            DiscoBallSoundConstants.AssetFolder,
            DiscoBallSoundConstants.Feature,
            DiscoBallSoundConstants.TempSubfolder);

        private static string? _preloadedFingerprint;

        internal static AudioClip? TryGetCachedClip(string fileName) => ClipCache.TryGetCachedClip(fileName);

        internal static void RefreshFromConfig()
        {
            DiscoBallSoundPlayer.Prune();

            if (!DiscoBallSoundResolver.ShouldApplyReplacement())
            {
                ClearPreload();
                DiscoBallSoundSession.ClearStickyVariant();
                DiscoBallSoundPlayer.StopAll();
                return;
            }

            string fingerprint = BuildPreloadFingerprint();
            bool fingerprintChanged = !string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal);

            if (fingerprintChanged)
            {
                DiscoBallSoundSession.ClearStickyVariant();
                ClipCache.Clear();
                PreloadVariants();
                _preloadedFingerprint = fingerprint;
                RearmActiveLoops();
                return;
            }

            if (!ClipCache.HasCachedClips)
            {
                PreloadVariants();
                _preloadedFingerprint = fingerprint;
            }

            DiscoBallSoundPlayer.ApplyVolumeToActiveLoops();
        }

        internal static void OnDungeonEntryBegin()
        {
            DiscoBallSoundSession.ClearStickyVariant();
            if (DiscoBallSoundResolver.ShouldApplyReplacement())
            {
                EnsurePreloaded();
            }
        }

        internal static void OnSessionEnded()
        {
            DiscoBallSoundSession.ClearStickyVariant();
            DiscoBallSoundPlayer.StopAll();
            ClearPreload();
        }

        internal static void Shutdown()
        {
            OnSessionEnded();
        }

        private static void EnsurePreloaded()
        {
            string fingerprint = BuildPreloadFingerprint();
            if (string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal)
                && ClipCache.HasCachedClips)
            {
                return;
            }

            ClipCache.Clear();
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
            foreach (string fileName in DiscoBallSoundResolver.ListVariantFileNames())
            {
                _ = ClipCache.TryPreloadClip(fileName);
            }
        }

        private static void RearmActiveLoops()
        {
            IReadOnlyList<PartyButtonLevelObject> activeButtons = DiscoBallSoundPlayer.GetActiveButtons();
            for (int i = 0; i < activeButtons.Count; i++)
            {
                PartyButtonLevelObject button = activeButtons[i];
                if (button != null)
                {
                    button.SetPartyState(true);
                }
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
                ModConfig.DiscoBallSoundMode.Value ?? "",
                ModConfig.DiscoBallSoundVariant.Value ?? "",
                ModConfig.DiscoBallSoundRandomPool.Value ?? "");
        }
    }
}
