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

        private static volatile string _cachedLanguage = "en";
        private static int _mainThreadId;

        internal static bool IsMainThread
        {
            get
            {
                int mainThreadId = _mainThreadId;
                return mainThreadId == 0 || Environment.CurrentManagedThreadId == mainThreadId;
            }
        }

        internal static void CaptureMainThread()
        {
            _mainThreadId = Environment.CurrentManagedThreadId;
            _cachedLanguage = ResolveLanguageFromUnity();
        }

        internal static string GetCurrentLanguage()
        {
            int mainThreadId = _mainThreadId;
            if (mainThreadId != 0 && Environment.CurrentManagedThreadId != mainThreadId)
            {
                return _cachedLanguage;
            }

            if (mainThreadId == 0)
            {
                _mainThreadId = Environment.CurrentManagedThreadId;
            }

            _cachedLanguage = ResolveLanguageFromUnity();
            return _cachedLanguage;
        }

        private static string ResolveLanguageFromUnity()
        {
            try
            {
                L10NManager? manager = UnityEngine.Object.FindAnyObjectByType<L10NManager>();
                if (manager != null
                    && L10NManagerLanguageProperty?.GetValue(manager) is string language
                    && TryResolveSupportedLocale(language, out string locale))
                {
                    return locale;
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
            return TryResolveSupportedLocale(language, out string locale) ? locale : "en";
        }

        internal static bool TryResolveSupportedLocale(string? language, out string locale)
        {
            locale = "en";
            string normalized = NormalizeLanguageTag(language);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            foreach (string available in ModL10n.GetAvailableLocales())
            {
                if (string.Equals(available, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    locale = available;
                    return true;
                }
            }

            return false;
        }

        internal static string GetL10NText(string key, params object[] formattingArgs)
        {
            if (!IsMainThread)
            {
                return key;
            }

            if (GetL10NTextMethod != null)
            {
                return GetL10NTextMethod.Invoke(null, [key, formattingArgs]) as string ?? key;
            }

            return key;
        }

        private static string NormalizeLanguageTag(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return string.Empty;
            }

            string normalized = language.Trim().Replace('_', '-').ToLowerInvariant();
            int dash = normalized.IndexOf('-', StringComparison.Ordinal);
            if (dash > 0)
            {
                normalized = normalized[..dash];
            }

            return normalized;
        }
    }
}
