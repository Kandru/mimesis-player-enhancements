namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardCatalogLabels
    {
        internal static bool IsBrokenGameLabel(string resolved, string nameKey, int masterId, bool rejectItemNameTokens = false)
        {
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return true;
            }

            if (resolved.Contains("L10N error", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(resolved, nameKey, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(resolved, masterId.ToString(), StringComparison.Ordinal))
            {
                return true;
            }

            if (rejectItemNameTokens
                && resolved.Contains("STRING_ITEM_NAME", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        internal static string HumanizeNameKey(string nameKey)
        {
            string[] parts = nameKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return nameKey;
            }

            string[] words = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                {
                    words[i] = part;
                    continue;
                }

                if (part.Length == 1)
                {
                    words[i] = part.ToUpperInvariant();
                    continue;
                }

                words[i] = char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
            }

            return string.Join(" ", words);
        }
    }
}
