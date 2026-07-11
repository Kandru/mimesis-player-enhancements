namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(VCreature), nameof(VCreature.ForcedDying))]
    internal static class BlockForcedDyingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VCreature __instance)
        {
            return __instance is not VPlayer player || !WebDashboardHostCheatsRuntime.IsGodModeActive(player);
        }
    }
}
