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

        internal static float GetStartupMoneyEffectiveMultiplier(int playerCount)
        {
            return ComputeStartupMoneyEffectiveMultiplier(
                ModConfig.StartupMoneyMultiplier.Value,
                ModConfig.AutoScaleStartupMoneyByPlayerCount.Value,
                ModConfig.EconomyPlayerCountScaleRate.Value,
                playerCount);
        }

        internal static int ScaleStartupMoney(int vanilla, int playerCount)
        {
            float effective = GetStartupMoneyEffectiveMultiplier(playerCount);
            if (effective == FeatureToggleGate.NeutralMultiplier)
            {
                return vanilla;
            }

            return ScalingMath.ScaleCount(vanilla, effective);
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

        internal static float ComputeStartupMoneyEffectiveMultiplier(
            float startupMultiplier,
            bool autoScaleByPlayerCount,
            float economyPlayerCountScaleRate,
            int playerCount)
        {
            if (startupMultiplier == FeatureToggleGate.NeutralMultiplier)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            float playerScale = ScalingMath.GetPlayerScale(
                playerCount,
                autoScaleByPlayerCount,
                economyPlayerCountScaleRate);

            return startupMultiplier * playerScale;
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
