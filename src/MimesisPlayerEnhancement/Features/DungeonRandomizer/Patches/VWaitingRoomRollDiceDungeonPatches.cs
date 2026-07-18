namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(VWaitingRoom), nameof(VWaitingRoom.RollDiceDungeon))]
    internal static class VWaitingRoomRollDiceDungeonPatch
    {
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
}
