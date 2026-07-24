namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class PersistencePatches
    {
        private const string Feature = "Persistence";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PersistencePatches)));
        }
    }
}
