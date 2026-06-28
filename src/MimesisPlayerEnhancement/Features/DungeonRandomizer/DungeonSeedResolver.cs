namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonSeedResolver
{
    internal static int RollSeed(int vanillaSeed)
    {
        if (!ModConfig.RandomizeDungeonSeed.Value)
            return vanillaSeed;

        int seed = UnityEngine.Random.Range(1, int.MaxValue);
        DungeonRandomizerLog.Debug($"Dungeon seed: {vanillaSeed} -> {seed}");
        return seed;
    }
}
