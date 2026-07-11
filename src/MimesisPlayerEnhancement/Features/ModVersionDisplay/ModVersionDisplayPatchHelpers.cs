using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ModVersionDisplay
{
    internal static class ModVersionDisplayPatchHelpers
    {
        private const BindingFlags VersionTextBinding = BindingFlags.Instance | BindingFlags.Public;

        internal static void PrependModVersion(object uiPrefab)
        {
            PropertyInfo? versionTextProp = uiPrefab.GetType().GetProperty("UE_versionText", VersionTextBinding);
            object? versionText = versionTextProp?.GetValue(uiPrefab);
            if (versionText == null)
            {
                return;
            }

            PropertyInfo? textProp = versionText.GetType().GetProperty("text");
            if (textProp == null || textProp.PropertyType != typeof(string))
            {
                return;
            }

            string current = textProp.GetValue(versionText) as string ?? string.Empty;
            string prefix = $"MimesisPlayerEnhancement v{VersionInfo.ModuleVersion}";
            if (current.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            textProp.SetValue(
                versionText,
                $"{prefix}\n{current}");
        }
    }
}
