namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenPatches
    {
        private const string Feature = CustomLoadingScreenConstants.Feature;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(CustomLoadingScreenPatches));
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);
        }
    }
}
