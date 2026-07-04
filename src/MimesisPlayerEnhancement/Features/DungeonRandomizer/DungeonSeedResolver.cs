namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonSeedResolver
    {
        private const string Feature = "DungeonRandomizer";

        internal static int RollSeed(int vanillaSeed)
        {
            if (!ModConfig.RandomizeDungeonSeed.Value)
            {
                return vanillaSeed;
            }

            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            DungeonRandomizerLog.InfoSeedRolled(vanillaSeed, seed);
            return seed;
        }
    }
}
