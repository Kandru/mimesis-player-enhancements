namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Parses comma-separated variant ID lists from config strings.
    /// Unknown tokens are skipped with a warning; duplicates are removed while preserving order.
    /// </summary>
    internal static class VariantIdListParser
    {
        internal static List<string> ParseOrdered(
            string? csv,
            IReadOnlyList<string> allowedValues,
            string logFeature,
            string tokenLabel)
        {
            List<string> result = [];
            if (string.IsNullOrWhiteSpace(csv))
            {
                return result;
            }

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string part in csv.Split(','))
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed) || seen.Contains(trimmed))
                {
                    continue;
                }

                string? canonical = FindCanonicalValue(trimmed, allowedValues);
                if (canonical == null)
                {
                    ModLog.Warn(logFeature, $"Unknown {tokenLabel} in list — {trimmed}");
                    continue;
                }

                seen.Add(canonical);
                result.Add(canonical);
            }

            return result;
        }

        internal static string NormalizeCsv(
            string? csv,
            IReadOnlyList<string> allowedValues,
            string logFeature,
            string tokenLabel)
        {
            List<string> parsed = ParseOrdered(csv, allowedValues, logFeature, tokenLabel);
            return string.Join(",", parsed);
        }

        private static string? FindCanonicalValue(string token, IReadOnlyList<string> allowedValues)
        {
            for (int i = 0; i < allowedValues.Count; i++)
            {
                if (string.Equals(allowedValues[i], token, StringComparison.OrdinalIgnoreCase))
                {
                    return allowedValues[i];
                }
            }

            return null;
        }
    }
}
