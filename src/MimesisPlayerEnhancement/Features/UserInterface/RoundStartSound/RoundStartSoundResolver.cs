namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundResolver
    {
        internal const float DefaultVolume = 0.8f;

        private static readonly EmbeddedAudioVariantCatalog Catalog = new(
            RoundStartSoundConstants.AssetFolder,
            RoundStartSoundConstants.Feature,
            "sound variant");

        internal static RoundStartSoundMode GetMode()
        {
            if (!ModConfig.IsInitialized)
            {
                return RoundStartSoundMode.Vanilla;
            }

            return ParseMode(ModConfig.RoundStartSoundMode.Value);
        }

        internal static bool ShouldApplyReplacement() => GetMode() != RoundStartSoundMode.Vanilla;

        internal static float GetVolumeScale()
        {
            if (!ModConfig.IsInitialized)
            {
                return DefaultVolume;
            }

            return UnityEngine.Mathf.Clamp01(ModConfig.RoundStartSoundVolume.Value);
        }

        internal static string? ResolveVariantFileName()
        {
            RoundStartSoundMode mode = GetMode();
            return mode switch
            {
                RoundStartSoundMode.Specific => Catalog.ResolveSpecificVariant(ModConfig.RoundStartSoundVariant.Value),
                RoundStartSoundMode.Random => ResolveRandomVariant(),
                _ => null,
            };
        }

        internal static string NormalizeRandomPoolValue(string? value) => Catalog.NormalizeRandomPoolValue(value);

        internal static IReadOnlyList<string> ListVariantFileNames() => Catalog.ListVariantFileNames();

        internal static IReadOnlyList<string> ListVariantOptionValues() => Catalog.ListVariantOptionValues();

        internal static string GetDefaultVariantOptionValue() => Catalog.GetDefaultVariantOptionValue();

        internal static string NormalizeVariantOptionValue(string? value) => Catalog.NormalizeVariantOptionValue(value);

        internal static string FormatVariantDisplayName(string optionValue) =>
            EmbeddedAudioVariantCatalog.FormatVariantDisplayName(optionValue);

        private static string? ResolveRandomVariant()
        {
            string picked = Catalog.ResolveRandomVariant(ModConfig.RoundStartSoundRandomPool.Value);
            return string.IsNullOrWhiteSpace(picked) ? null : picked;
        }

        private static RoundStartSoundMode ParseMode(string? value)
        {
            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return RoundStartSoundMode.Random;
            }

            if (string.Equals(value, "Specific", StringComparison.OrdinalIgnoreCase))
            {
                return RoundStartSoundMode.Specific;
            }

            return RoundStartSoundMode.Vanilla;
        }
    }
}
