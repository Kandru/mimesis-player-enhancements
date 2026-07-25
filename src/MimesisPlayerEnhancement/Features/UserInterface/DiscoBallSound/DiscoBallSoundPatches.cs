namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundPatches
    {
        private const string Feature = DiscoBallSoundConstants.Feature;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(DiscoBallSoundPatches));
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);
        }
    }
}
