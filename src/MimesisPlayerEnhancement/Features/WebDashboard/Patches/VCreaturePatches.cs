namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/VCreature.cs:L258-265
    [HarmonyPatch(typeof(VCreature), nameof(VCreature.ForcedDying))]
    internal static class BlockForcedDyingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(VCreature __instance)
        {
            if (!WebDashboardHostCheatsRuntime.HasActiveGodMode)
            {
                return true;
            }

            return __instance is not VPlayer player || !WebDashboardHostCheatsRuntime.IsGodModeActive(player);
        }
    }
}
