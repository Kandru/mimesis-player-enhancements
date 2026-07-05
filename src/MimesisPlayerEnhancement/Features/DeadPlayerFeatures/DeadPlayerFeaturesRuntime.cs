namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures
{
    internal static class DeadPlayerFeaturesRuntime
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void OnDungeonEnter()
        {
            ResetDungeonState();
            MimicPossessionResolver.RefreshFromDungeonLifecycle();
            ModLog.Debug(Feature, "Dungeon entered — dead-player feature state initialized");
        }

        internal static void OnDungeonEnd()
        {
            ResetDungeonState();
            ModLog.Debug(Feature, "Dungeon ended — dead-player feature state reset");
        }

        private static void ResetDungeonState()
        {
            MimicPossessionSessions.ClearAll();
        }
    }
}
