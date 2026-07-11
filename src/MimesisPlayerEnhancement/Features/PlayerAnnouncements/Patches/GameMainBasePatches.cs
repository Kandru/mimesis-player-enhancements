namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements.Patches
{
    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))]
    internal static class GameMainDeathAnnouncementPatch
    {
        private const string Feature = "Announcements";

        [HarmonyPostfix]
        public static void Postfix(ProtoActor actor)
        {
            try
            {
                MapRunStatsTracker.OnLocalPlayerDeath(actor);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Death announcement failed — {ex.Message}");
            }
        }
    }
}
