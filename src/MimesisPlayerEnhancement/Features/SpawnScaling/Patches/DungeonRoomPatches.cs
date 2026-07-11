namespace MimesisPlayerEnhancement.Features.SpawnScaling.Patches
{
    [HarmonyPatch(typeof(DungeonRoom), "InitSpawn")]
    internal static class DungeonRoomInitSpawnPatch
    {
        private const string Feature = SpawnScalingPatchHelpers.Feature;

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                SpawnScalingApplier.EnsureApplied(__instance);
                MapPlacedEncounterScheduler.ApplyAfterInit(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InitSpawn postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), "ManageSpawnData")]
    internal static class DungeonRoomManageSpawnDataPatch
    {
        private const string Feature = SpawnScalingPatchHelpers.Feature;

        [ThreadStatic]
        private static ManageSpawnDataSnapshot? _snapshot;

        [HarmonyPrefix]
        public static void Prefix(DungeonRoom __instance)
        {
            try
            {
                _snapshot = PeriodicSpawnWaitApplier.CaptureSnapshot(__instance);
                SpawnTimingOverrideApplier.BeginManageSpawnData(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ManageSpawnData prefix failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                if (_snapshot != null)
                {
                    PeriodicSpawnWaitApplier.OnManageSpawnDataPostfix(__instance, _snapshot.Value);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ManageSpawnData postfix failed — {ex.Message}");
            }
        }

        [HarmonyFinalizer]
        public static void Finalizer(DungeonRoom __instance)
        {
            try
            {
                SpawnTimingOverrideApplier.EndManageSpawnData(__instance);
                _snapshot = null;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ManageSpawnData finalizer failed — {ex.Message}");
            }
        }
    }
}
