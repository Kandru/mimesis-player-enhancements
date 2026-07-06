namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherApplier
    {
        internal static void EnsureApplied(DungeonRoom room)
        {
            if (DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.Weather))
            {
                return;
            }

            ApplyToRoom(room);
            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.Weather);
        }

        internal static void ApplyToRoom(DungeonRoom room)
        {
            WeatherRoomAccess.GetOrCreateState(room);

            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => WeatherResolver.IsFeatureEnabled))
            {
                WeatherLog.DebugSkipped("feature disabled or not host");
                return;
            }

            ApplyWeatherMode(room);
            WeatherRoomAccess.ResetPrevSyncTime(room);
        }

        internal static void RestoreVanilla(DungeonRoom room)
        {
            WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(room);
            if (state.VanillaSnapshot != null)
            {
                WeatherRoomAccess.RestoreWeatherSnapshot(room, state.VanillaSnapshot);
            }

            WeatherCycleScheduler.Stop(room);
            WeatherRoomAccess.ResetPrevSyncTime(room);
        }

        private static void ApplyWeatherMode(DungeonRoom room)
        {
            WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(room);
            WeatherMode mode = WeatherResolver.GetMode();
            switch (mode)
            {
                case WeatherMode.Fixed:
                    WeatherCycleScheduler.Stop(room);
                    if (WeatherResolver.TryGetFixedWeatherMasterId(out int masterId))
                    {
                        room.AdminChangeWeather(masterId);
                        WeatherLog.InfoApplied($"Fixed weather applied — masterId={masterId}");
                    }

                    break;

                case WeatherMode.Cycle:
                    WeatherCycleScheduler.RestartFromConfig(room);
                    WeatherLog.InfoApplied("Cycle weather started");
                    break;

                case WeatherMode.Vanilla:
                    WeatherCycleScheduler.Stop(room);
                    if (state.VanillaSnapshot != null)
                    {
                        WeatherRoomAccess.RestoreWeatherSnapshot(room, state.VanillaSnapshot);
                    }

                    if (WeatherResolver.ShouldStripRandomWeather()
                        && WeatherRoomAccess.TryGetWeather(room, out DungeonWeather? weather)
                        && weather != null
                        && weather.IsRandomOccured
                        && state.VanillaSnapshot != null)
                    {
                        WeatherScheduleRebuilder.StripRandomWeather(
                            weather,
                            state.VanillaSnapshot.DayCount,
                            state.VanillaSnapshot.RandomSeed,
                            state.VanillaSnapshot.OverrideDefaultWeatherId);
                        WeatherLog.InfoApplied("Random weather stripped");
                    }

                    break;
            }
        }
    }
}
