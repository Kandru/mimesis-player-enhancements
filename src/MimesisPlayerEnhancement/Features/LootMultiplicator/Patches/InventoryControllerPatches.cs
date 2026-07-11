namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch(typeof(InventoryController), "BarterItem", [typeof(PosWithRot)])]
    internal static class InventoryControllerBarterItemPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(ref bool __state)
        {
            __state = true;
            BarterDropTableContext.Enter();
        }

        [HarmonyFinalizer]
        public static void Finalizer(bool __state)
        {
            if (__state)
            {
                BarterDropTableContext.Exit();
            }
        }
    }
}
