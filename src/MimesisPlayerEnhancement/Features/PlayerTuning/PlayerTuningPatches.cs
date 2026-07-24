namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningPatches
    {
        private const string Feature = "PlayerTuning";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PlayerTuningPatches)));
        }
    }
}
