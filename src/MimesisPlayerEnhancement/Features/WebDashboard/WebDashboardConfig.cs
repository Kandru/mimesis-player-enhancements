using MelonLoader;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_WebDashboard] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class WebDashboardConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_WebDashboard", "Web Dashboard");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableWebDashboard = ModConfig.CreateTrackedEntry(_category,
                "EnableWebDashboard",
                true,
                "Enable Web Dashboard",
                "Serve a local web UI for connected players and host moderation. Default bind is loopback only.");

            ModConfig.WebDashboardListenAddress = ModConfig.CreateTrackedEntry(_category,
                "WebDashboardListenAddress",
                "127.0.0.1",
                "Listen Address",
                "HTTP bind address. Use 127.0.0.1 for local-only access.");

            ModConfig.WebDashboardListenPort = ModConfig.CreateTrackedEntry(_category,
                "WebDashboardListenPort",
                8001,
                "Listen Port",
                "TCP port for the local web dashboard.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableWebDashboard.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableWebDashboard));
            ModConfig.WebDashboardListenAddress.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.WebDashboardListenAddress));
            ModConfig.WebDashboardListenPort.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value is < 1 or > 65535)
                {
                    logger.Warning("WebDashboardListenPort must be between 1 and 65535; resetting to 8001.");
                    ModConfig.WebDashboardListenPort.Value = 8001;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.WebDashboardListenPort);
            });
        }
    }
}
