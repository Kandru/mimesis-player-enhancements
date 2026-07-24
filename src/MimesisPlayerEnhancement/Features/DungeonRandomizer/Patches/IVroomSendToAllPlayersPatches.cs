namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    // game@0.3.1 Assembly-CSharp/IVroom.cs:L688-697
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.SendToAllPlayers), typeof(IMsg), typeof(VActor))]
    internal static class VroomSendToAllPlayersSeedPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(IMsg msg)
        {
            try
            {
                if (msg is not MoveToDungeonSig sig)
                {
                    return;
                }

                int seed = sig.randDungeonSeed;
                DungeonRandomizerPatchHelpers.TryCurateSeed(ref seed, sig.selectedDungeonMasterID);
                sig.randDungeonSeed = seed;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SendToAllPlayers seed curation failed — {ex.Message}");
            }
        }
    }
}
