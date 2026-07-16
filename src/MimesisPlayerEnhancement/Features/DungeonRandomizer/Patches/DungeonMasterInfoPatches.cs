namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.PickMapID))]
    internal static class DungeonMasterInfoPickMapIdPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(DungeonMasterInfo __instance, ref int __result)
        {
            try
            {
                if (!DungeonRandomizerPatchHelpers.ShouldApply)
                {
                    return;
                }

                int? replacement = DungeonVariantResolver.ResolveMapId(__instance, __result);
                if (replacement.HasValue)
                {
                    __result = replacement.Value;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PickMapID postfix failed — {ex.Message}");
            }
        }
    }
}
