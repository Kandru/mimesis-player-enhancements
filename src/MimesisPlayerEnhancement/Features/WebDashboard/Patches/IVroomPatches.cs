namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.ValidPosition))]
    internal static class ValidPositionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(IVroom __instance, ref bool __result)
        {
            VCreature? bypassCreature = WebDashboardHostCheatsRuntime.MoveValidationCreature;
            if (bypassCreature != null
                && bypassCreature.VRoom == __instance
                && WebDashboardHostCheatsRuntime.IsNoClipActive(bypassCreature))
            {
                __result = true;
                return false;
            }

            if (!WebDashboardHostCheatsRuntime.IsNoClipActiveInRoom(__instance))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }
}
