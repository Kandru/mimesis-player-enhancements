using System;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherPresetListParser
    {
        private const string Feature = "Weather";

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
