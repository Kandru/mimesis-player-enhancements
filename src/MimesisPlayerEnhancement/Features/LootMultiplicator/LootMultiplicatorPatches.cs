using MimesisPlayerEnhancement.Features.LootMultiplicator.Patches;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplicatorPatches
    {
        private const string Feature = LootMultiplicatorPatchHelpers.Feature;

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(LootMultiplicatorPatches)));
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the LootMultiplicator section changes.</summary>
        public static void RefreshFromConfig()
        {
            LootMultiplicatorPatchHelpers.RefreshFromConfig();
        }
    }
}
