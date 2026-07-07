using System.Net;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardRequestLocale
    {
        private static readonly AsyncLocal<string?> CurrentLocale = new();

        internal static string Current => CurrentLocale.Value ?? "en";

        internal static void Set(HttpListenerRequest request)
        {
            CurrentLocale.Value = WebDashboardLocaleResolver.ResolveFromRequest(request);
        }

        internal static void Clear()
        {
            CurrentLocale.Value = null;
        }

        internal static T RunWithLocale<T>(string locale, Func<T> work)
        {
            string? previous = CurrentLocale.Value;
            try
            {
                CurrentLocale.Value = locale;
                return work();
            }
            finally
            {
                CurrentLocale.Value = previous;
            }
        }

        internal static void RunWithLocale(string locale, Action work)
        {
            _ = RunWithLocale(
                locale,
                () =>
                {
                    work();
                    return true;
                });
        }
    }

    internal static class WebDashboardL10n
    {
        internal static string Get(string key, params object[] args) =>
            ModL10n.GetForLocale(WebDashboardRequestLocale.Current, key, args);

        internal static string? GetConfigSectionTitle(string sectionId) =>
            ModL10n.GetConfigSectionTitle(sectionId, WebDashboardRequestLocale.Current);

        internal static string? GetConfigEntryTitle(string sectionId, string key) =>
            ModL10n.GetConfigEntryTitle(sectionId, key, WebDashboardRequestLocale.Current);

        internal static string? GetConfigEntryDescription(string sectionId, string key) =>
            ModL10n.GetConfigEntryDescription(sectionId, key, WebDashboardRequestLocale.Current);

        internal static string? GetConfigSelectOptionLabel(string sectionId, string key, string value) =>
            ModL10n.GetConfigSelectOptionLabel(sectionId, key, value, WebDashboardRequestLocale.Current);
    }
}
