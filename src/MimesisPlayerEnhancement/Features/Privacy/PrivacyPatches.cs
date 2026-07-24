namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class PrivacyPatches
    {
        private const string Feature = "Privacy";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PrivacyPatches)));
        }
    }
}
