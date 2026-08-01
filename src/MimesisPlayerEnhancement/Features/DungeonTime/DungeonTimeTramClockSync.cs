namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    /// <summary>
    /// Host-only minute-level <see cref="TimeSyncSig"/> for the tram console clock during dungeon runs.
    /// Vanilla only syncs when the in-game hour changes (~once per real minute at default time scale).
    /// </summary>
    internal static class DungeonTimeTramClockSync
    {
        private const string Feature = "DungeonTime";

        private static bool _wasMinuteSyncActive;

        /// <summary>
        /// Floors a display <see cref="TimeSpan"/> to the hour for tram/alarm view
        /// (<c>HH:00</c>) without changing dungeon elapsed or start time.
        /// </summary>
        internal static TimeSpan FloorToDisplayHour(TimeSpan time) =>
            new TimeSpan(time.Days, time.Hours, minutes: 0, seconds: 0);

        internal static void RefreshFromConfig()
        {
            bool active = IsMinuteSyncActive();
            if (_wasMinuteSyncActive && !active)
            {
                SnapDisplayMinutesToHour();
            }
            else if (active)
            {
                InvalidateAll();
            }

            _wasMinuteSyncActive = active;
        }

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

                TrySendDisplayTime(room, state, displayTime);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Realtime tram clock sync failed — {ex.Message}");
            }
        }

        private static bool IsMinuteSyncActive() =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() =>
                SceneScopedConfigGate.DungeonTime.EnableDungeonTime
                && ModConfig.EnableRealtimeTramClock.Value);

        private static void SnapDisplayMinutesToHour()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
            foreach (KeyValuePair<DungeonRoom, DungeonTimeRoomState> entry in DungeonTimeRuntime.RoomStates.EnumerateAll())
            {
                DungeonRoom room = entry.Key;
                DungeonTimeRoomState state = entry.Value;
                if (!DungeonTimeRoomAccess.IsPlaying(room) || state.LastTramClockSyncMinute <= 0)
                {
                    continue;
                }

                try
                {
                    TimeSpan floored = FloorToDisplayHour(
                        DungeonTimeClockResolver.ComputeDisplayTime(room, config));
                    TrySendDisplayTime(room, state, floored);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Realtime tram clock snap-to-hour failed — {ex.Message}");
                }
            }
        }

        private static void TrySendDisplayTime(
            DungeonRoom room,
            DungeonTimeRoomState state,
            TimeSpan displayTime)
        {
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
    }
}
