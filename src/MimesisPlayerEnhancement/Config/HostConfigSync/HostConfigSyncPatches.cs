namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    internal static class HostConfigSyncPatches
    {
        private const string Feature = "HostConfigSync";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(HostConfigSyncPatches)));
        }
    }
}
