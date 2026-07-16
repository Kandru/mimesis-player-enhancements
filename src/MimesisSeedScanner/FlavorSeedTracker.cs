using MimesisPlayerEnhancement.Features.DungeonRandomizer;

namespace MimesisSeedScanner
{
    /// <summary>
    /// Keeps a bounded reservoir of top candidates per flavor during scanning.
    /// Final pool selection (qualify + random sample) happens at merge time.
    /// </summary>
    public sealed class FlavorSeedTracker
    {
        public const int DefaultReservoirCapacity = 5000;

        private readonly DungeonSeedFlavor _flavor;
        private readonly int _capacity;
        private readonly List<(int Seed, GenerationMetrics Metrics)> _entries = [];

        public FlavorSeedTracker(DungeonSeedFlavor flavor, int capacity = DefaultReservoirCapacity)
        {
            _flavor = flavor;
            _capacity = capacity;
        }

        public string Flavor => _flavor.ToString();

        internal DungeonSeedFlavor FlavorValue => _flavor;

        public void Consider(int seed, GenerationMetrics metrics)
        {
            if (metrics.GenerationFailed && _flavor is not (DungeonSeedFlavor.Reliable or DungeonSeedFlavor.StableCompact))
            {
                return;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Seed == seed)
                {
                    if (SeedScoring.IsBetter(_flavor, metrics, _entries[i].Metrics))
                    {
                        _entries[i] = (seed, metrics);
                    }

                    return;
                }
            }

            if (_entries.Count < _capacity)
            {
                _entries.Add((seed, metrics));
                return;
            }

            int worstIndex = 0;
            for (int i = 1; i < _entries.Count; i++)
            {
                if (SeedScoring.IsBetter(_flavor, _entries[worstIndex].Metrics, _entries[i].Metrics))
                {
                    worstIndex = i;
                }
            }

            if (SeedScoring.IsBetter(_flavor, metrics, _entries[worstIndex].Metrics))
            {
                _entries[worstIndex] = (seed, metrics);
            }
        }

        public void RestoreCandidate(int seed, GenerationMetrics metrics) => Consider(seed, metrics);

        public IReadOnlyList<(int Seed, GenerationMetrics Metrics)> GetCandidates() => _entries;

        public FlavorScanCheckpoint ToCheckpoint() =>
            new()
            {
                Flavor = Flavor,
                Candidates = _entries
                    .Select(entry => new SeedMetricsCheckpoint
                    {
                        Seed = entry.Seed,
                        Metrics = SeedMetricsMapper.ToDto(entry.Metrics),
                    })
                    .ToList(),
            };

        public static FlavorSeedTracker FromCheckpoint(FlavorScanCheckpoint checkpoint, int capacity = DefaultReservoirCapacity)
        {
            if (!DungeonSeedFlavorUtil.TryParse(checkpoint.Flavor, out DungeonSeedFlavor flavor))
            {
                throw new InvalidOperationException($"Unknown flavor in checkpoint: '{checkpoint.Flavor}'");
            }

            var tracker = new FlavorSeedTracker(flavor, capacity);
            foreach (SeedMetricsCheckpoint candidate in checkpoint.Candidates)
            {
                tracker.RestoreCandidate(candidate.Seed, SeedMetricsMapper.FromDto(candidate.Metrics));
            }

            return tracker;
        }
    }
}
