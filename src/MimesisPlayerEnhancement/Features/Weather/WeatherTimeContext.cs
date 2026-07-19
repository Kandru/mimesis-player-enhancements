namespace MimesisPlayerEnhancement.Features.Weather
{
    /// <summary>
    /// Thread-local marker while <see cref="DungeonRoom.OnUpdate"/> computes display time.
    /// </summary>
    internal static class WeatherTimeContext
    {
        [System.ThreadStatic]
        private static DungeonRoom? _activeRoom;

        internal static void Enter(DungeonRoom room) => _activeRoom = room;

        internal static void Exit() => _activeRoom = null;

        internal static bool TryGetActiveRoom(out DungeonRoom room)
        {
            if (_activeRoom != null)
            {
                room = _activeRoom;
                return true;
            }

            room = null!;
            return false;
        }

        internal static bool ShouldOverrideConvertResult(long vanillaSeconds)
        {
            if (!TryGetActiveRoom(out DungeonRoom room))
            {
                return false;
            }

            long roomVanillaStart = WeatherRoomAccess.GetVanillaStartSeconds(room);
            return ShouldOverrideConvertResult(vanillaSeconds, roomVanillaStart);
        }

        internal static bool ShouldOverrideConvertResult(long vanillaSeconds, long roomVanillaStart) =>
            roomVanillaStart > 0 && vanillaSeconds == roomVanillaStart;
    }
}
