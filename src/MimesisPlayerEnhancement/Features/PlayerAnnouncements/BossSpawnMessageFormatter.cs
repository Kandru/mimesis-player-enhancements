namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class BossSpawnMessageFormatter
    {
        internal static string Format(
            IReadOnlyDictionary<int, int> spawns,
            Func<int, string> resolveDisplayName)
        {
            List<string> segments = [];
            foreach (KeyValuePair<int, int> kvp in spawns)
            {
                string humanizedName = resolveDisplayName(kvp.Key);
                string segment = FormatSegment(kvp.Value, humanizedName, capitalizeArticle: segments.Count == 0);
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    segments.Add(segment);
                }
            }

            if (segments.Count == 0)
            {
                return "";
            }

            string joined = segments.Count switch
            {
                1 => segments[0],
                2 => ModL10n.Get("announce.spawn_join_two", new Dictionary<string, object>
                {
                    ["first"] = segments[0],
                    ["second"] = segments[1],
                }),
                _ => ModL10n.Get("announce.spawn_join_many", new Dictionary<string, object>
                {
                    ["rest"] = string.Join(ModL10n.Get("announce.spawn_join_comma"), segments.GetRange(0, segments.Count - 1)),
                    ["last"] = segments[^1],
                }),
            };

            return ModL10n.Get("announce.spawn_appeared", new Dictionary<string, object> { ["entities"] = joined });
        }

        private static string FormatSegment(int count, string humanizedName, bool capitalizeArticle)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(humanizedName))
            {
                return "";
            }

            return count == 1
                ? EntityDisplayNameFormatter.FormatWithArticle(humanizedName, capitalizeArticle)
                : $"{count} {EntityDisplayNameFormatter.Pluralize(humanizedName)}";
        }
    }
}
