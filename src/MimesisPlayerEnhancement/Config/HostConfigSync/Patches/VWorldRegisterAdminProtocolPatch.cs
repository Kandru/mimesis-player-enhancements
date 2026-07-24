namespace MimesisPlayerEnhancement.Config.HostConfigSync.Patches
{
    // game@0.3.1 Assembly-CSharp/VWorld.cs:L72-100
    [HarmonyPatch(typeof(VWorld), nameof(VWorld.RegisterAdminProtocol))]
    internal static class VWorldRegisterAdminProtocolPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VWorld __instance)
        {
            try
            {
                HostConfigSyncTransport.RegisterAdminCommands(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn("HostConfigSync", $"RegisterAdminProtocol postfix failed — {ex.Message}");
            }
        }
    }
}
