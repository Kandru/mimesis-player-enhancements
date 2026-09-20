namespace MimesisPlayerEnhancement.Config.HostConfigSync.Patches
{
    // game@0.3.2 Assembly-CSharp/VRoomManager.cs:L894-905
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))]
    internal static class VRoomManagerOnRegistPlayerConfigSyncPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ulong steamID)
        {
            try
            {
                HostConfigSyncRuntime.OnPlayerRegistered(steamID);
            }
            catch (Exception ex)
            {
                ModLog.Warn("HostConfigSync", $"{nameof(VRoomManager.OnRegistPlayer)} postfix failed — {ex.Message}");
            }
        }
    }
}
