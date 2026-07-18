namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicInventoryCopyPatches
    {
        private const string Feature = "MimicTuning";

        private static readonly System.Reflection.FieldInfo? SelfField =
            AccessTools.Field(typeof(AIController), "_self");

        [HarmonyPatch(typeof(AIController), nameof(AIController.CopyInventory))]
        internal static class CopyInventoryPatch
        {
            [HarmonyPrefix]
            internal static void Prefix(AIController __instance, ref BTTargetPickRule rule)
            {
                try
                {
                    if (!MimicInventoryCopyResolver.ShouldApplyCustom)
                    {
                        return;
                    }

                    if (SelfField?.GetValue(__instance) is not VCreature creature)
                    {
                        return;
                    }

                    if (!MonsterTypeLookup.TryGetMonster(creature.MasterID, out Bifrost.Cooked.MonsterInfo info)
                        || !info.IsMimic())
                    {
                        return;
                    }

                    rule = MimicInventoryCopyResolver.PickRule;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"CopyInventory prefix failed — {ex.Message}");
                }
            }
        }
    }
}
