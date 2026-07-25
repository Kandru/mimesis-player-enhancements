using System.IO;

namespace MimesisPlayerEnhancement.Util
{
    internal sealed class EmbeddedAudioVariantCatalog
    {
        private static readonly string[] AudioExtensions = [".wav", ".ogg"];
        private static readonly Random RandomSource = new();

        private readonly string _assetFolder;
        private readonly string _featureTag;
        private readonly string _variantNoun;

        internal EmbeddedAudioVariantCatalog(string assetFolder, string featureTag, string variantNoun)
        {
            _assetFolder = assetFolder;
            _featureTag = featureTag;
            _variantNoun = variantNoun;
        }

        internal IReadOnlyList<string> ListVariantFileNames()
        {
            List<string> files = [];
            foreach (string fileName in EmbeddedAssets.ListFeatureFiles(_assetFolder))
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

        internal IReadOnlyList<string> ListVariantOptionValues()
        {
            List<string> options = [];
            foreach (string fileName in ListVariantFileNames())
            {
                options.Add(Path.GetFileNameWithoutExtension(fileName));
            }

            return options;
        }

        internal string GetDefaultVariantOptionValue()
        {
            IReadOnlyList<string> options = ListVariantOptionValues();
            return options.Count > 0 ? options[0] : "";
        }

        internal string NormalizeVariantOptionValue(string? value)
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

        internal string NormalizeRandomPoolValue(string? value)
        {
            return VariantIdListParser.NormalizeCsv(
                value,
                ListVariantOptionValues(),
                _featureTag,
                _variantNoun);
        }

        internal string ResolveRandomVariant(string? poolCsv)
        {
            IReadOnlyList<string> variants = ListVariantFileNames();
            if (variants.Count == 0)
            {
                return "";
            }

            List<string> pool = VariantIdListParser.ParseOrdered(
                poolCsv,
                ListVariantOptionValues(),
                _featureTag,
                _variantNoun);
            IReadOnlyList<string> source = FilterVariants(variants, pool);
            return source[RandomSource.Next(source.Count)];
        }

        internal string? ResolveSpecificVariant(string configuredValue)
        {
            IReadOnlyList<string> variants = ListVariantFileNames();
            if (variants.Count == 0)
            {
                return null;
            }

            string configured = configuredValue?.Trim() ?? "";
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

        private static IReadOnlyList<string> FilterVariants(IReadOnlyList<string> variants, List<string> pool)
        {
            if (pool.Count == 0)
            {
                return variants;
            }

            List<string> filtered = [];
            HashSet<string> poolSet = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pool.Count; i++)
            {
                _ = poolSet.Add(pool[i]);
            }

            for (int i = 0; i < variants.Count; i++)
            {
                string fileName = variants[i];
                string stem = Path.GetFileNameWithoutExtension(fileName);
                if (poolSet.Contains(fileName) || poolSet.Contains(stem))
                {
                    filtered.Add(fileName);
                }
            }

            return filtered.Count > 0 ? filtered : variants;
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
