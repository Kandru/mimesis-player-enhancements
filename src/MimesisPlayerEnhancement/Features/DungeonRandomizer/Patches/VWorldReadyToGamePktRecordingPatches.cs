namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    // game@0.3.2 Assembly-CSharp/VWorld.cs:L2268-2274
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
