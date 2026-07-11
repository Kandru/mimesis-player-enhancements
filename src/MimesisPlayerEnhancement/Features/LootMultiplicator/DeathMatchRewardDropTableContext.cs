namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    /// <summary>
    /// Marks drop-table resolution during deathmatch MVP winner reward (DeathMatchRoom.ExtractRoomInfo).
    /// </summary>
    internal static class DeathMatchRewardDropTableContext
    {
        [ThreadStatic]
        private static int _depth;

        internal static void Enter()
        {
            _depth++;
        }

        internal static void Exit()
        {
            _depth = Math.Max(0, _depth - 1);
        }

        internal static bool IsActive => _depth > 0;
    }
}
