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
    }
}
