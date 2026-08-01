namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    internal static class SavegamePreparationLog
    {
        private const string Feature = "SavegamePreparation";

        internal static void InfoStartingZoneApplied(int zone)
        {
            ModLog.Info(Feature, $"Starting zone applied on new save — zone={zone}");
        }

        internal static void InfoStartupMoneyApplied(int vanilla, int configured)
        {
            ModLog.Info(
                Feature,
                $"Startup money applied on new save — vanilla={vanilla}, configured={configured}");
        }

        internal static void InfoStartupTramUpgradesApplied(IReadOnlyList<int> ids)
        {
            ModLog.Info(
                Feature,
                $"Startup tram upgrades applied on new save — ids=[{string.Join(",", ids)}]");
        }

        internal static void WarnTramUpgradeNotUsable(int id)
        {
            ModLog.Warn(
                Feature,
                $"Startup tram upgrade skipped — masterdata id={id} is missing or not usable");
        }
    }
}
