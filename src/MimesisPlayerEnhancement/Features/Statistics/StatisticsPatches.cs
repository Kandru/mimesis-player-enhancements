namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsPatches
    {
        private const string Feature = "Statistics";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(StatisticsPatches)));
        }
    }
}
