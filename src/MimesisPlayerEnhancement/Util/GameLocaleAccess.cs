using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    internal static class GameLocaleAccess
    {
        private static readonly MethodInfo? GetL10NTextMethod =
            AccessTools.Method(typeof(Hub), "GetL10NText", [typeof(string), typeof(object[])]);

        // game@0.3.1 Assembly-CSharp/Hub.cs:L344
        private static readonly FieldInfo? HubLcmanField =
            AccessTools.Field(typeof(Hub), "lcman");

        private static volatile string _cachedLanguage = "en";
        private static int _mainThreadId;

        internal static event Action? LanguageChanged;

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

        internal static void NotifyLanguageChanged()
        {
            _cachedLanguage = ResolveLanguageFromUnity();
            LanguageChanged?.Invoke();
        }

        private static string ResolveLanguageFromUnity()
        {
            try
            {
                if (Hub.s != null
                    && HubLcmanField?.GetValue(Hub.s) is L10NManager manager
                    && TryResolveSupportedLocale(manager.language, out string locale))
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

            if (TryMatchAvailable(normalized, out locale))
            {
                return true;
            }

            string prefix = GetLanguagePrefix(normalized);
            return !string.Equals(prefix, normalized, StringComparison.Ordinal)
                   && TryMatchAvailable(prefix, out locale);
        }

        private static bool TryMatchAvailable(string tag, out string locale)
        {
            foreach (string candidate in ModL10n.GetAvailableLocales())
            {
                if (string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase))
                {
                    locale = candidate;
                    return true;
                }
            }

            locale = "en";
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

            return language.Trim().Replace('-', '_').ToLowerInvariant();
        }

        private static string GetLanguagePrefix(string normalized)
        {
            int separator = normalized.IndexOf('_', StringComparison.Ordinal);
            return separator > 0 ? normalized[..separator] : normalized;
        }
    }
}
