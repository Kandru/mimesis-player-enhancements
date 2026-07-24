namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenRuntime
    {
        private static string? _lastApplyFingerprint;

        internal static void RefreshFromConfig()
        {
            // Embedded catalog is static; only wipe decoded textures when the active theme pick
            // can change (mode/variant/pool). Motion toggles reuse cached textures.
            string fingerprint = BuildApplyFingerprint();
            if (!string.Equals(fingerprint, _lastApplyFingerprint, StringComparison.Ordinal))
            {
                _lastApplyFingerprint = fingerprint;
                CustomLoadingScreenTextureCache.Clear();
            }

            CustomLoadingScreenApplier.RefreshMotionFromConfig();
            CustomLoadingScreenApplier.ReapplyActivePhaseIfNeeded();
        }

        internal static void OnSessionEnded()
        {
            _lastApplyFingerprint = null;
            CustomLoadingScreenApplier.ForceReset();
        }

        private static string BuildApplyFingerprint()
        {
            if (!ModConfig.IsInitialized)
            {
                return "uninit";
            }

            return string.Join(
                "|",
                ModConfig.CustomLoadingScreenMode.Value ?? "",
                ModConfig.CustomLoadingScreenVariant.Value ?? "",
                ModConfig.CustomLoadingScreenRandomPool.Value ?? "");
        }
    }
}
