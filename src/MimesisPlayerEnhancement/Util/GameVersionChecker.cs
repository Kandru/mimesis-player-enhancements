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
                ClearAcknowledgmentIfPresent();
                ModLog.Debug(Feature, $"Game version matches — {detected}");
                return true;
            }

            ModLog.Warn(Feature, $"Game version mismatch — detected={detected}, expected={expected}");

            if (IsAlreadyAcknowledged(detected))
            {
                ModLog.Warn(Feature, "Plugin load refused — game version mismatch (already notified).");
                return false;
            }

            ShowMismatchDialog(detected, expected);
            ModConfig.AcknowledgedMismatchGameVersion.Value = detected;
            ModConfig.SaveToFile();
            ModLog.Warn(Feature, "Plugin load refused — game version mismatch.");
            return false;
        }

        private static void ShowMismatchDialog(string detected, string expected)
        {
            string caption = "Mimesis Player Enhancement — Game Version Mismatch";
            string message =
                $"This plugin (v{VersionInfo.ModuleVersion}) was built for MIMESIS {expected}.\r\n\r\n" +
                $"Your game reports version {detected}.\r\n\r\n" +
                "The plugin will be disabled and will not load until the game version matches again.\r\n\r\n" +
                $"Update the game or install a matching plugin release:\r\n" +
                $"{VersionInfo.ReleasesUrl}";

            if (NativeMessageBox.TryShowWarning(caption, message))
            {
                return;
            }

            ModLog.Warn(Feature, message.Replace("\r\n", " — "));
        }

        private static bool IsAlreadyAcknowledged(string detected)
        {
            return VersionsMatch(Normalize(ModConfig.AcknowledgedMismatchGameVersion.Value), detected);
        }

        private static void ClearAcknowledgmentIfPresent()
        {
            if (string.IsNullOrEmpty(ModConfig.AcknowledgedMismatchGameVersion.Value))
            {
                return;
            }

            ModConfig.AcknowledgedMismatchGameVersion.Value = string.Empty;
            ModConfig.SaveToFile();
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
