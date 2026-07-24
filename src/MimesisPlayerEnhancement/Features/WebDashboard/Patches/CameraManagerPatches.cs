namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/CameraManager.cs:L240-245
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
    internal static class OnEnterDungeonCheatsPatch
    {
        [HarmonyPostfix]
        private static void Postfix() =>
            WebDashboardHostCheatsRuntime.EndRoomTransition("dungeon entered");
    }

    // game@0.3.1 Assembly-CSharp/CameraManager.cs:L247-250
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEndDungeon))]
    internal static class OnEndDungeonCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("leaving dungeon");
    }
}
