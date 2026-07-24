namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    // game@0.3.1 Assembly-CSharp/VWaitingRoom.cs:L179-221
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
