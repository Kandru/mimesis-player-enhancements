namespace MimesisPlayerEnhancement.Config.HostConfigSync.Patches
{
    // game@0.3.1 Assembly-CSharp/NetworkManagerV2.cs:L103-120
    [HarmonyPatch(typeof(NetworkManagerV2), nameof(NetworkManagerV2.OnRecvPacket))]
    internal static class NetworkManagerV2ConfigSyncPacketPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(IMsg msg)
        {
            if (msg is not AdminCommandReq request)
            {
                return true;
            }

            try
            {
                return !HostConfigSyncTransport.TryHandleClientPacket(request);
            }
            catch (Exception ex)
            {
                ModLog.Warn("HostConfigSync", $"Client packet handler failed — {ex.Message}");
                return true;
            }
        }
    }
}
