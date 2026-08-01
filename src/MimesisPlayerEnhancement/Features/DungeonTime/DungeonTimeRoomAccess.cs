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

            return TimeSpan.TryParse(displayTime, out TimeSpan parsed)
                ? (long)parsed.TotalSeconds
                : 0;
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
