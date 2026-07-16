namespace MimesisSeedScanner
{
    public static class PoolSelector
    {
        public const int DefaultPoolSelectionSeed = 42;

        /// <summary>
        /// Selects up to poolSize seeds from candidates using percentile qualification and random sampling.
        /// </summary>
        public static List<int> SelectPool(
            string flavor,
            IReadOnlyList<(int Seed, GenerationMetrics Metrics)> candidates,
            int poolSize,
            int selectionSeed = DefaultPoolSelectionSeed)
        {
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

            int qualifyCount = Math.Max(poolSize, (int)Math.Ceiling(scored.Count * 0.1));
            qualifyCount = Math.Min(qualifyCount, scored.Count);
            var qualified = scored.Take(qualifyCount).Select(entry => entry.Seed).ToList();

            if (qualified.Count <= poolSize)
            {
                qualified.Sort();
                return qualified;
            }

            var random = new RandomStream(selectionSeed ^ flavor.GetHashCode(StringComparison.Ordinal));
            var selected = new List<int>(poolSize);
            var remaining = new List<int>(qualified);
            while (selected.Count < poolSize && remaining.Count > 0)
            {
                int pick = random.Next(0, remaining.Count);
                selected.Add(remaining[pick]);
                remaining.RemoveAt(pick);
            }

            selected.Sort();
            return selected;
        }
    }
}
