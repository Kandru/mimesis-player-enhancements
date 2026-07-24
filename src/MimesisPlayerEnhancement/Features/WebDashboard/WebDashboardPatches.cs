namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPatches
    {
        private const string Feature = "WebDashboard";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WebDashboardPatches)));
        }
    }
}
