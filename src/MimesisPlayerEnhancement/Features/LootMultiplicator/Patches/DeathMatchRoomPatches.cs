namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch(typeof(DeathMatchRoom), "ExtractRoomInfo", [typeof(bool)])]
    internal static class DeathMatchRoomExtractRoomInfoPatch
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(ref bool __state)
        {
            __state = true;
            DeathMatchRewardDropTableContext.Enter();
        }

        [HarmonyFinalizer]
        public static void Finalizer(bool __state)
        {
            if (__state)
            {
                DeathMatchRewardDropTableContext.Exit();
            }
        }
    }
}
