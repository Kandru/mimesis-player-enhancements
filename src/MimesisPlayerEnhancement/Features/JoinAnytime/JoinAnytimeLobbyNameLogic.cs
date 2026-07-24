using System.Text.RegularExpressions;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>Pure lobby display-name formatting (no Steam/Unity).</summary>
    internal static class JoinAnytimeLobbyNameLogic
    {
        private static readonly Regex DisplaySuffixPattern = new(
            @"\s*(\[(join now|join in \d+ min|open|wait \d+ min)\])?\s*\(\d+/\d+\)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal const string DefaultBaseLobbyName = "Train";

        internal static string StripDisplaySuffix(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return DisplaySuffixPattern.Replace(value, string.Empty).TrimEnd();
        }

        internal static string BuildDisplayLobbyName(
            string baseLobbyName,
            JoinAnytimeSessionPhase phase,
            int waitMinutes,
            int sessionCount,
            int maxPlayers)
        {
            string baseName = string.IsNullOrWhiteSpace(baseLobbyName)
                ? DefaultBaseLobbyName
                : baseLobbyName.Trim();

            string tag = phase == JoinAnytimeSessionPhase.Dungeon && waitMinutes > 0
                ? $" [join in {waitMinutes} min]"
                : phase is JoinAnytimeSessionPhase.Maintenance
                    or JoinAnytimeSessionPhase.Tram
                    or JoinAnytimeSessionPhase.Dungeon
                    ? " [join now]"
                    : string.Empty;

            return $"{baseName}{tag} ({sessionCount}/{maxPlayers})";
        }

        internal static string ToPhaseKey(JoinAnytimeSessionPhase phase) =>
            phase switch
            {
                JoinAnytimeSessionPhase.Maintenance => JoinAnytimeLobbyMetadata.PhaseMaintenance,
                JoinAnytimeSessionPhase.Tram => JoinAnytimeLobbyMetadata.PhaseTram,
                JoinAnytimeSessionPhase.Dungeon => JoinAnytimeLobbyMetadata.PhaseDungeon,
                _ => string.Empty,
            };
    }
}
