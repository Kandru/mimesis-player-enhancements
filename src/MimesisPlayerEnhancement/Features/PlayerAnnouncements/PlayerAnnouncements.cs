namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class PlayerAnnouncements
    {
        private const string Feature = "Announcements";

        private static readonly HashSet<DungeonRoom> EntryAnnouncedRooms = [];

        internal static void OnAllMembersEnteredDungeon(DungeonRoom room)
        {
            if (!EntryAnnouncedRooms.Add(room))
            {
                return;
            }

            MapRunStatsTracker.ResetForDungeonEntry();
            BossSpawnAnnouncer.BeginDungeonRun();

            if (!ModConfig.ShowPlayerAnnouncements.Value)
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            string? settings = DungeonSettingsFormatter.FormatForDungeonEntry(room);
            if (!string.IsNullOrWhiteSpace(settings))
            {
                ShowToast(settings);
            }
        }

        internal static void ShowToast(string message, bool isEntering = true, bool localOnly = false)
        {
            if (!ModConfig.ShowPlayerAnnouncements.Value)
            {
                return;
            }

            InGameMessageHelper.ShowModMessage(message, isEntering, localOnly);
            ModLog.Debug(Feature, localOnly ? $"Local toast: {message}" : $"Toast: {message}");
        }
    }
}
