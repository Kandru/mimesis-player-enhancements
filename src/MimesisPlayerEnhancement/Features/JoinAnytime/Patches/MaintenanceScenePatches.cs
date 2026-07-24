namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/MaintenanceScene.cs:L243-258
    [HarmonyPatch(typeof(MaintenanceScene), "TryInitHostMaintenenceRoom")]
    internal static class MaintenanceSceneTryInitHostMaintenenceRoomPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.OnHostSceneReady();
        }
    }
}
