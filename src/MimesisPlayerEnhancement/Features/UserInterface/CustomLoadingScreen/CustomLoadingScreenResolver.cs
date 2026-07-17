using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenResolver
    {
        private static readonly string[] ImageExtensions = [".png"];
        private static readonly System.Random RandomSource = new();
        private static readonly Dictionary<string, HashSet<string>> ThemesByContextFolder =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> AllThemeNames = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> KnownAssetPaths = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CustomLoadingScreenThemeManifest> ManifestsByTheme =
            new(StringComparer.OrdinalIgnoreCase);
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

        internal static bool IsMotionEnabled()
        {
            if (!ModConfig.IsInitialized)
            {
                return true;
            }

            return ModConfig.CustomLoadingScreenMotion.Value;
        }

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
            themeList = FilterThemesForRandomPool(themeList);

            return GetMode() switch
            {
                CustomLoadingScreenMode.Random => themeList[RandomSource.Next(themeList.Count)],
                CustomLoadingScreenMode.Specific => ResolveSpecificTheme(themeList),
                _ => null,
            };
        }

        internal static CustomLoadingScreenResolvedPhase? ResolvePhasePresentation(
            CustomLoadingScreenContext context,
            string theme,
            CustomLoadingScreenPhase phase)
        {
            return ResolvePhasePresentation(context, theme, phase, allowPhaseFallback: true);
        }

        /// <summary>Wait-phase art only when the theme ships dedicated wait images
        /// (<c>wait.png</c> / <c>wait_NN.png</c> / theme.json wait images). Does not fall back
        /// to loading/background — callers use that to skip the wait crossfade when absent.</summary>
        internal static CustomLoadingScreenResolvedPhase? ResolveDedicatedWaitPresentation(
            CustomLoadingScreenContext context,
            string theme)
        {
            return ResolvePhasePresentation(
                context,
                theme,
                CustomLoadingScreenPhase.Wait,
                allowPhaseFallback: false);
        }

        private static CustomLoadingScreenResolvedPhase? ResolvePhasePresentation(
            CustomLoadingScreenContext context,
            string theme,
            CustomLoadingScreenPhase phase,
            bool allowPhaseFallback)
        {
            EnsureCatalog();
            string contextFolder = CustomLoadingScreenContextUtil.ToFolderName(context);
            CustomLoadingScreenThemeManifest? manifest = ManifestsByTheme.GetValueOrDefault(theme);
            CustomLoadingScreenPhaseManifest? phaseManifest = ResolvePhaseManifest(manifest, context, phase);

            List<string> imagePaths = ResolveImagePaths(
                context,
                contextFolder,
                theme,
                phase,
                phaseManifest,
                allowPhaseFallback);
            if (imagePaths.Count == 0)
            {
                return null;
            }

            CustomLoadingScreenMotionSettings motion = ResolveMotion(manifest?.Motion, phaseManifest?.Motion);
            return new CustomLoadingScreenResolvedPhase
            {
                ImagePaths = imagePaths,
                FrameRate = ClampFrameRate(phaseManifest?.FrameRate ?? manifest?.FrameRate),
                Loop = ParseLoopMode(phaseManifest?.Loop ?? manifest?.Loop),
                Motion = motion,
                BackgroundColor = ParseBackgroundColor(manifest?.BackgroundColor),
            };
        }

        internal static List<string> ParseRandomPool()
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

        internal static string FormatVariantDisplayName(string optionValue) => optionValue;

        internal static void InvalidateCatalog()
        {
            _catalogBuilt = false;
            ThemesByContextFolder.Clear();
            AllThemeNames.Clear();
            KnownAssetPaths.Clear();
            ManifestsByTheme.Clear();
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

        private static void EnsureCatalog()
        {
            if (_catalogBuilt)
            {
                return;
            }

            _catalogBuilt = true;
            foreach (string resourcePath in EmbeddedAssets.ListFeatureFiles(CustomLoadingScreenConstants.AssetFolder))
            {
                if (!TryParseAssetPath(resourcePath, out string theme, out string contextFolder, out string fileName))
                {
                    continue;
                }

                AllThemeNames.Add(theme);

                if (string.IsNullOrEmpty(contextFolder))
                {
                    if (string.Equals(fileName, CustomLoadingScreenConstants.ThemeManifestFile, StringComparison.OrdinalIgnoreCase))
                    {
                        LoadManifest(theme, $"{theme}/{fileName}");
                    }

                    continue;
                }

                string relativePath = $"{theme}/{contextFolder}/{fileName}";
                if (IsImageFile(fileName))
                {
                    _ = KnownAssetPaths.Add(relativePath);
                }

                if (!ThemesByContextFolder.TryGetValue(contextFolder, out HashSet<string>? themes))
                {
                    themes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    ThemesByContextFolder[contextFolder] = themes;
                }

                themes.Add(theme);
            }
        }

        private static void LoadManifest(string theme, string relativePath)
        {
            if (ManifestsByTheme.ContainsKey(theme))
            {
                return;
            }

            if (!EmbeddedAssets.TryReadFeature(
                    CustomLoadingScreenConstants.AssetFolder,
                    relativePath,
                    out byte[] bytes,
                    out _))
            {
                return;
            }

            try
            {
                string json = Encoding.UTF8.GetString(bytes);
                CustomLoadingScreenThemeManifest? manifest =
                    JsonConvert.DeserializeObject<CustomLoadingScreenThemeManifest>(json);
                if (manifest == null)
                {
                    ModLog.Warn(CustomLoadingScreenConstants.Feature,
                        $"Custom loading screen theme.json is empty — {theme}");
                    return;
                }

                ManifestsByTheme[theme] = manifest;
            }
            catch (Exception ex)
            {
                ModLog.Warn(CustomLoadingScreenConstants.Feature,
                    $"Custom loading screen theme.json parse failed — {theme}: {ex.Message}");
            }
        }

        private static List<string> ResolveImagePaths(
            CustomLoadingScreenContext context,
            string contextFolder,
            string theme,
            CustomLoadingScreenPhase phase,
            CustomLoadingScreenPhaseManifest? phaseManifest,
            bool allowPhaseFallback = true)
        {
            string contextPrefix = $"{theme}/{contextFolder}/";
            if (phaseManifest?.Images is { Count: > 0 } explicitImages)
            {
                List<string> explicitPaths = FilterExistingPaths(theme, contextFolder, explicitImages);
                if (explicitPaths.Count > 0)
                {
                    return explicitPaths;
                }
            }

            List<string> primary = ResolveConventionPaths(context, contextPrefix, phase);
            if (primary.Count > 0)
            {
                return primary;
            }

            if (!allowPhaseFallback || context != CustomLoadingScreenContext.DungeonStart)
            {
                return [];
            }

            if (phase == CustomLoadingScreenPhase.Wait)
            {
                List<string> loadingFallback = ResolveConventionPaths(
                    context,
                    contextPrefix,
                    CustomLoadingScreenPhase.Loading);
                if (loadingFallback.Count > 0)
                {
                    return loadingFallback;
                }
            }

            List<string> backgroundFallback = ResolveConventionPaths(
                context,
                contextPrefix,
                CustomLoadingScreenPhase.Background);
            return backgroundFallback.Count > 0 ? backgroundFallback : [];
        }

        private static List<string> ResolveConventionPaths(
            CustomLoadingScreenContext context,
            string contextPrefix,
            CustomLoadingScreenPhase phase)
        {
            string baseName = ResolveConventionBaseName(context, phase);
            List<string> numbered = DiscoverNumberedFrames(contextPrefix, baseName);
            if (numbered.Count > 0)
            {
                return numbered;
            }

            string singlePath = contextPrefix + baseName + ".png";
            return AssetExists(singlePath) ? [singlePath] : [];
        }

        private static string ResolveConventionBaseName(
            CustomLoadingScreenContext context,
            CustomLoadingScreenPhase phase)
        {
            if (phase == CustomLoadingScreenPhase.Background
                || context != CustomLoadingScreenContext.DungeonStart)
            {
                return Path.GetFileNameWithoutExtension(CustomLoadingScreenConstants.BackgroundImageFile);
            }

            return phase == CustomLoadingScreenPhase.Loading
                ? Path.GetFileNameWithoutExtension(CustomLoadingScreenConstants.LoadingImageFile)
                : Path.GetFileNameWithoutExtension(CustomLoadingScreenConstants.WaitImageFile);
        }

        private static List<string> DiscoverNumberedFrames(string contextPrefix, string baseName)
        {
            string searchPrefix = contextPrefix + baseName + "_";
            List<(int Number, string Path)> matches = [];
            foreach (string assetPath in KnownAssetPaths)
            {
                if (!assetPath.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = assetPath[(contextPrefix.Length)..];
                if (!TryParseNumberedFrame(fileName, baseName, out int number))
                {
                    continue;
                }

                matches.Add((number, assetPath));
            }

            matches.Sort((left, right) => left.Number.CompareTo(right.Number));
            List<string> paths = [];
            for (int i = 0; i < matches.Count; i++)
            {
                paths.Add(matches[i].Path);
            }

            return paths;
        }

        private static bool TryParseNumberedFrame(string fileName, string baseName, out int number)
        {
            number = 0;
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string withoutExtension = fileName[..^4];
            string expectedPrefix = baseName + "_";
            if (!withoutExtension.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string suffix = withoutExtension[expectedPrefix.Length..];
            return int.TryParse(suffix, out number);
        }

        private static List<string> FilterExistingPaths(
            string theme,
            string contextFolder,
            IEnumerable<string> images)
        {
            string contextPrefix = $"{theme}/{contextFolder}/";
            List<string> paths = [];
            foreach (string image in images)
            {
                if (string.IsNullOrWhiteSpace(image))
                {
                    continue;
                }

                string trimmed = image.Trim().Replace('\\', '/');
                string relativePath = trimmed.Contains('/', StringComparison.Ordinal)
                    ? $"{theme}/{trimmed}"
                    : contextPrefix + trimmed;
                if (AssetExists(relativePath))
                {
                    paths.Add(relativePath);
                }
            }

            return paths;
        }

        private static CustomLoadingScreenPhaseManifest? ResolvePhaseManifest(
            CustomLoadingScreenThemeManifest? manifest,
            CustomLoadingScreenContext context,
            CustomLoadingScreenPhase phase)
        {
            if (manifest?.Phases == null)
            {
                return null;
            }

            if (context == CustomLoadingScreenContext.DungeonStart)
            {
                return phase == CustomLoadingScreenPhase.Loading
                    ? manifest.Phases.Loading
                    : manifest.Phases.Wait;
            }

            return manifest.Phases.Background ?? manifest.Phases.Loading;
        }

        private static CustomLoadingScreenMotionSettings ResolveMotion(
            CustomLoadingScreenMotionManifest? themeMotion,
            CustomLoadingScreenMotionManifest? phaseMotion)
        {
            CustomLoadingScreenMotionManifest? source = phaseMotion ?? themeMotion;
            if (source == null)
            {
                return CustomLoadingScreenMotionSettings.Default;
            }

            return new CustomLoadingScreenMotionSettings
            {
                Mode = ParseMotionMode(source.Mode),
                Zoom = source.Zoom ?? CustomLoadingScreenConstants.DefaultMotionZoom,
                CycleSeconds = source.CycleSeconds ?? CustomLoadingScreenConstants.DefaultMotionCycleSeconds,
            };
        }

        private static float ClampFrameRate(float? frameRate)
        {
            float value = frameRate ?? CustomLoadingScreenConstants.DefaultFrameRate;
            return Mathf.Clamp(value, CustomLoadingScreenConstants.MinFrameRate, CustomLoadingScreenConstants.MaxFrameRate);
        }

        private static CustomLoadingScreenLoopMode ParseLoopMode(string? value)
        {
            if (string.Equals(value, "pingPong", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenLoopMode.PingPong;
            }

            if (string.Equals(value, "once", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenLoopMode.Once;
            }

            return CustomLoadingScreenLoopMode.Loop;
        }

        private static CustomLoadingScreenMotionMode ParseMotionMode(string? value)
        {
            if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenMotionMode.None;
            }

            return CustomLoadingScreenMotionMode.PanZoom;
        }

        private static Color ParseBackgroundColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Color.black;
            }

            string trimmed = value.Trim();
            if (trimmed.StartsWith('#'))
            {
                trimmed = trimmed[1..];
            }

            if (trimmed.Length != 6 && trimmed.Length != 8)
            {
                return Color.black;
            }

            if (!uint.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, null, out uint rgba))
            {
                return Color.black;
            }

            if (trimmed.Length == 6)
            {
                float r = ((rgba >> 16) & 0xFF) / 255f;
                float g = ((rgba >> 8) & 0xFF) / 255f;
                float b = (rgba & 0xFF) / 255f;
                return new Color(r, g, b, 1f);
            }

            float alpha = ((rgba >> 24) & 0xFF) / 255f;
            float red = ((rgba >> 16) & 0xFF) / 255f;
            float green = ((rgba >> 8) & 0xFF) / 255f;
            float blue = (rgba & 0xFF) / 255f;
            return new Color(red, green, blue, alpha);
        }

        private static bool TryParseAssetPath(
            string resourcePath,
            out string theme,
            out string contextFolder,
            out string fileName)
        {
            theme = "";
            contextFolder = "";
            fileName = "";

            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            string extension = Path.GetExtension(resourcePath);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            string withoutExtension = resourcePath[..^extension.Length];
            string[] parts = withoutExtension.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            theme = parts[0];
            if (parts.Length == 2)
            {
                contextFolder = "";
                fileName = parts[1] + extension;
                return !string.IsNullOrWhiteSpace(theme) && !string.IsNullOrWhiteSpace(fileName);
            }

            contextFolder = parts[1];
            fileName = string.Join('.', parts, 2, parts.Length - 2) + extension;
            return !string.IsNullOrWhiteSpace(theme)
                   && !string.IsNullOrWhiteSpace(contextFolder)
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

        private static bool AssetExists(string relativePath) => KnownAssetPaths.Contains(relativePath);

        private static bool IsImageFile(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            for (int i = 0; i < ImageExtensions.Length; i++)
            {
                if (string.Equals(extension, ImageExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
    }
}
