namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    /// <summary>
    /// Host <see cref="TimeSyncSig"/> for tram realtime clock and non-vanilla display-rate corrections.
    /// </summary>
    internal static class DungeonTimeTramClockSync
    {
        private const string Feature = "DungeonTime";

        private static bool _wasRealtimeTramClock;

        /// <summary>
        /// Floors a display <see cref="TimeSpan"/> to the hour for tram/alarm view
        /// (<c>HH:00</c>) without changing dungeon elapsed or start time.
        /// </summary>
        internal static TimeSpan FloorToDisplayHour(TimeSpan time) =>
            new TimeSpan(time.Days, time.Hours, minutes: 0, seconds: 0);

        internal static void RefreshFromConfig()
        {
            bool tramRealtime = IsRealtimeTramClockEnabled();
            if (_wasRealtimeTramClock && !tramRealtime)
            {
                SnapDisplayMinutesToHour();
            }
            else if (tramRealtime || NeedsRateSync())
            {
                InvalidateAll();
                TrySyncAllPlayingRooms();
            }

            _wasRealtimeTramClock = tramRealtime;
            DungeonTimeClientWorldClock.RefreshFromConfig();
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
            if (!ShouldSendTimeSync(room) || !DungeonTimeRoomAccess.IsPlaying(room))
            {
                return;
            }

            try
            {
                DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
                long startSeconds = DungeonTimeClockResolver.GetEffectiveStartSeconds(room, config);
                long endSeconds = DungeonTimeRoomAccess.GetEndSeconds(room);
                double elapsedGameSeconds = DungeonTimeRoomAccess.GetElapsedGameSeconds(room);
                // Stop once the display has filled start→end. Further syncs wrap to 00:xx and
                // re-trigger tram alarms; do not force a 24:00 packet here (vanilla time-over does).
                if (DungeonTimeResolver.HasReachedOrPassedDisplayEnd(
                        elapsedGameSeconds,
                        startSeconds,
                        endSeconds))
                {
                    return;
                }

                TimeSpan displayTime = DungeonTimeClockResolver.ComputeDisplayTime(room, config);
                // Without realtime tram clock, only push HH:00 (hourly) even when rate sync is on.
                if (!ModConfig.EnableRealtimeTramClock.Value)
                {
                    displayTime = FloorToDisplayHour(displayTime);
                }

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
                ModLog.Warn(Feature, $"Display clock sync failed — {ex.Message}");
            }
        }

        private static bool ShouldSendTimeSync(DungeonRoom room)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => SceneScopedConfigGate.DungeonTime.EnableDungeonTime))
            {
                return false;
            }

            return ModConfig.EnableRealtimeTramClock.Value
                || DungeonTimeResolver.IsNonVanillaDisplayRate(
                    DungeonTimeRuntime.GetEffectiveDisplayRate(room));
        }

        private static bool IsRealtimeTramClockEnabled() =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() =>
                SceneScopedConfigGate.DungeonTime.EnableDungeonTime
                && ModConfig.EnableRealtimeTramClock.Value);

        private static bool NeedsRateSync() =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => SceneScopedConfigGate.DungeonTime.EnableDungeonTime)
            && DungeonTimeResolver.IsNonVanillaDisplayRate(
                DungeonTimeResolver.GetEffectiveDisplayRate(1d, ModConfig.TimeMultiplier.Value));

        private static void TrySyncAllPlayingRooms()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            foreach (KeyValuePair<DungeonRoom, DungeonTimeRoomState> entry in DungeonTimeRuntime.RoomStates.EnumerateAll())
            {
                if (DungeonTimeRoomAccess.IsPlaying(entry.Key))
                {
                    TrySyncFromUpdate(entry.Key);
                }
            }
        }

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
                if (!DungeonTimeRoomAccess.IsPlaying(room))
                {
                    continue;
                }

                try
                {
                    // View-only: zero minutes on the current hour. Never jump to shift-end 24:00.
                    TimeSpan floored = FloorToDisplayHour(
                        DungeonTimeClockResolver.ComputeDisplayTime(room, config));
                    if (state.LastTramClockSyncHour == floored.Hours
                        && state.LastTramClockSyncMinute == floored.Minutes)
                    {
                        continue;
                    }

                    TrySendDisplayTime(room, state, floored);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Tram clock snap-to-hour failed — {ex.Message}");
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
