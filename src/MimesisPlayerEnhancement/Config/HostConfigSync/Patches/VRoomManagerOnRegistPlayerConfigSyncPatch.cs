namespace MimesisPlayerEnhancement.Config.HostConfigSync.Patches
{
    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L748-761
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
