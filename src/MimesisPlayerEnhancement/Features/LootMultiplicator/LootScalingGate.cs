namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootScalingGate
    {
        internal static bool ShouldScale()
        {
            return HostApplyGate.ShouldApplyHostOnlyFeature(() => SceneScopedConfigGate.Loot.EnableLootMultiplicator);
        }

        internal static bool ShouldScale(DungeonRoom room)
        {
            return room != null && ShouldScale();
        }
    }
}
