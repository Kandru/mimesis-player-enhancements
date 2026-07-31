namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    internal static class SavegamePreparationLog
    {
        private const string Feature = "SavegamePreparation";

        internal static void InfoStartingZoneApplied(int zone)
        {
            ModLog.Info(Feature, $"Starting zone applied on new save — zone={zone}");
        }

        internal static void InfoStartupMoneyApplied(int vanilla, int scaled, int playerCount, float effectiveMultiplier)
        {
            ModLog.Info(
                Feature,
                $"Startup money applied on new save — vanilla={vanilla}, scaled={scaled}, players={playerCount}, multiplier={effectiveMultiplier:0.###}");
        }
    }
}
