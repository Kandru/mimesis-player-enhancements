namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(ProtoActor), "UpdateControl")]
    internal static class NoClipUpdateControlPatch
    {
        private const string Feature = "WebDashboard";

        [HarmonyPrefix]
        private static bool Prefix(ProtoActor __instance)
        {
            if (!WebDashboardHostCheatsRuntime.HasActiveNoClip
                || WebDashboardHostCheatsRuntime.IsRoomTransitionSuspended
                || !WebDashboardHostCheatsNoClipMovement.ShouldReplaceControl(__instance))
            {
                return true;
            }

            try
            {
                WebDashboardHostCheatsNoClipMovement.Apply(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Noclip movement failed — {ex.Message}");
            }

            return false;
        }
    }
}
