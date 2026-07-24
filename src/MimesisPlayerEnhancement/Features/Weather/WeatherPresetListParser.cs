namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherPresetListParser
    {
        private const string Feature = "Weather";

        private static string? _cachedCsv;
        private static List<string>? _cachedPresets;

        internal static void InvalidateCache()
        {
            _cachedCsv = null;
            _cachedPresets = null;
        }

        internal static IReadOnlyList<string> GetOrderedPresets(string? csv)
        {
            if (_cachedPresets != null && string.Equals(_cachedCsv, csv, StringComparison.Ordinal))
            {
                return _cachedPresets;
            }

            _cachedCsv = csv;
            _cachedPresets = ParseOrderedPresets(csv);
            return _cachedPresets;
        }

        internal static List<string> ParseOrderedPresets(string? csv)
        {
            List<string> presets = [];
            if (string.IsNullOrWhiteSpace(csv))
            {
                return presets;
            }

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string part in csv.Split(','))
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed) || seen.Contains(trimmed))
                {
                    continue;
                }

                if (!WeatherResolver.TryParsePresetName(trimmed, out _))
                {
                    ModLog.Warn(Feature, $"Unknown weather preset in cycle list — {trimmed}");
                    continue;
                }

                seen.Add(trimmed);
                presets.Add(trimmed);
            }

            return presets;
        }
    }
}
