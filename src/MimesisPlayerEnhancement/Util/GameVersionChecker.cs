using MelonLoader.InternalUtils;

namespace MimesisPlayerEnhancement
{
    internal static class GameVersionChecker
    {
        private const string Feature = "Startup";

        internal static bool TryAllowLoad()
        {
            string detected = Normalize(UnityInformationHandler.GameVersion);
            string expected = Normalize(VersionInfo.GameVersion);

            if (VersionsMatch(detected, expected))
            {
                ModLog.Debug(Feature, $"Game version matches — {detected}");
                return true;
            }

            ModLog.Error(
                Feature,
                $"Plugin refused to load — game version mismatch — detected={detected}, expected={expected} — " +
                $"plugin v{VersionInfo.ModuleVersion} built for MIMESIS {expected} — " +
                $"install a matching release: {VersionInfo.ReleasesUrl}");

            return false;
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool VersionsMatch(string detected, string expected)
        {
            if (string.IsNullOrEmpty(detected) || string.IsNullOrEmpty(expected))
            {
                return false;
            }

            return string.Equals(detected, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
