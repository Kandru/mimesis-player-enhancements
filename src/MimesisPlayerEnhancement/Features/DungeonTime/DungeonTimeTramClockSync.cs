namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    /// <summary>
    /// Host-only minute-level <see cref="TimeSyncSig"/> for the tram console clock during dungeon runs.
    /// Vanilla only syncs when the in-game hour changes (~once per real minute at default time scale).
    /// </summary>
    internal static class DungeonTimeTramClockSync
    {
        private const string Feature = "DungeonTime";

        internal static void InvalidateAll()
        {
            foreach (KeyValuePair<DungeonRoom, DungeonTimeRoomState> entry in DungeonTimeRuntime.RoomStates.EnumerateAll())
            {
                InvalidateRoom(entry.Value);
            }
        }

        internal static void InvalidateRoom(DungeonTimeRoomState state)
        {
            state.LastTramClockSyncHour = -1;
            state.LastTramClockSyncMinute = -1;
        }

        internal static void TrySyncFromUpdate(DungeonRoom room)
        {
            DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() =>
                    config.EnableDungeonTime && ModConfig.EnableRealtimeTramClock.Value)
                || !DungeonTimeRoomAccess.IsPlaying(room))
            {
                return;
            }

            try
            {
                TimeSpan displayTime = DungeonTimeClockResolver.ComputeDisplayTime(room, config);
                DungeonTimeRoomState state = DungeonTimeRoomAccess.GetOrCreateState(room);
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
