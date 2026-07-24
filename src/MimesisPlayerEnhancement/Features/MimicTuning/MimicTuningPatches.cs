namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningPatches
    {
        private const string Feature = "MimicTuning";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MimicTuningPatches)));
        }
    }
}
