namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L206-315
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
