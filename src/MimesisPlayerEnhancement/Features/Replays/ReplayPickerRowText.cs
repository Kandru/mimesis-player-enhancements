using ReluReplay.Data;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayPickerRowText
    {
        internal static string Compose(ReplayLibraryEntry entry)
        {
            IReplayHeader header = entry.Header;
            long durationMs = Math.Max(0, header.GetReplayRecordEndTime() - header.GetReplayRecordStartTime());
            string duration = FormatDuration(durationMs);
            string mapInfo = FormatMapInfo(header.GetMapInfos());
            string players = FormatPlayers(header.GetPlayerActorNames());
            string sizeMb = (entry.FileSizeBytes / (1024f * 1024f)).ToString("0.0");
            return $"{entry.FileName}\n{mapInfo}  |  {duration}  |  {sizeMb} MB\n{players}";
        }

        private static string FormatMapInfo(List<int>? mapInfo)
        {
            if (mapInfo == null || mapInfo.Count < 3)
            {
                return "Map ?/?/?";
            }

            return $"Map {mapInfo[0]}/{mapInfo[1]}/{mapInfo[2]}";
        }

        private static string FormatPlayers(List<string>? names)
        {
            if (names == null || names.Count == 0)
            {
                return "Players: —";
            }

            return "Players: " + string.Join(", ", names);
        }

        private static string FormatDuration(long milliseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
            return span.TotalHours >= 1
                ? span.ToString(@"h\:mm\:ss")
                : span.ToString(@"m\:ss");
        }
    }
}
