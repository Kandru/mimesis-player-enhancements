namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimePatches
    {
        private const string Feature = "DungeonTime";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(DungeonTimePatches)));
        }
    }
}
