using MimesisPlayerEnhancement.Features.DungeonRandomizer;

namespace MimesisSeedScanner
{
    public static class PoolSelector
    {
        /// <summary>
        /// Selects up to poolSize highest-scoring seeds from candidates.
        /// </summary>
        public static List<int> SelectPool(
            DungeonSeedFlavor flavor,
            IReadOnlyList<(int Seed, GenerationMetrics Metrics)> candidates,
            int poolSize,
            int selectionSeed = 0)
        {
            _ = selectionSeed;

            if (candidates.Count == 0 || poolSize <= 0)
            {
                return [];
            }

            var scored = new List<(int Seed, float Score)>(candidates.Count);
            foreach ((int seed, GenerationMetrics metrics) in candidates)
            {
                scored.Add((seed, SeedScoring.GetScore(flavor, metrics)));
            }

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));

            int take = Math.Min(poolSize, scored.Count);
            var selected = new List<int>(take);
            for (int i = 0; i < take; i++)
            {
                selected.Add(scored[i].Seed);
            }

            selected.Sort();
            return selected;
        }
    }
}
