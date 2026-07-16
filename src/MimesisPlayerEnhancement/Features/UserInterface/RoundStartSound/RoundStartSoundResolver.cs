using System.IO;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundResolver
    {
        private static readonly string[] AudioExtensions = [".wav", ".ogg"];
        private static readonly Random RandomSource = new();

        internal static RoundStartSoundMode GetMode()
        {
            if (!ModConfig.IsInitialized)
            {
                return RoundStartSoundMode.Vanilla;
            }

            return ParseMode(ModConfig.RoundStartSoundMode.Value);
        }

        internal static bool ShouldApplyReplacement() => GetMode() != RoundStartSoundMode.Vanilla;

        internal static string? ResolveVariantFileName()
        {
            IReadOnlyList<string> variants = ListVariantFileNames();
            if (variants.Count == 0)
            {
                return null;
            }

            RoundStartSoundMode mode = GetMode();
            return mode switch
            {
                RoundStartSoundMode.Specific => ResolveSpecificVariant(variants),
                RoundStartSoundMode.Random => variants[RandomSource.Next(variants.Count)],
                _ => null,
            };
        }

        internal static IReadOnlyList<string> ListVariantFileNames()
        {
            List<string> files = [];
            foreach (string fileName in EmbeddedAssets.ListFeatureFiles(RoundStartSoundConstants.AssetFolder))
            {
                if (!IsAudioFile(fileName))
                {
                    continue;
                }

                files.Add(fileName);
            }

            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        internal static IReadOnlyList<string> ListVariantOptionValues()
        {
            List<string> options = [];
            foreach (string fileName in ListVariantFileNames())
            {
                options.Add(Path.GetFileNameWithoutExtension(fileName));
            }

            return options;
        }

        internal static string GetDefaultVariantOptionValue()
        {
            IReadOnlyList<string> options = ListVariantOptionValues();
            return options.Count > 0 ? options[0] : "";
        }

        internal static string NormalizeVariantOptionValue(string? value)
        {
            IReadOnlyList<string> options = ListVariantOptionValues();
            if (options.Count == 0)
            {
                return value?.Trim() ?? "";
            }

            string trimmed = value?.Trim() ?? "";
            if (string.IsNullOrEmpty(trimmed))
            {
                return options[0];
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i], trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return options[i];
                }
            }

            return options[0];
        }

        internal static string FormatVariantDisplayName(string optionValue)
        {
            if (string.IsNullOrWhiteSpace(optionValue))
            {
                return optionValue;
            }

            string[] parts = optionValue.Replace('_', ' ').Split(
                [' '],
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return optionValue;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = ToTitleCaseWord(parts[i]);
            }

            return string.Join(' ', parts);
        }

        private static string ToTitleCaseWord(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return word;
            }

            if (word.Length == 1)
            {
                return word.ToUpperInvariant();
            }

            return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
        }

        private static string? ResolveSpecificVariant(IReadOnlyList<string> variants)
        {
            string configured = ModConfig.RoundStartSoundVariant.Value?.Trim() ?? "";
            if (string.IsNullOrEmpty(configured))
            {
                return variants[0];
            }

            for (int i = 0; i < variants.Count; i++)
            {
                string fileName = variants[i];
                if (string.Equals(fileName, configured, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileNameWithoutExtension(fileName), configured, StringComparison.OrdinalIgnoreCase))
                {
                    return fileName;
                }
            }

            return variants[0];
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

        private static bool IsAudioFile(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            for (int i = 0; i < AudioExtensions.Length; i++)
            {
                if (string.Equals(extension, AudioExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
