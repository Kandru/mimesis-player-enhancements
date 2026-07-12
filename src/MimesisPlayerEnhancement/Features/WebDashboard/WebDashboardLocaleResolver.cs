using System.Globalization;
using System.Net;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardLocaleResolver
    {
        internal static string ResolveFromRequest(HttpListenerRequest request)
        {
            return ResolveAcceptLanguage(request.Headers["Accept-Language"]);
        }

        internal static string ResolveAcceptLanguage(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return "en";
            }

            List<LanguagePreference> preferences = [];
            int order = 0;
            foreach (string part in header.Split(','))
            {
                string trimmed = part.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                string tag = trimmed;
                double quality = 1.0;
                int semicolon = trimmed.IndexOf(';', StringComparison.Ordinal);
                if (semicolon >= 0)
                {
                    tag = trimmed[..semicolon].Trim();
                    string qPart = trimmed[(semicolon + 1)..].Trim();
                    if (qPart.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                        && double.TryParse(
                            qPart[2..],
                            NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture,
                            out double parsed))
                    {
                        quality = parsed;
                    }
                }

                if (quality <= 0 || string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                preferences.Add(new LanguagePreference(tag, quality, order++));
            }

            if (preferences.Count == 0)
            {
                return "en";
            }

            preferences.Sort((a, b) =>
            {
                int byQuality = b.Quality.CompareTo(a.Quality);
                return byQuality != 0 ? byQuality : a.Order.CompareTo(b.Order);
            });

            foreach (LanguagePreference preference in preferences)
            {
                if (GameLocaleAccess.TryResolveSupportedLocale(preference.Tag, out string locale))
                {
                    return locale;
                }
            }

            return "en";
        }

        private readonly struct LanguagePreference
        {
            internal readonly string Tag;
            internal readonly double Quality;
            internal readonly int Order;

            internal LanguagePreference(string tag, double quality, int order)
            {
                Tag = tag;
                Quality = quality;
                Order = order;
            }
        }
    }
}
