namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonRandomizerPatches
    {
        private const string Feature = "DungeonRandomizer";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(DungeonRandomizerPatches)));
        }
    }
}
