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

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L707-766
    // Shrink the pending _elapsedTime add so display/hourly TimeSyncSig span the extended real shift.
    // Do not send extra TimeSyncSig — clients set worldTime = Hours and locally advance at vanilla
    // scale between packets, so sub-hour syncs snap the sky backward.
    [HarmonyPatch(typeof(DungeonRoom), "OnUpdate")]
    [HarmonyPriority(HarmonyLib.Priority.First)]
    internal static class DungeonRoomOnUpdateDisplayScalePatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPrefix]
        public static void Prefix(DungeonRoom __instance, long delta)
        {
            try
            {
                _ = DungeonTimeRuntime.TryPrepareElapsedForScaledDelta(__instance, delta);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnUpdate display-scale prefix failed — {ex.Message}");
            }
        }
    }
}
