using System.IO;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenResolver
    {
        private static readonly string[] ImageExtensions = [".png"];
        private static readonly Random RandomSource = new();
        private static readonly Dictionary<string, HashSet<string>> ThemesByContextFolder = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> AllThemeNames = new(StringComparer.OrdinalIgnoreCase);
        private static bool _catalogBuilt;

        internal static CustomLoadingScreenMode GetMode()
        {
            if (!ModConfig.IsInitialized)
            {
                return CustomLoadingScreenMode.Vanilla;
            }

            return ParseMode(ModConfig.CustomLoadingScreenMode.Value);
        }

        internal static bool ShouldApplyReplacement() => GetMode() != CustomLoadingScreenMode.Vanilla;

        internal static string? ResolveThemeForContext(CustomLoadingScreenContext context)
        {
            EnsureCatalog();
            string contextFolder = CustomLoadingScreenContextUtil.ToFolderName(context);
            if (!ThemesByContextFolder.TryGetValue(contextFolder, out HashSet<string>? themes) || themes.Count == 0)
            {
                return null;
            }

            List<string> themeList = [.. themes];
            themeList.Sort(StringComparer.OrdinalIgnoreCase);

            return GetMode() switch
            {
                CustomLoadingScreenMode.Random => ResolveRandomTheme(themeList),
                CustomLoadingScreenMode.Specific => ResolveSpecificTheme(themeList),
                _ => null,
            };
        }

        private static string ResolveRandomTheme(List<string> themeList)
        {
            List<string> filtered = FilterThemesForRandomPool(themeList);
            return filtered[RandomSource.Next(filtered.Count)];
        }

        private static List<string> ParseRandomPool()
        {
            return VariantIdListParser.ParseOrdered(
                ModConfig.CustomLoadingScreenRandomPool.Value,
                ListVariantOptionValues(),
                CustomLoadingScreenConstants.Feature,
                "loading screen theme");
        }

        internal static string NormalizeRandomPoolValue(string? value)
        {
            return VariantIdListParser.NormalizeCsv(
                value,
                ListVariantOptionValues(),
                CustomLoadingScreenConstants.Feature,
                "loading screen theme");
        }

        private static List<string> FilterThemesForRandomPool(List<string> themeList)
        {
            List<string> pool = ParseRandomPool();
            if (pool.Count == 0)
            {
                return themeList;
            }

            HashSet<string> poolSet = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pool.Count; i++)
            {
                _ = poolSet.Add(pool[i]);
            }

            List<string> filtered = [];
            for (int i = 0; i < themeList.Count; i++)
            {
                if (poolSet.Contains(themeList[i]))
                {
                    filtered.Add(themeList[i]);
                }
            }

            return filtered.Count > 0 ? filtered : themeList;
        }

        internal static string? ResolveImageRelativePath(
            CustomLoadingScreenContext context,
            string theme,
            CustomLoadingScreenPhase phase)
        {
            string contextFolder = CustomLoadingScreenContextUtil.ToFolderName(context);
            string prefix = $"{contextFolder}/{theme}/";

            if (context == CustomLoadingScreenContext.Dungeon)
            {
                if (phase == CustomLoadingScreenPhase.Loading)
                {
                    string loadingPath = prefix + CustomLoadingScreenConstants.LoadingImageFile;
                    if (AssetExists(loadingPath))
                    {
                        return loadingPath;
                    }
                }
                else
                {
                    string waitPath = prefix + CustomLoadingScreenConstants.WaitImageFile;
                    if (AssetExists(waitPath))
                    {
                        return waitPath;
                    }
                }
            }

            string backgroundPath = prefix + CustomLoadingScreenConstants.BackgroundImageFile;
            if (AssetExists(backgroundPath))
            {
                return backgroundPath;
            }

            if (context == CustomLoadingScreenContext.Dungeon && phase == CustomLoadingScreenPhase.Wait)
            {
                string loadingPath = prefix + CustomLoadingScreenConstants.LoadingImageFile;
                if (AssetExists(loadingPath))
                {
                    return loadingPath;
                }
            }

            return null;
        }

        internal static IReadOnlyList<string> ListVariantOptionValues()
        {
            EnsureCatalog();
            List<string> options = [.. AllThemeNames];
            options.Sort(StringComparer.OrdinalIgnoreCase);
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

        internal static void InvalidateCatalog()
        {
            _catalogBuilt = false;
            ThemesByContextFolder.Clear();
            AllThemeNames.Clear();
        }

        private static void EnsureCatalog()
        {
            if (_catalogBuilt)
            {
                return;
            }

            _catalogBuilt = true;
            foreach (string resourcePath in EmbeddedAssets.ListFeatureFiles(CustomLoadingScreenConstants.AssetFolder))
            {
                if (!TryParseAssetPath(resourcePath, out string contextFolder, out string theme, out _))
                {
                    continue;
                }

                if (!ThemesByContextFolder.TryGetValue(contextFolder, out HashSet<string>? themes))
                {
                    themes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    ThemesByContextFolder[contextFolder] = themes;
                }

                themes.Add(theme);
                AllThemeNames.Add(theme);
            }
        }

        private static bool TryParseAssetPath(
            string resourcePath,
            out string contextFolder,
            out string theme,
            out string fileName)
        {
            contextFolder = "";
            theme = "";
            fileName = "";

            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            string extension = Path.GetExtension(resourcePath);
            if (!IsImageExtension(extension))
            {
                return false;
            }

            string withoutExtension = resourcePath[..^extension.Length];
            string[] parts = withoutExtension.Split('.');
            if (parts.Length < 3)
            {
                return false;
            }

            contextFolder = parts[0];
            theme = parts[1];
            fileName = string.Join('.', parts, 2, parts.Length - 2) + extension;
            return !string.IsNullOrWhiteSpace(contextFolder)
                   && !string.IsNullOrWhiteSpace(theme)
                   && !string.IsNullOrWhiteSpace(fileName);
        }

        private static string? ResolveSpecificTheme(IReadOnlyList<string> themesForContext)
        {
            string configured = ModConfig.CustomLoadingScreenVariant.Value?.Trim() ?? "";
            if (string.IsNullOrEmpty(configured))
            {
                return themesForContext[0];
            }

            for (int i = 0; i < themesForContext.Count; i++)
            {
                if (string.Equals(themesForContext[i], configured, StringComparison.OrdinalIgnoreCase))
                {
                    return themesForContext[i];
                }
            }

            return null;
        }

        private static bool AssetExists(string relativePath)
        {
            return EmbeddedAssets.TryReadFeature(
                CustomLoadingScreenConstants.AssetFolder,
                relativePath,
                out _,
                out _);
        }

        private static CustomLoadingScreenMode ParseMode(string? value)
        {
            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenMode.Random;
            }

            if (string.Equals(value, "Specific", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenMode.Specific;
            }

            return CustomLoadingScreenMode.Vanilla;
        }

        private static bool IsImageExtension(string extension)
        {
            for (int i = 0; i < ImageExtensions.Length; i++)
            {
                if (string.Equals(extension, ImageExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
    }
}
