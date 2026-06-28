using MimesisPlayerEnhancement.Features.Statistics;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements;

internal static class PlayerAnnouncements
{
    private const string Feature = "Announcements";

    internal static void OnAllMembersEnteredDungeon(DungeonRoom room)
    {
        if (!ModConfig.ShowPlayerAnnouncements.Value)
            return;

        MapRunStatsTracker.ResetForDungeonEntry();
        BossSpawnAnnouncer.BeginDungeonRun();

        string? settings = DungeonSettingsFormatter.FormatForDungeonEntry(room);
        if (!string.IsNullOrWhiteSpace(settings))
            ShowToast(settings);
    }

    internal static void ShowToast(string message, bool isEntering = true, bool localOnly = false)
    {
        if (!ModConfig.ShowPlayerAnnouncements.Value)
            return;

        InGameMessageHelper.ShowModMessage(message, isEntering, localOnly);
        ModLog.Debug(Feature, localOnly ? $"Local toast: {message}" : $"Toast: {message}");
    }
}
