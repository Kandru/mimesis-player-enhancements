namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundPatches
    {
        private const string Feature = RoundStartSoundConstants.Feature;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(RoundStartSoundPatches));
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);
        }
    }
}
