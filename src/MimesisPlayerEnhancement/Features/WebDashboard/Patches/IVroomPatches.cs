namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/IVroom.cs:L1097-1100
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.ValidPosition))]
    internal static class ValidPositionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(IVroom __instance, ref bool __result)
        {
            if (!WebDashboardHostCheatsRuntime.HasActiveNoClip)
            {
                return true;
            }

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
