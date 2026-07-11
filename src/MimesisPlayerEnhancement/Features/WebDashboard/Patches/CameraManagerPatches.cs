namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
    internal static class OnEnterDungeonCheatsPatch
    {
        [HarmonyPostfix]
        private static void Postfix() =>
            WebDashboardHostCheatsRuntime.EndRoomTransition("dungeon entered");
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEndDungeon))]
    internal static class OnEndDungeonCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("leaving dungeon");
    }
}
