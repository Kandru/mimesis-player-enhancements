namespace MimesisPlayerEnhancement.Config.HostConfigSync.Patches
{
    // game@0.3.2 Assembly-CSharp/VWorld.cs:L1880-1883
    [HarmonyPatch(typeof(VWorld), nameof(VWorld.OnUnregistPlayer))]
    internal static class VWorldOnUnregistPlayerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ulong steamID)
        {
            try
            {
                HostConfigSyncRuntime.OnPlayerUnregistered(steamID);
            }
            catch (Exception ex)
            {
                ModLog.Warn("HostConfigSync", $"{nameof(VWorld.OnUnregistPlayer)} postfix failed — {ex.Message}");
            }
        }
    }
}
