namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherRuntime
    {
        internal static void RefreshFromConfig()
        {
            WeatherPresetListParser.InvalidateCache();
            WeatherTramClockSync.InvalidateAll();

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            foreach (KeyValuePair<DungeonRoom, WeatherRoomState> entry in WeatherRoomAccess.RoomStates.EnumerateAll())
            {
                RefreshRoom(entry.Key);
            }
        }

        private static void RefreshRoom(DungeonRoom room)
        {
            if (!WeatherRoomAccess.IsPlaying(room))
            {
                return;
            }

            if (!WeatherResolver.IsFeatureEnabled
                || !HostApplyGate.ShouldApplyHostOnlyFeature(() => true))
            {
                WeatherApplier.RestoreVanilla(room);
                WeatherLog.InfoRestoredVanilla();
                return;
            }

            WeatherApplier.ApplyToRoom(room);
        }
    }
}
