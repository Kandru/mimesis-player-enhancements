namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimePatches
    {
        private const string Feature = "JoinAnytime";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(JoinAnytimePatches)));
        }
    }
}
