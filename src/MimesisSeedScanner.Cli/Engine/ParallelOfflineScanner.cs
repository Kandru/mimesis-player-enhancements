using System.Diagnostics;
using MimesisPlayerEnhancement.Features.DungeonRandomizer;

namespace MimesisSeedScanner.Cli.Engine
{
    internal sealed class ParallelOfflineScanner
    {
        private const int SaveEverySeeds = 250;
        private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(60);

        private readonly ScanCatalog _catalog;
        private readonly IReadOnlyList<BakedFlow> _flows;
        private readonly int _maxSeed;
        private readonly int _poolSize;
        private readonly int _seedStride;
        private readonly int _threadCount;
        private readonly TimeSpan? _timeBudget;
        private readonly Thread[] _threads;
        private readonly ParallelScanWorkerState[] _states;

        internal ParallelOfflineScanner(
            ScanCatalog catalog,
            IReadOnlyList<BakedFlow> flows,
            int maxSeed,
            int poolSize,
            int seedStride,
            int threadCount,
            TimeSpan? timeBudget)
        {
            _catalog = catalog;
            _flows = flows;
            _maxSeed = maxSeed;
            _poolSize = poolSize;
            _seedStride = Math.Max(1, seedStride);
            _threadCount = Math.Max(1, threadCount);
            _timeBudget = timeBudget;
            _threads = new Thread[_threadCount];
            _states = new ParallelScanWorkerState[_threadCount];
        }

        internal int ThreadCount => _threadCount;

        internal void Start()
        {
            int totalStridedSeeds = CountStridedSeeds(1, _maxSeed, _seedStride);
            int seedsPerThread = Math.Max(1, totalStridedSeeds / _threadCount);
            int startIndex = 0;
            for (int threadId = 0; threadId < _threadCount; threadId++)
            {
                int endIndex = threadId == _threadCount - 1
                    ? totalStridedSeeds
                    : Math.Min(totalStridedSeeds, startIndex + seedsPerThread);
                int seedStart = NthStridedSeed(1, _seedStride, startIndex);
                int seedEndExclusive = endIndex >= totalStridedSeeds
                    ? _maxSeed
                    : NthStridedSeed(1, _seedStride, endIndex);
                startIndex = endIndex;
                if (seedStart >= seedEndExclusive)
                {
                    _states[threadId] = new ParallelScanWorkerState(threadId, seedStart, seedEndExclusive, _seedStride)
                    {
                        IsComplete = true,
                    };
                    continue;
                }

                var state = new ParallelScanWorkerState(threadId, seedStart, seedEndExclusive, _seedStride);
                _states[threadId] = state;
                int capturedThreadId = threadId;
                _threads[threadId] = new Thread(() => RunWorker(capturedThreadId))
                {
                    IsBackground = true,
                    Name = $"SeedScanner-{capturedThreadId}",
                };
                _threads[threadId].Start();
            }
        }

        internal bool IsComplete => _states.All(state => state.IsComplete);

        internal long GenerationsCompleted => _states.Sum(s => s.GenerationsCompleted);

        internal long TotalGenerations =>
            ComputeTotalGenerations(_maxSeed, _seedStride, _flows.Count);

        internal double OverallGenerationsPerSecond(double elapsedSeconds)
        {
            elapsedSeconds = Math.Max(0.001, elapsedSeconds);
            return GenerationsCompleted / elapsedSeconds;
        }

        internal void Join()
        {
            foreach (Thread thread in _threads)
            {
                thread?.Join();
            }
        }

        internal SeedScanDocument MergeResults() =>
            ScanShardMerger.Merge(
                _states.Where(state => state.Shard != null).Select(state => state.Shard!).ToList(),
                _maxSeed,
                _poolSize,
                _seedStride);

        private void RunWorker(int threadId)
        {
            ParallelScanWorkerState state = _states[threadId];
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ThreadShardDocument? resumeShard = TryLoadResumeShard(threadId, state);
                var shard = resumeShard ?? new ThreadShardDocument
                {
                    ThreadId = threadId,
                    SeedStart = state.SeedStart,
                    SeedEndExclusive = state.SeedEndExclusive,
                    MaxSeed = _maxSeed,
                    PoolSize = _poolSize,
                    SeedStride = _seedStride,
                    ThreadCount = _threadCount,
                };
                shard.ThreadCount = _threadCount;
                shard.SeedStride = _seedStride;

                Dictionary<string, FlavorSeedTracker[]> trackersByFlow = CreateTrackers(resumeShard);
                int seedsInRange = CountStridedSeeds(state.SeedStart, state.SeedEndExclusive, _seedStride);
                if (resumeShard is { IsComplete: true } || resumeShard?.SeedsCompleted >= seedsInRange)
                {
                    state.SeedsCompleted = seedsInRange;
                    state.GenerationsCompleted = resumeShard?.GenerationsCompleted
                        ?? (long)seedsInRange * _flows.Count;
                    UpdateShard(shard, state, trackersByFlow, seedsInRange, isComplete: true);
                    ScanShardMerger.SaveShard(shard);
                    state.Shard = shard;
                    return;
                }

                state.SeedsCompleted = resumeShard?.SeedsCompleted ?? 0;
                state.GenerationsCompleted = resumeShard?.GenerationsCompleted ?? 0;
                int completedInRange = state.SeedsCompleted;
                DateTime lastSaveAt = DateTime.UtcNow;
                while (completedInRange < seedsInRange)
                {
                    if (_timeBudget.HasValue && stopwatch.Elapsed >= _timeBudget.Value)
                    {
                        break;
                    }

                    int seed = NthStridedSeed(state.SeedStart, _seedStride, completedInRange);
                    foreach (BakedFlow flow in _flows)
                    {
                        try
                        {
                            if (OfflineDungeonGenerator.TryGenerateMetrics(
                                    _catalog,
                                    flow,
                                    seed,
                                    out GenerationMetrics metrics))
                            {
                                foreach (FlavorSeedTracker tracker in trackersByFlow[flow.FlowId])
                                {
                                    tracker.Consider(seed, metrics);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(
                                $"Generation failed — thread={threadId}, flow={flow.FlowId}, seed={seed}: {ex.Message}");
                        }

                        state.GenerationsCompleted++;
                    }

                    completedInRange++;
                    state.SeedsCompleted = completedInRange;

                    bool shouldSave = completedInRange % SaveEverySeeds == 0
                        || DateTime.UtcNow - lastSaveAt >= SaveInterval;
                    if (shouldSave)
                    {
                        bool complete = completedInRange >= seedsInRange;
                        UpdateShard(shard, state, trackersByFlow, completedInRange, complete);
                        ScanShardMerger.SaveShard(shard);
                        lastSaveAt = DateTime.UtcNow;
                    }
                }

                bool isComplete = completedInRange >= seedsInRange;
                UpdateShard(shard, state, trackersByFlow, completedInRange, isComplete);
                ScanShardMerger.SaveShard(shard);
                state.Shard = shard;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Parallel scan thread {threadId} failed — {ex.Message}");
            }
            finally
            {
                state.IsComplete = true;
            }
        }

        internal static long ComputeTotalGenerations(int maxSeed, int seedStride, int flowCount) =>
            (long)CountStridedSeeds(1, maxSeed, seedStride) * flowCount;

        private ThreadShardDocument? TryLoadResumeShard(int threadId, ParallelScanWorkerState state)
        {
            ThreadShardDocument? shard = ScanShardMerger.TryLoadShard(threadId);
            if (shard == null)
            {
                return null;
            }

            if (shard.MaxSeed != _maxSeed
                || shard.PoolSize != _poolSize
                || shard.SeedStride != _seedStride
                || shard.SeedStart != state.SeedStart
                || shard.SeedEndExclusive != state.SeedEndExclusive)
            {
                Console.Error.WriteLine($"Ignoring shard {threadId} — parameters changed.");
                return null;
            }

            if (shard.ThreadCount > 0 && shard.ThreadCount != _threadCount)
            {
                Console.Error.WriteLine($"Ignoring shard {threadId} — thread count changed.");
                return null;
            }

            return shard;
        }

        private Dictionary<string, FlavorSeedTracker[]> CreateTrackers(ThreadShardDocument? resumeShard)
        {
            var trackersByFlow = new Dictionary<string, FlavorSeedTracker[]>(StringComparer.Ordinal);
            foreach (BakedFlow flow in _flows)
            {
                FlowShardCheckpoint? existingFlow = resumeShard?.Flows.FirstOrDefault(
                    entry => entry.FlowId.Equals(flow.FlowId, StringComparison.Ordinal));
                if (existingFlow != null)
                {
                    var trackerByFlavor = existingFlow.Flavors.ToDictionary(
                        flavor => flavor.Flavor,
                        StringComparer.Ordinal);
                    trackersByFlow[flow.FlowId] = DungeonSeedFlavorUtil.Curated
                        .Select(flavor => trackerByFlavor.TryGetValue(flavor.ToString(), out FlavorScanCheckpoint? checkpoint)
                            ? FlavorSeedTracker.FromCheckpoint(checkpoint)
                            : new FlavorSeedTracker(flavor))
                        .ToArray();
                }
                else
                {
                    trackersByFlow[flow.FlowId] = DungeonSeedFlavorUtil.Curated
                        .Select(flavor => new FlavorSeedTracker(flavor))
                        .ToArray();
                }
            }

            return trackersByFlow;
        }

        private static void UpdateShard(
            ThreadShardDocument shard,
            ParallelScanWorkerState state,
            Dictionary<string, FlavorSeedTracker[]> trackersByFlow,
            int seedsCompleted,
            bool isComplete)
        {
            shard.ThreadId = state.ThreadId;
            shard.SeedStart = state.SeedStart;
            shard.SeedEndExclusive = state.SeedEndExclusive;
            shard.SeedStride = state.SeedStride;
            shard.SeedsCompleted = seedsCompleted;
            shard.GenerationsCompleted = state.GenerationsCompleted;
            shard.IsComplete = isComplete;
            shard.Flows.Clear();
            foreach (string flowId in trackersByFlow.Keys.OrderBy(flowId => flowId, StringComparer.Ordinal))
            {
                shard.Flows.Add(new FlowShardCheckpoint
                {
                    FlowId = flowId,
                    Flavors = trackersByFlow[flowId]
                        .Select(tracker => tracker.ToCheckpoint())
                        .ToList(),
                });
            }
        }

        internal static int CountStridedSeeds(int seedStart, int seedEndExclusive, int stride)
        {
            if (seedStart >= seedEndExclusive || stride <= 0)
            {
                return 0;
            }

            return 1 + (seedEndExclusive - 1 - seedStart) / stride;
        }

        internal static int NthStridedSeed(int seedStart, int stride, int index) =>
            seedStart + index * stride;

        internal sealed class ParallelScanWorkerState
        {
            internal ParallelScanWorkerState(int threadId, int seedStart, int seedEndExclusive, int seedStride)
            {
                ThreadId = threadId;
                SeedStart = seedStart;
                SeedEndExclusive = seedEndExclusive;
                SeedStride = seedStride;
            }

            internal int ThreadId { get; }

            internal int SeedStart { get; }

            internal int SeedEndExclusive { get; }

            internal int SeedStride { get; }

            internal int SeedsCompleted { get; set; }

            internal long GenerationsCompleted { get; set; }

            internal bool IsComplete { get; set; }

            internal ThreadShardDocument? Shard { get; set; }
        }
    }
}
