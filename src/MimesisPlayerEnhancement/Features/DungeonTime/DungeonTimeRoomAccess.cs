using System.Reflection;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeRoomAccess
    {
        private static readonly FieldInfo DungeonMasterInfoField =
            AccessTools.Field(typeof(DungeonRoom), "_dungeonMasterInfo")
            ?? throw new InvalidOperationException("DungeonRoom._dungeonMasterInfo not found");

        private static readonly FieldInfo StateField =
            AccessTools.Field(typeof(DungeonRoom), "_state")
            ?? throw new InvalidOperationException("DungeonRoom._state not found");

        internal static DungeonTimeRoomState GetOrCreateState(DungeonRoom room) =>
            DungeonTimeRuntime.RoomStates.GetOrCreate(room, () => new DungeonTimeRoomState());

        internal static DungeonMasterInfo? GetDungeonMasterInfo(DungeonRoom room) =>
            DungeonMasterInfoField.GetValue(room) as DungeonMasterInfo;

        internal static bool IsPlaying(DungeonRoom room) =>
            StateField.GetValue(room) is DungeonState.OnPlaying;

        internal static double GetElapsedGameSeconds(DungeonRoom room)
        {
            long elapsedMs = DungeonRoomSessionTime.GetElapsedMilliseconds(room);
            long scaleFactor = HubGameDataAccess.Excel?.Consts.C_GameTimeScaleFactor ?? 1000;
            return elapsedMs * 0.001 * (scaleFactor * 0.001);
        }

        /// <summary>
        /// Parses dungeon display times without calling <see cref="VWorldUtil.ConvertTimeToSeconds"/>,
        /// which is Harmony-patched and must not be invoked reentrantly from that patch.
        /// </summary>
        internal static long ParseDisplayTimeToSeconds(string displayTime)
        {
            if (string.IsNullOrWhiteSpace(displayTime))
            {
                return 0;
            }

            string trimmed = displayTime.Trim();

            // TimeSpan.TryParse("24:00:00") yields 24 days — treat explicit 24:00 as end-of-day.
            if (TryParseClockComponents(trimmed, out int hours, out int minutes, out int seconds))
            {
                if (hours == 24 && minutes == 0 && seconds == 0)
                {
                    return DaySeconds;
                }

                if (hours is >= 0 and < 24 && minutes is >= 0 and < 60 && seconds is >= 0 and < 60)
                {
                    return (hours * 3600L) + (minutes * 60L) + seconds;
                }

                return 0;
            }

            return TimeSpan.TryParse(trimmed, out TimeSpan parsed)
                ? (long)parsed.TotalSeconds
                : 0;
        }

        private const long DaySeconds = 86400L;

        private static bool TryParseClockComponents(
            string displayTime,
            out int hours,
            out int minutes,
            out int seconds)
        {
            hours = 0;
            minutes = 0;
            seconds = 0;
            string[] parts = displayTime.Split(':');
            if (parts.Length is < 2 or > 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out hours) || !int.TryParse(parts[1], out minutes))
            {
                return false;
            }

            if (parts.Length == 3)
            {
                if (!int.TryParse(parts[2], out seconds))
                {
                    return false;
                }
            }

            return true;
        }

        internal static long CaptureVanillaStartSeconds(DungeonRoom room)
        {
            DungeonMasterInfo? info = GetDungeonMasterInfo(room);
            if (info == null || string.IsNullOrEmpty(info.StartDisplayTime))
            {
                return 0;
            }

            return ParseDisplayTimeToSeconds(info.StartDisplayTime);
        }

        internal static long GetVanillaStartSeconds(DungeonRoom room)
        {
            if (DungeonTimeRuntime.TryGetState(room, out DungeonTimeRoomState state)
                && state.VanillaStartSeconds.HasValue)
            {
                return state.VanillaStartSeconds.Value;
            }

            return CaptureVanillaStartSeconds(room);
        }

        internal static long GetEndSeconds(DungeonRoom room)
        {
            DungeonMasterInfo? info = GetDungeonMasterInfo(room);
            if (info == null || string.IsNullOrEmpty(info.EndTime))
            {
                return 0;
            }

            return ParseDisplayTimeToSeconds(info.EndTime);
        }
    }
}
