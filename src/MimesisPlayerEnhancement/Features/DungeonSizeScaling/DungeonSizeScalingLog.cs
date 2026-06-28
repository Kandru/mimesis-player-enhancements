namespace MimesisPlayerEnhancement.Features.DungeonSizeScaling;

internal static class DungeonSizeScalingLog
{
    private const string Feature = "DungeonSizeScaling";

    internal static void Debug(string message)
    {
        if (ModConfig.EnableDebugLogging.Value)
            ModLog.Debug(Feature, message);
    }

    internal static void Info(
        int playerCount,
        float appliedScale,
        float previousLengthMultiplier,
        float newLengthMultiplier)
    {
        ModLog.Info(
            Feature,
            $"Dungeon size scaled — players={playerCount}, " +
            $"base={ModConfig.DungeonSizeMultiplier.Value:0.###}×, " +
            $"playerScale={DungeonSizeScalingResolver.GetPlayerScale(playerCount):0.###}× " +
            $"(auto={ModConfig.AutoScaleDungeonSizeByPlayerCount.Value}, baseline={ModConfig.DungeonSizeBaselinePlayerCount.Value}), " +
            $"combined={appliedScale:0.###}×, " +
            $"LengthMultiplier {previousLengthMultiplier:0.###} -> {newLengthMultiplier:0.###}");
    }
}
