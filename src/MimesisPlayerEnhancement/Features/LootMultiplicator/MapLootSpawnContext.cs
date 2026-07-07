namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    /// <summary>
    /// Marks spawns originating from map marker ExecuteLootingObjectSpawn (uses EventAction internally).
    /// </summary>
    internal static class MapLootSpawnContext
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
