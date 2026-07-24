namespace MimesisPlayerEnhancement.Config
{
    internal static class SaveSlotSidecarPersistencePatches
    {
        private const string Feature = "SaveSlotSidecar";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SaveSlotSidecarPersistencePatches)));
        }
    }
}
