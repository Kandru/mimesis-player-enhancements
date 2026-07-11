namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.SetNextDungeonMasterID))]
    internal static class GameSessionInfoSetNextDungeonMasterIdPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(ref int randomDungeonSeed)
        {
            try
            {
                if (!DungeonRandomizerPatchHelpers.ShouldApply)
                {
                    return;
                }

                randomDungeonSeed = DungeonSeedResolver.RollSeed(randomDungeonSeed);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetNextDungeonMasterID prefix failed — {ex.Message}");
            }
        }
    }
}
