namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
    internal static class VRoomManagerPendMoveToDungeonSeedPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(Dictionary<ulong, long> playerUIDs, int nextDungeonMasterID, ref int randomDungeonSeed, RoomDrainInfo drainInfo)
        {
            try
            {
                DungeonRandomizerPatchHelpers.TryCurateSeed(ref randomDungeonSeed, nextDungeonMasterID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PendMoveToDungeon seed curation failed — {ex.Message}");
            }
        }
    }
}
