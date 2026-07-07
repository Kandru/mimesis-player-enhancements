namespace MimesisPlayerEnhancement.Features.Weather
{
    /// <summary>
    /// Host-only minute-level <see cref="TimeSyncSig"/> for the tram console clock during dungeon runs.
    /// Vanilla only syncs when the in-game hour changes (~once per real minute at default time scale).
    /// </summary>
    internal static class WeatherTramClockSync
    {
        private const string Feature = "Weather";

        internal static void InvalidateAll()
        {
            foreach (KeyValuePair<DungeonRoom, WeatherRoomState> entry in WeatherRoomAccess.RoomStates.EnumerateAll())
            {
                InvalidateRoom(entry.Value);
            }
        }

        internal static void InvalidateRoom(WeatherRoomState state)
        {
            state.LastTramClockSyncHour = -1;
            state.LastTramClockSyncMinute = -1;
        }

        internal static void TrySyncFromUpdate(DungeonRoom room)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() =>
                    WeatherResolver.IsFeatureEnabled && ModConfig.EnableRealtimeTramClock.Value)
                || !WeatherRoomAccess.IsPlaying(room))
            {
                return;
            }

            try
            {
                TimeSpan displayTime = WeatherTimeResolver.ComputeDisplayTime(room);
                WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(room);
                if (state.LastTramClockSyncHour == displayTime.Hours
                    && state.LastTramClockSyncMinute == displayTime.Minutes)
                {
                    return;
                }

                if (!WeatherRoomAccess.TryGetWeather(room, out DungeonWeather? weather) || weather == null)
                {
                    return;
                }

                int hour = displayTime.Hours;
                TimeSyncSig msg = new TimeSyncSig
                {
                    currentTime = displayTime,
                    currentWeatherMasterID = weather.GetWeatherMasterID(hour),
                    forecastWeatherMasterID = weather.GetWeatherForecastMasterID(hour),
                };
                room.SendToAll(msg);
                state.LastTramClockSyncHour = displayTime.Hours;
                state.LastTramClockSyncMinute = displayTime.Minutes;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Realtime tram clock sync failed — {ex.Message}");
            }
        }
    }
}
