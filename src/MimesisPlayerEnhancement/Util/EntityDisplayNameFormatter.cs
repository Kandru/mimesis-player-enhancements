using System.Text.RegularExpressions;

namespace MimesisPlayerEnhancement.Util
{
    internal static class EntityDisplayNameFormatter
    {
        private static readonly Regex PascalCaseSplitPattern = new(
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            RegexOptions.Compiled);

        internal static string Humanize(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return PascalCaseSplitPattern.Replace(name.Trim(), " ");
        }

        internal static string GetArticle(string humanizedName)
        {
            if (string.IsNullOrWhiteSpace(humanizedName))
            {
                return "a";
            }

            char first = char.ToLowerInvariant(humanizedName[0]);
            return first is 'a' or 'e' or 'i' or 'o' or 'u' ? "an" : "a";
        }

        internal static string FormatWithArticle(string humanizedName, bool capitalizeArticle)
        {
            if (string.IsNullOrWhiteSpace(humanizedName))
            {
                return string.Empty;
            }

            string article = GetArticle(humanizedName);
            if (capitalizeArticle)
            {
                article = char.ToUpperInvariant(article[0]) + article[1..];
            }

            return $"{article} {humanizedName}";
        }

        internal static string Pluralize(string humanizedName)
        {
            if (string.IsNullOrWhiteSpace(humanizedName))
            {
                return string.Empty;
            }

            if (humanizedName.EndsWith("s", System.StringComparison.OrdinalIgnoreCase))
            {
                return humanizedName;
            }

            char last = humanizedName[^1];
            if (last is 'o' or 'O')
            {
                return humanizedName;
            }

            return humanizedName + "s";
        }
    }
}
