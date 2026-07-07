namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    /// <summary>
    /// Marks drop-table resolution during Crow Shop scrap exchange (InventoryController.BarterItem).
    /// </summary>
    internal static class BarterDropTableContext
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
