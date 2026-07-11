namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(NetworkManagerV2), "HandleGlobalPacket")]
    internal static class NoClipSyncPacketPatch
    {
        [HarmonyPostfix]
        private static void Postfix(IMsg msg, ref bool __result)
        {
            if (!__result || msg is not AdminCommandRes response)
            {
                return;
            }

            WebDashboardHostCheatsNoClipSync.TryHandleAdminCommandRes(response);
        }
    }
}
