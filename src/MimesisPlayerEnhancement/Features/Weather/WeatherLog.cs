namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherLog
    {
        private const string Feature = "Weather";

        internal static void InfoApplied(string summary) =>
            ModLog.Info(Feature, summary);

        internal static void InfoCycleTransition(int index, string presetName, int masterId) =>
            ModLog.Info(Feature, $"Cycle transition — index={index}, preset={presetName}, masterId={masterId}");

        internal static void InfoRestoredVanilla() =>
            ModLog.Info(Feature, "Restored vanilla weather and start time on active dungeon");

        internal static void DebugSkipped(string reason) =>
            ModLog.Debug(Feature, reason);
    }
}
