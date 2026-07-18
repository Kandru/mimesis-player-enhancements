namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(VWorld), nameof(VWorld.ReadyToGamePktRecording))]
    internal static class VWorldReadyToGamePktRecordingSeedPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(int nextDungeonMasterID, ref int randomDungeonSeed, int pickedMapID)
        {
            try
            {
                DungeonRandomizerPatchHelpers.TryCurateSeed(ref randomDungeonSeed, nextDungeonMasterID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ReadyToGamePktRecording seed curation failed — {ex.Message}");
            }
        }
    }
}
