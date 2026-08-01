namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    internal static class SavegamePreparationResolver
    {
        private const int ConfigStartingZoneMax = 99;

        internal static int ResolveStartingZone()
        {
            return ClampStartingZone(ModConfig.StartingZone.Value, GetMaxStageCount());
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

        internal static int ClampStartingZone(int requested, int maxStageCount)
        {
            if (requested < 1)
            {
                return 1;
            }

            int cap = maxStageCount < 1 ? ConfigStartingZoneMax : maxStageCount;
            return Math.Min(requested, cap);
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
