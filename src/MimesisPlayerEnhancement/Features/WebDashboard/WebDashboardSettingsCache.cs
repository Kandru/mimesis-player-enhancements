namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Caches serialized global settings for the dashboard. Config schema is static for a
    /// running session; values refresh after edits via explicit invalidation.
    /// </summary>
    internal static class WebDashboardSettingsCache
    {
        private static string? _globalJson;

        internal static void Invalidate()
        {
            _globalJson = null;
        }

        internal static void WarmGlobal()
        {
            _ = GetGlobalSettingsJson();
        }

        internal static string GetGlobalSettingsJson()
        {
            if (_globalJson != null)
            {
                return _globalJson;
            }

            _globalJson = ModJson.Serialize(WebDashboardConfigBridge.BuildGlobalSettings());
            return _globalJson;
        }
    }
}
