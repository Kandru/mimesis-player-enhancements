namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundResolver
    {
        internal const float DefaultVolume = 0.8f;

        private static readonly EmbeddedAudioVariantCatalog Catalog = new(
            DiscoBallSoundConstants.AssetFolder,
            DiscoBallSoundConstants.Feature,
            "sound variant");

        internal static DiscoBallSoundMode GetMode()
        {
            if (!ModConfig.IsInitialized)
            {
                return DiscoBallSoundMode.Vanilla;
            }

            return ParseMode(ModConfig.DiscoBallSoundMode.Value);
        }

        internal static bool ShouldApplyReplacement() => GetMode() != DiscoBallSoundMode.Vanilla;

        internal static float GetVolumeScale()
        {
            float configured = !ModConfig.IsInitialized
                ? DefaultVolume
                : UnityEngine.Mathf.Clamp01(ModConfig.DiscoBallSoundVolume.Value);
            return configured * DiscoBallSoundConstants.PlaybackGain;
        }

        internal static string? ResolveSpecificVariantFileName()
        {
            return Catalog.ResolveSpecificVariant(ModConfig.DiscoBallSoundVariant.Value);
        }

        internal static string? ResolveRandomVariantFileName()
        {
            string picked = Catalog.ResolveRandomVariant(ModConfig.DiscoBallSoundRandomPool.Value);
            return string.IsNullOrWhiteSpace(picked) ? null : picked;
        }

        internal static string NormalizeRandomPoolValue(string? value) => Catalog.NormalizeRandomPoolValue(value);

        internal static IReadOnlyList<string> ListVariantFileNames() => Catalog.ListVariantFileNames();

        internal static IReadOnlyList<string> ListVariantOptionValues() => Catalog.ListVariantOptionValues();

        internal static string GetDefaultVariantOptionValue() => Catalog.GetDefaultVariantOptionValue();

        internal static string NormalizeVariantOptionValue(string? value) => Catalog.NormalizeVariantOptionValue(value);

        internal static string FormatVariantDisplayName(string optionValue) =>
            EmbeddedAudioVariantCatalog.FormatVariantDisplayName(optionValue);

        private static DiscoBallSoundMode ParseMode(string? value)
        {
            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return DiscoBallSoundMode.Random;
            }

            if (string.Equals(value, "Specific", StringComparison.OrdinalIgnoreCase))
            {
                return DiscoBallSoundMode.Specific;
            }

            return DiscoBallSoundMode.Vanilla;
        }
    }
}
