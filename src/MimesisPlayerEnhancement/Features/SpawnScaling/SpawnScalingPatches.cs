namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingPatches
    {
        private const string Feature = "SpawnScaling";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();
            SceneScopedConfigGate.RegisterDungeonRunEndCleanup(ResetRuntimeState);

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SpawnScalingPatches)));
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the SpawnScaling section changes.</summary>
        public static void RefreshFromConfig()
        {
            if (!ModConfig.EnableSpawnScaling.Value)
            {
                ResetRuntimeState();
            }
        }

        /// <summary>Called via FeatureModule.onSessionEnded.</summary>
        public static void OnSessionEnded()
        {
            ResetRuntimeState();
        }

        internal static void ResetRuntimeState()
        {
            MapPlacedEncounterScheduler.ClearPendingEncounters();
            MapPlacedEncounterProximity.ClearCaches();
        }
    }
}
