using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootScalingGate
    {
        internal static bool ShouldScale()
        {
            return HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableLootMultiplicator.Value);
        }

        internal static bool ShouldScale(DungeonRoom room)
        {
            return room != null && ShouldScale();
        }
    }
}
