namespace MimesisPlayerEnhancement.Features.DungeonTime.Patches
{
    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L1018-1021
    [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
    internal static class DungeonRoomOnAllMemberEnteredPatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                DungeonTimeApplier.EnsureApplied(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnAllMemberEntered postfix failed — {ex.Message}");
            }
        }
    }
}
