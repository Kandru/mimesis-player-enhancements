using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ModVersionDisplay
{
    internal static class ModVersionDisplayPatchHelpers
    {
        private const string ModPrefixLead = "MimesisPlayerEnhancement v";

        private static readonly PropertyInfo? MainMenuVersionTextProperty =
            AccessTools.Property(typeof(UIPrefab_MainMenu), "UE_versionText");

        private static readonly PropertyInfo? InGameMenuVersionTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGameMenu), "UE_versionText");

        internal static void PrependModVersion(UIPrefab_MainMenu menu)
        {
            PrependModVersion(GetVersionTextComponent(menu, MainMenuVersionTextProperty));
        }

        internal static void PrependModVersion(UIPrefab_InGameMenu menu)
        {
            PrependModVersion(GetVersionTextComponent(menu, InGameMenuVersionTextProperty));
        }

        private static Component? GetVersionTextComponent(object menu, PropertyInfo? versionTextProperty)
        {
            if (versionTextProperty?.GetValue(menu) is Component versionText)
            {
                return versionText;
            }

            return null;
        }

        private static void PrependModVersion(Component? versionText)
        {
            if (versionText == null)
            {
                return;
            }

            string current = ModUiText.GetText(versionText) ?? string.Empty;
            string target = BuildTargetText(current, VersionInfo.ModuleVersion);

            if (string.Equals(current, target, StringComparison.Ordinal))
            {
                return;
            }

            ModUiText.SetText(versionText, target);
        }

        internal static string BuildTargetText(string currentText, string moduleVersion)
        {
            string vanillaText = StripExistingModPrefix(currentText);
            string prefix = $"{ModPrefixLead}{moduleVersion}";
            return string.IsNullOrEmpty(vanillaText)
                ? prefix
                : $"{prefix}\n{vanillaText}";
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
