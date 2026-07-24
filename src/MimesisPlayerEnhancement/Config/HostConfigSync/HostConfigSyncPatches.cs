namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    internal static class HostConfigSyncPatches
    {
        private const string Feature = "HostConfigSync";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(HostConfigSyncPatches)));

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnRecvPacket/NetworkManagerV2", AccessTools.Method(typeof(NetworkManagerV2), nameof(NetworkManagerV2.OnRecvPacket))),
                ("RegisterAdminProtocol/VWorld", AccessTools.Method(typeof(VWorld), nameof(VWorld.RegisterAdminProtocol))),
                ("OnUnregistPlayer/VWorld", AccessTools.Method(typeof(VWorld), nameof(VWorld.OnUnregistPlayer))),
                ("OnRegistPlayer/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))),
            ]);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }
    }
}
