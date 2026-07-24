namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyPatches
    {
        private const string Feature = "Economy";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(EconomyPatches)));
        }

        internal static void OnSessionEnded()
        {
            EconomyApplier.OnSessionEnded();
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the Economy section changes.</summary>
        public static void RefreshFromConfig()
        {
            if (SceneScopedConfigGate.IsModuleSyncDeferred(Feature))
            {
                return;
            }

            EconomyApplier.InvalidateScrapScaling();
            MaintenanceShopApplier.NotifyConfigChanged();

            if (!ModConfig.EnableEconomy.Value)
            {
                MaintenanceShopApplier.RestoreVanillaPrices();
            }
        }
    }
}
