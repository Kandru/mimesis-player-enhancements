namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(VWaitingRoom), nameof(VWaitingRoom.RollDiceDungeon))]
    internal static class VWaitingRoomRollDiceDungeonPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        internal static bool ClearFirstPickExcludes { get; set; }

        [HarmonyPrefix]
        public static void Prefix(bool reroll)
        {
            ClearFirstPickExcludes = reroll && DungeonRandomizerPatchHelpers.ShouldIgnoreRerollExcludes();
        }

        [HarmonyFinalizer]
        public static Exception? Finalizer(Exception? __exception)
        {
            ClearFirstPickExcludes = false;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ExcelDataManager), nameof(ExcelDataManager.PickDungeon))]
    internal static class ExcelDataManagerPickDungeonPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        private static IReadOnlyList<int> _excludeIdsForResolve = [];

        [HarmonyPrefix]
        public static void Prefix(ref List<int> excludeDungeonIDs)
        {
            try
            {
                if (VWaitingRoomRollDiceDungeonPatch.ClearFirstPickExcludes)
                {
                    _excludeIdsForResolve = Array.Empty<int>();
                    excludeDungeonIDs = [];
                    VWaitingRoomRollDiceDungeonPatch.ClearFirstPickExcludes = false;
                    return;
                }

                _excludeIdsForResolve = excludeDungeonIDs;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PickDungeon prefix failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            try
            {
                if (!DungeonRandomizerPatchHelpers.ShouldApply || !SceneScopedConfigGate.DungeonRandomizer.RandomizeDungeonPick)
                {
                    return;
                }

                __result = DungeonPickResolver.ResolvePick(__result, _excludeIdsForResolve);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PickDungeon postfix failed — {ex.Message}");
            }
            finally
            {
                _excludeIdsForResolve = [];
            }
        }
    }
}
