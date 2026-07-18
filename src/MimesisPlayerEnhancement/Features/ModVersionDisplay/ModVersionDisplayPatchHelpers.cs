namespace MimesisPlayerEnhancement.Features.ModVersionDisplay
{
    internal static class ModVersionDisplayPatchHelpers
    {
        private const string ModPrefixLead = "MimesisPlayerEnhancement v";

        internal static void PrependModVersion(UIPrefab_MainMenu menu)
        {
            if (menu.UE_versionText == null)
            {
                return;
            }

            PrependModVersion(
                () => menu.UE_versionText.text,
                value => menu.UE_versionText.text = value);
        }

        internal static void PrependModVersion(UIPrefab_InGameMenu menu)
        {
            if (menu.UE_versionText == null)
            {
                return;
            }

            PrependModVersion(
                () => menu.UE_versionText.text,
                value => menu.UE_versionText.text = value);
        }

        private static void PrependModVersion(Func<string> getText, Action<string> setText)
        {
            string current = getText() ?? string.Empty;
            string vanillaText = StripExistingModPrefix(current);
            string prefix = $"{ModPrefixLead}{VersionInfo.ModuleVersion}";
            string target = string.IsNullOrEmpty(vanillaText)
                ? prefix
                : $"{prefix}\n{vanillaText}";

            if (string.Equals(current, target, StringComparison.Ordinal))
            {
                return;
            }

            setText(target);
        }

        private static string StripExistingModPrefix(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string[] lines = text.Split('\n');
            int start = 0;
            while (start < lines.Length
                   && lines[start].StartsWith(ModPrefixLead, StringComparison.Ordinal))
            {
                start++;
            }

            if (start == 0)
            {
                return text;
            }

            if (start >= lines.Length)
            {
                return string.Empty;
            }

            return string.Join("\n", lines, start, lines.Length - start);
        }
    }
}
