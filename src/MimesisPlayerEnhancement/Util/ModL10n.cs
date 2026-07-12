using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace MimesisPlayerEnhancement.Util
{
    internal static class ModL10n
    {
        private const string DefaultLocale = "en";
        private const string LocaleFolder = "Locale";

        private static readonly Regex NamedPlaceholderPattern = new(
            @"\{(\w+)\}",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, JObject> LocaleRoots = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> AvailableLocales = [];
        private static bool _initialized;

        internal static IReadOnlyList<string> GetAvailableLocales()
        {
            EnsureInitialized();
            return AvailableLocales;
        }

        internal static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            foreach (string fileName in EmbeddedAssets.ListFeatureFiles(LocaleFolder))
            {
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string locale = Path.GetFileNameWithoutExtension(fileName);
                if (!string.IsNullOrWhiteSpace(locale))
                {
                    LoadLocale(locale);
                }
            }

            if (!LocaleRoots.ContainsKey(DefaultLocale))
            {
                LoadLocale(DefaultLocale);
            }

            _initialized = true;
        }

        internal static string Get(string key, params object[] args)
        {
            EnsureInitialized();
            return Resolve(key, GameLocaleAccess.GetCurrentLanguage(), args);
        }

        internal static string GetForLocale(string locale, string key, params object[] args)
        {
            EnsureInitialized();
            return Resolve(key, GameLocaleAccess.NormalizeLanguageCode(locale), args);
        }

        internal static string? GetConfigSectionTitle(string sectionId)
        {
            return GetConfigSectionTitle(sectionId, null);
        }

        internal static string? GetConfigSectionTitle(string sectionId, string? locale)
        {
            return GetOptional($"config.{sectionId}._section", locale);
        }

        internal static string? GetConfigEntryTitle(string sectionId, string key)
        {
            return GetConfigEntryTitle(sectionId, key, null);
        }

        internal static string? GetConfigEntryTitle(string sectionId, string key, string? locale)
        {
            return GetOptional($"config.{sectionId}.{key}.title", locale);
        }

        internal static string? GetConfigEntryDescription(string sectionId, string key)
        {
            return GetConfigEntryDescription(sectionId, key, null);
        }

        internal static string? GetConfigEntryDescription(string sectionId, string key, string? locale)
        {
            return GetOptional($"config.{sectionId}.{key}.description", locale);
        }

        internal static string? GetConfigSelectOptionLabel(string sectionId, string key, string value)
        {
            return GetConfigSelectOptionLabel(sectionId, key, value, null);
        }

        internal static string? GetConfigSelectOptionLabel(string sectionId, string key, string value, string? locale)
        {
            return GetOptional($"config.{sectionId}.{key}.options.{value}", locale);
        }

        internal static bool TryGetLocaleJson(string locale, out string json)
        {
            EnsureInitialized();
            json = "";

            locale = GameLocaleAccess.NormalizeLanguageCode(locale);
            if (!LocaleRoots.TryGetValue(locale, out JObject? root) || root == null)
            {
                if (!string.Equals(locale, DefaultLocale, StringComparison.OrdinalIgnoreCase)
                    && LocaleRoots.TryGetValue(DefaultLocale, out root)
                    && root != null)
                {
                    json = root.ToString(Newtonsoft.Json.Formatting.None);
                    return true;
                }

                return false;
            }

            json = root.ToString(Newtonsoft.Json.Formatting.None);
            return true;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static void LoadLocale(string locale)
        {
            if (!EmbeddedAssets.TryReadFeature(LocaleFolder, $"{locale}.json", out byte[] bytes, out _))
            {
                return;
            }

            string json = System.Text.Encoding.UTF8.GetString(bytes);
            JObject? root = ModJson.Deserialize<JObject>(json);
            if (root != null)
            {
                LocaleRoots[locale] = root;
                bool localeKnown = false;
                foreach (string existing in AvailableLocales)
                {
                    if (string.Equals(existing, locale, StringComparison.OrdinalIgnoreCase))
                    {
                        localeKnown = true;
                        break;
                    }
                }

                if (!localeKnown)
                {
                    AvailableLocales.Add(locale);
                }
            }
        }

        private static string Resolve(string key, string locale, object[] args)
        {
            string? template = Lookup(locale, key) ?? Lookup(DefaultLocale, key);
            if (string.IsNullOrEmpty(template))
            {
                return key;
            }

            return Format(template, args);
        }

        private static string? GetOptional(string key)
        {
            return GetOptional(key, null);
        }

        private static string? GetOptional(string key, string? locale)
        {
            EnsureInitialized();
            string resolvedLocale = string.IsNullOrWhiteSpace(locale)
                ? GameLocaleAccess.GetCurrentLanguage()
                : GameLocaleAccess.NormalizeLanguageCode(locale);
            return Lookup(resolvedLocale, key) ?? Lookup(DefaultLocale, key);
        }

        private static string? Lookup(string locale, string key)
        {
            if (!LocaleRoots.TryGetValue(locale, out JObject? root) || root == null)
            {
                return null;
            }

            JToken? token = SelectToken(root, key);
            return token?.Type == JTokenType.String ? token.Value<string>() : null;
        }

        private static JToken? SelectToken(JObject root, string key)
        {
            string[] parts = key.Split('.');
            JToken? current = root;
            foreach (string part in parts)
            {
                if (current is not JObject obj)
                {
                    return null;
                }

                current = obj[part];
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static string Format(string template, object[] args)
        {
            if (args.Length == 0)
            {
                return template;
            }

            if (args.Length == 1 && args[0] is object[] nestedArgs)
            {
                args = nestedArgs;
            }

            if (args.Length == 1 && args[0] is IReadOnlyDictionary<string, object> namedArgs)
            {
                return NamedPlaceholderPattern.Replace(
                    template,
                    match => namedArgs.TryGetValue(match.Groups[1].Value, out object? value)
                        ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                        : match.Value);
            }

            if (args.Length == 1 && args[0] is Dictionary<string, object> mutableNamedArgs)
            {
                return NamedPlaceholderPattern.Replace(
                    template,
                    match => mutableNamedArgs.TryGetValue(match.Groups[1].Value, out object? value)
                        ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                        : match.Value);
            }

            return string.Format(CultureInfo.InvariantCulture, template, args);
        }
    }
}
