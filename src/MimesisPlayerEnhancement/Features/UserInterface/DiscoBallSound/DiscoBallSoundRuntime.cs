using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundRuntime
    {
        private static readonly EmbeddedAudioClipCache ClipCache = new(
            DiscoBallSoundConstants.AssetFolder,
            DiscoBallSoundConstants.Feature,
            DiscoBallSoundConstants.AssetFolder);

        private static string? _preloadedFingerprint;

        internal static AudioClip? TryGetCachedClip(string fileName) => ClipCache.TryGetCachedClip(fileName);

        internal static void RefreshFromConfig()
        {
            List<PartyButtonLevelObject> toRearm = FindActivePartyButtons();

            if (!DiscoBallSoundResolver.ShouldApplyReplacement())
            {
                // Stop sources before destroying clips — Destroy on a playing clip freezes Unity.
                DiscoBallSoundPlayer.StopAll();
                ClearPreload();
                DiscoBallSoundSession.ClearStickyVariant();
                RearmPartyButtons(toRearm);
                return;
            }

            string fingerprint = BuildPreloadFingerprint();
            if (!string.Equals(fingerprint, _preloadedFingerprint, StringComparison.Ordinal))
            {
                DiscoBallSoundSession.ClearStickyVariant();
                DiscoBallSoundPlayer.StopAll();
                // Keep already-decoded clips — clearing + re-decoding on the main thread freezes the game.
                PreloadVariants();
                _preloadedFingerprint = fingerprint;
                RearmPartyButtons(toRearm);
                return;
            }

            EnsurePreloaded();
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

        internal static void OnPlaySceneDestroyed()
        {
            DiscoBallSoundSession.ClearStickyVariant();
            DiscoBallSoundPlayer.StopAll();
        }

        internal static void OnSessionEnded()
        {
            DiscoBallSoundSession.ClearStickyVariant();
            DiscoBallSoundPlayer.StopAll();
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
            foreach (string fileName in DiscoBallSoundResolver.ListVariantFileNames())
            {
                _ = ClipCache.TryPreloadClip(fileName);
            }
        }

        private static List<PartyButtonLevelObject> FindActivePartyButtons()
        {
            List<PartyButtonLevelObject> buttons = [];
            PartyButtonLevelObject[] sceneButtons =
                UnityEngine.Object.FindObjectsByType<PartyButtonLevelObject>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                PartyButtonLevelObject button = sceneButtons[i];
                if (button != null && button.IsOn)
                {
                    buttons.Add(button);
                }
            }

            return buttons;
        }

        private static void RearmPartyButtons(List<PartyButtonLevelObject> buttons)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                PartyButtonLevelObject button = buttons[i];
                if (button == null || !button.IsOn)
                {
                    continue;
                }

                try
                {
                    button.SetPartyState(true);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(
                        DiscoBallSoundConstants.Feature,
                        $"Disco ball sound re-arm failed — {ex.Message}");
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
