using System;
using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    internal static class GameLocaleAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly MethodInfo? GetL10NTextMethod =
            AccessTools.Method(typeof(Hub), "GetL10NText", [typeof(string), typeof(object[])]);

        private static readonly PropertyInfo? L10NManagerLanguageProperty =
            typeof(L10NManager).GetProperty("language", InstanceFlags);

        private static readonly HashSet<string> SupportedModLocales = new(StringComparer.OrdinalIgnoreCase)
        {
            "en",
            "de",
        };

        internal static string GetCurrentLanguage()
        {
            try
            {
                L10NManager? manager = UnityEngine.Object.FindAnyObjectByType<L10NManager>();
                if (manager != null
                    && L10NManagerLanguageProperty?.GetValue(manager) is string language
                    && !string.IsNullOrWhiteSpace(language))
                {
                    return NormalizeLanguageCode(language);
                }
            }
            catch
            {
                /* ignore */
            }

            return "en";
        }

        internal static string NormalizeLanguageCode(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return "en";
            }

            string normalized = language.Trim().Replace('_', '-').ToLowerInvariant();
            int dash = normalized.IndexOf('-', StringComparison.Ordinal);
            if (dash > 0)
            {
                normalized = normalized[..dash];
            }

            return SupportedModLocales.Contains(normalized) ? normalized : "en";
        }

        internal static string GetL10NText(string key, params object[] formattingArgs)
        {
            if (GetL10NTextMethod != null)
            {
                return GetL10NTextMethod.Invoke(null, [key, formattingArgs]) as string ?? key;
            }

            return key;
        }
    }
}
