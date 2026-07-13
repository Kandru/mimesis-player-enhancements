using System.Text.RegularExpressions;
using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapTileLabels
    {
        private static readonly Regex TileIdPrefixPattern = new(
            @"^\(\d+\)\s*",
            RegexOptions.Compiled);

        internal static string ResolveTileLabel(Tile tile)
        {
            string? fromPlacement = tile.Placement?.TileSet?.name;
            string? raw = SanitizeRawName(tile.name)
                ?? SanitizeRawName(tile.gameObject?.name)
                ?? SanitizeRawName(fromPlacement);
            return ResolveLabel(raw);
        }

        internal static string ResolveLabel(string? rawName)
        {
            string? sanitized = SanitizeRawName(rawName);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return ResolveLabelForKey("Room");
            }

            return ResolveLabelForKey(NormalizeTileKey(sanitized));
        }

        private static string ResolveLabelForKey(string tileKey)
        {
            string l10nKey = "dashboard.minimap_tile_" + tileKey;
            string locale = GameLocaleAccess.GetCurrentLanguage();
            string translated = ModL10n.GetForLocale(locale, l10nKey);
            if (!string.Equals(translated, l10nKey, StringComparison.Ordinal))
            {
                return translated;
            }

            return tileKey;
        }

        internal static string NormalizeTileKey(string rawName)
        {
            string trimmed = TileIdPrefixPattern.Replace(rawName.Trim(), string.Empty);
            if (trimmed.EndsWith("(Clone)", StringComparison.Ordinal))
            {
                trimmed = trimmed[..^"(Clone)".Length].TrimEnd();
            }

            return trimmed;
        }

        private static string? SanitizeRawName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string trimmed = NormalizeTileKey(name);
            return trimmed.Equals("GameObject", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
        }
    }
}
