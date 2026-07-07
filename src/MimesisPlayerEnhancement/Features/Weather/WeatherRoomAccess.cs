using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal sealed class WeatherVanillaSnapshot
    {
        internal List<int> WeatherByHour = [];
        internal List<bool> WeatherForecastByHour = [];
        internal bool IsRandomOccured;
        internal long VanillaStartSeconds;
        internal int DayCount;
        internal int RandomSeed;
        internal int OverrideDefaultWeatherId;
    }

    internal sealed class WeatherRoomState
    {
        internal WeatherVanillaSnapshot? VanillaSnapshot;
        internal int CycleIndex;
        internal long NextTransitionTickMs;
        internal bool CycleActive;
    }

    internal static class WeatherRoomAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo WeatherField =
            AccessTools.Field(typeof(DungeonRoom), "_weather")
            ?? throw new InvalidOperationException("DungeonRoom._weather not found");

        private static readonly FieldInfo PrevSyncTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_prevSyncTime")
            ?? throw new InvalidOperationException("DungeonRoom._prevSyncTime not found");

        private static readonly FieldInfo ElapsedTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_elapsedTime")
            ?? throw new InvalidOperationException("DungeonRoom._elapsedTime not found");

        private static readonly FieldInfo DungeonMasterInfoField =
            AccessTools.Field(typeof(DungeonRoom), "_dungeonMasterInfo")
            ?? throw new InvalidOperationException("DungeonRoom._dungeonMasterInfo not found");

        private static readonly FieldInfo StateField =
            AccessTools.Field(typeof(DungeonRoom), "_state")
            ?? throw new InvalidOperationException("DungeonRoom._state not found");

        private static readonly FieldInfo WeatherByHourField =
            AccessTools.Field(typeof(DungeonWeather), "_weatherByHour")
            ?? throw new InvalidOperationException("DungeonWeather._weatherByHour not found");

        private static readonly FieldInfo WeatherForecastByHourField =
            AccessTools.Field(typeof(DungeonWeather), "_weatherForecastByHour")
            ?? throw new InvalidOperationException("DungeonWeather._weatherForecastByHour not found");

        private static readonly FieldInfo IsRandomOccuredField =
            AccessTools.Field(typeof(DungeonWeather), "_isRandomOccured")
            ?? throw new InvalidOperationException("DungeonWeather._isRandomOccured not found");

        internal static readonly DungeonRoomStateRegistry<WeatherRoomState> RoomStates = new();

        internal static WeatherRoomState GetOrCreateState(DungeonRoom room) =>
            RoomStates.GetOrCreate(room, () => new WeatherRoomState());

        internal static bool TryGetWeather(DungeonRoom room, out DungeonWeather? weather)
        {
            weather = WeatherField.GetValue(room) as DungeonWeather;
            return weather != null;
        }

        internal static DungeonMasterInfo? GetDungeonMasterInfo(DungeonRoom room) =>
            DungeonMasterInfoField.GetValue(room) as DungeonMasterInfo;

        internal static bool IsPlaying(DungeonRoom room)
        {
            object? state = StateField.GetValue(room);
            return state != null && state.ToString() == "OnPlaying";
        }

        internal static void ResetPrevSyncTime(DungeonRoom room) =>
            PrevSyncTimeField.SetValue(room, TimeSpan.Zero);

        internal static double GetElapsedGameSeconds(DungeonRoom room)
        {
            long elapsedMs = (long)ElapsedTimeField.GetValue(room);
            long scaleFactor = HubGameDataAccess.Excel?.Consts.C_GameTimeScaleFactor ?? 1000;
            return elapsedMs * 0.001 * (scaleFactor * 0.001);
        }

        internal static long GetVanillaStartSeconds(DungeonRoom room)
        {
            WeatherRoomState state = GetOrCreateState(room);
            if (state.VanillaSnapshot != null)
            {
                return state.VanillaSnapshot.VanillaStartSeconds;
            }

            DungeonMasterInfo? info = GetDungeonMasterInfo(room);
            if (info == null || string.IsNullOrEmpty(info.StartDisplayTime))
            {
                return 0;
            }

            return ParseDisplayTimeToSeconds(info.StartDisplayTime);
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

        internal static WeatherVanillaSnapshot CaptureWeatherSnapshot(DungeonRoom room, DungeonWeather weather)
        {
            List<int> hours = weather.GetAllWeather();
            List<bool> forecast = (List<bool>)WeatherForecastByHourField.GetValue(weather)!;
            DungeonMasterInfo? info = GetDungeonMasterInfo(room);
            string? startDisplayTime = info?.StartDisplayTime;
            return new WeatherVanillaSnapshot
            {
                WeatherByHour = [.. hours],
                WeatherForecastByHour = [.. forecast],
                IsRandomOccured = (bool)IsRandomOccuredField.GetValue(weather)!,
                VanillaStartSeconds = string.IsNullOrEmpty(startDisplayTime)
                    ? 0
                    : ParseDisplayTimeToSeconds(startDisplayTime),
            };
        }

        internal static void RestoreWeatherSnapshot(DungeonRoom room, WeatherVanillaSnapshot snapshot)
        {
            if (!TryGetWeather(room, out DungeonWeather? weather) || weather == null)
            {
                return;
            }

            WeatherByHourField.SetValue(weather, new List<int>(snapshot.WeatherByHour));
            WeatherForecastByHourField.SetValue(weather, new List<bool>(snapshot.WeatherForecastByHour));
            IsRandomOccuredField.SetValue(weather, snapshot.IsRandomOccured);
            ResetPrevSyncTime(room);
        }
    }
}
