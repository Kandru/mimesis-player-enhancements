namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Parses comma-separated lists of positive integer master IDs from config strings.
    /// Invalid tokens are skipped with a debug log entry.
    /// </summary>
    internal static class CsvIdSetParser
    {
        internal static HashSet<int> Parse(string? csv, string logFeature, string tokenLabel)
        {
            HashSet<int> ids = [];
            if (string.IsNullOrWhiteSpace(csv))
            {
                return ids;
            }

            foreach (string token in csv.Split(','))
            {
                string trimmed = token.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (int.TryParse(trimmed, out int id) && id > 0)
                {
                    _ = ids.Add(id);
                }
                else
                {
                    ModLog.Debug(logFeature, $"Ignoring invalid {tokenLabel} token: '{trimmed}'");
                }
            }

            return ids;
        }
    }
}
