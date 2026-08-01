using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal sealed class WeatherVanillaSnapshot
    {
        internal List<int> WeatherByHour = [];
        internal List<bool> WeatherForecastByHour = [];
        internal bool IsRandomOccured;
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
        private static readonly FieldInfo WeatherField =
            AccessTools.Field(typeof(DungeonRoom), "_weather")
            ?? throw new InvalidOperationException("DungeonRoom._weather not found");

        private static readonly FieldInfo PrevSyncTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_prevSyncTime")
            ?? throw new InvalidOperationException("DungeonRoom._prevSyncTime not found");

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

        internal static bool IsPlaying(DungeonRoom room) =>
            StateField.GetValue(room) is DungeonState.OnPlaying;

        internal static void ApplySchedule(
            DungeonWeather weather,
            List<int> weatherByHour,
            List<bool> forecastByHour,
            bool isRandomOccured)
        {
            WeatherByHourField.SetValue(weather, weatherByHour);
            WeatherForecastByHourField.SetValue(weather, forecastByHour);
            IsRandomOccuredField.SetValue(weather, isRandomOccured);
        }

        internal static void ResetPrevSyncTime(DungeonRoom room) =>
            PrevSyncTimeField.SetValue(room, TimeSpan.Zero);

        internal static WeatherVanillaSnapshot CaptureWeatherSnapshot(DungeonWeather weather)
        {
            List<int> hours = weather.GetAllWeather();
            List<bool> forecast = (List<bool>)WeatherForecastByHourField.GetValue(weather)!;
            return new WeatherVanillaSnapshot
            {
                WeatherByHour = [.. hours],
                WeatherForecastByHour = [.. forecast],
                IsRandomOccured = (bool)IsRandomOccuredField.GetValue(weather)!,
            };
        }

        internal static void RestoreWeatherSnapshot(DungeonRoom room, WeatherVanillaSnapshot snapshot)
        {
            if (!TryGetWeather(room, out DungeonWeather? weather) || weather == null)
            {
                return;
            }

            ApplySchedule(
                weather,
                new List<int>(snapshot.WeatherByHour),
                new List<bool>(snapshot.WeatherForecastByHour),
                snapshot.IsRandomOccured);
            ResetPrevSyncTime(room);
        }
    }
}
