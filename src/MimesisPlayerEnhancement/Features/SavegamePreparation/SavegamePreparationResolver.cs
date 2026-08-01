namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    internal static class SavegamePreparationResolver
    {
        private const int ConfigStartingZoneMax = 99;

        /// <summary>Masterdata TramHorn.</summary>
        internal const int TramUpgradeIdHorn = 1;

        /// <summary>Masterdata ScrapScanner.</summary>
        internal const int TramUpgradeIdScrapScanner = 2;

        /// <summary>Masterdata TramBooster.</summary>
        internal const int TramUpgradeIdBooster = 3;

        /// <summary>Masterdata DiscoBall (EN: Tram light).</summary>
        internal const int TramUpgradeIdLight = 5;

        internal static int ResolveStartingZone()
        {
            int cap = ShouldOverrideMaxStageCount()
                ? ConfigStartingZoneMax
                : GetMaxStageCount();
            return ClampStartingZone(ModConfig.StartingZone.Value, cap);
        }

        internal static bool ShouldApplyStartingZone()
        {
            return HostApplyGate.ShouldApplyHostOnlyFeature()
                && ResolveStartingZone() > 1;
        }

        internal static int ResolveStartupMoney()
        {
            int configured = ModConfig.StartupMoney.Value;
            return configured < 0 ? 0 : configured;
        }

        internal static bool ShouldOverrideMaxStageCount()
        {
            return ModConfig.EnableMorePlayers.Value
                && ModConfig.OverrideMaxStageCount.Value;
        }

        internal static int ClampStartingZone(int requested, int maxStageCount)
        {
            if (requested < 1)
            {
                return 1;
            }

            int cap = maxStageCount < 1 ? ConfigStartingZoneMax : maxStageCount;
            return Math.Min(requested, cap);
        }

        internal static List<int> MapStartupTramUpgradeIds(
            bool horn,
            bool scrapScanner,
            bool booster,
            bool light)
        {
            List<int> ids = [];
            if (horn)
            {
                ids.Add(TramUpgradeIdHorn);
            }

            if (scrapScanner)
            {
                ids.Add(TramUpgradeIdScrapScanner);
            }

            if (booster)
            {
                ids.Add(TramUpgradeIdBooster);
            }

            if (light)
            {
                ids.Add(TramUpgradeIdLight);
            }

            return ids;
        }

        internal static List<int> ResolveStartupTramUpgradeIds()
        {
            List<int> configured = MapStartupTramUpgradeIds(
                ModConfig.EnableUpgradeTramHorn.Value,
                ModConfig.EnableUpgradeScrapScanner.Value,
                ModConfig.EnableUpgradeTramBooster.Value,
                ModConfig.EnableUpgradeTramLight.Value);

            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return configured;
            }

            List<int> usable = [];
            foreach (int id in configured)
            {
                if (excel.IsTramUpgradeUsable(id))
                {
                    usable.Add(id);
                    continue;
                }

                SavegamePreparationLog.WarnTramUpgradeNotUsable(id);
            }

            return usable;
        }

        internal static bool ShouldApplyStartupTramUpgrades()
        {
            return HostApplyGate.ShouldApplyHostOnlyFeature()
                && ResolveStartupTramUpgradeIds().Count > 0;
        }

        private static int GetMaxStageCount()
        {
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return ConfigStartingZoneMax;
            }

            return excel.MaxStageCount < 1 ? ConfigStartingZoneMax : excel.MaxStageCount;
        }
    }
}
