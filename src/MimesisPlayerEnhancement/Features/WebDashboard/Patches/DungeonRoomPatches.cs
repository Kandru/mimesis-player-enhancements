namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(DungeonRoom), "InitSpawn")]
    internal static class DungeonRoomInitSpawnPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardMinimapLayoutBuilder.RequestRebuild();
        }
    }
}
