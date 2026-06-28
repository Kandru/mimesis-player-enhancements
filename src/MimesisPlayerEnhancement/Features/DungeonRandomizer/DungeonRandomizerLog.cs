namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonRandomizerLog
{
    private const string Feature = "DungeonRandomizer";

    internal static void Debug(string message)
    {
        if (ModConfig.EnableDebugLogging.Value)
            ModLog.Debug(Feature, message);
    }

    internal static void Warn(string message) => ModLog.Warn(Feature, message);
}
