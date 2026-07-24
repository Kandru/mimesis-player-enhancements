namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/VWorld.cs:L1655-1658
    [HarmonyPatch(typeof(VWorld), nameof(VWorld.OnUnregistPlayer))]
    public static class VWorldUnregisterPatches
    {
        private const string Feature = "Statistics";

        [HarmonyPostfix]
        public static void Postfix(ulong steamID)
        {
            try
            {
                PlayerPresenceEvents.OnPlayerUnregistered(steamID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"{nameof(VWorld.OnUnregistPlayer)} failed — {ex.Message}");
            }
        }
    }
}
