using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Newtonsoft.Json;

namespace MimesisSeedScanner.Cli.Engine
{
    internal static class ScanShardMerger
    {
        private const int MedianSampleSize = 100_000;

        internal static SeedScanDocument Merge(
            IReadOnlyList<ThreadShardDocument> shards,
            int maxSeed,
            int poolSize,
            int seedStride)
        {
            var accumulator = new ShardMergeAccumulator(poolSize);
            foreach (ThreadShardDocument shard in shards)
            {
                accumulator.AddShard(shard);
            }

            return accumulator.ToDocument(maxSeed, poolSize, seedStride);
        }

        internal static List<string> ListShardPaths()
        {
            if (!Directory.Exists(ScanShardPaths.Directory))
            {
                return [];
            }

            return Directory
                .EnumerateFiles(ScanShardPaths.Directory, "seed-scan-results.thread-*.json")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        internal static ThreadShardDocument LoadShardFile(string path)
        {
            string json = File.ReadAllText(path);
            ThreadShardDocument? shard = JsonConvert.DeserializeObject<ThreadShardDocument>(json);
            if (shard == null)
            {
                throw new InvalidDataException($"Shard file is empty or invalid: {path}");
            }

            return shard;
        }

        internal static void SaveShard(ThreadShardDocument shard)
        {
            Directory.CreateDirectory(ScanShardPaths.Directory);
            shard.LastSavedAt = DateTime.UtcNow;
            string json = JsonConvert.SerializeObject(shard, Formatting.None);
            string path = ScanShardPaths.ShardPath(shard.ThreadId);
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, path, overwrite: true);
            File.Delete(tempPath);
        }

        internal static ThreadShardDocument? TryLoadShard(int threadId)
        {
            string path = ScanShardPaths.ShardPath(threadId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return LoadShardFile(path);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not read shard {threadId} — {ex.Message}");
                return null;
            }
        }

        internal static List<ThreadShardDocument> LoadAllShards()
        {
            List<string> paths = ListShardPaths();
            var shards = new List<ThreadShardDocument>(paths.Count);
            foreach (string path in paths)
            {
                try
                {
                    shards.Add(LoadShardFile(path));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Could not read shard '{path}' — {ex.Message}");
                }
            }

            return shards
                .OrderBy(shard => shard.ThreadId)
                .ToList();
        }

        internal static bool TryValidateResume(
            IReadOnlyList<ThreadShardDocument> shards,
            int threadCount,
            int maxSeed,
            int poolSize,
            int seedStride,
            out string reason)
        {
            reason = string.Empty;
            if (shards.Count == 0)
            {
                return true;
            }

            ThreadShardDocument first = shards[0];
            if (first.MaxSeed != maxSeed)
            {
                reason = $"maxSeed mismatch (checkpoint {first.MaxSeed}, requested {maxSeed})";
                return false;
            }

            if (first.PoolSize != poolSize)
            {
                reason = $"poolSize mismatch (checkpoint {first.PoolSize}, requested {poolSize})";
                return false;
            }

            if (first.SeedStride != seedStride)
            {
                reason = $"seedStride mismatch (checkpoint {first.SeedStride}, requested {seedStride})";
                return false;
            }

            if (first.ThreadCount > 0 && first.ThreadCount != threadCount)
            {
                reason = $"thread count mismatch (checkpoint {first.ThreadCount}, current {threadCount})";
                return false;
            }

            HashSet<int> seenThreadIds = [];
            foreach (ThreadShardDocument shard in shards)
            {
                if (shard.MaxSeed != maxSeed || shard.PoolSize != poolSize || shard.SeedStride != seedStride)
                {
                    reason = "shard parameters are inconsistent";
                    return false;
                }

                if (shard.ThreadCount > 0 && shard.ThreadCount != threadCount)
                {
                    reason = "shard thread counts are inconsistent";
                    return false;
                }

                if (!seenThreadIds.Add(shard.ThreadId))
                {
                    reason = $"duplicate shard for thread {shard.ThreadId}";
                    return false;
                }
            }

            return true;
        }

        internal static void DeleteShards(int threadCount)
        {
            for (int i = 0; i < threadCount; i++)
            {
                string path = ScanShardPaths.ShardPath(i);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                string tempPath = path + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }

            if (Directory.Exists(ScanShardPaths.Directory)
                && !Directory.EnumerateFileSystemEntries(ScanShardPaths.Directory).Any())
            {
                Directory.Delete(ScanShardPaths.Directory);
            }
        }

        internal sealed class ShardMergeAccumulator
        {
            private readonly int _poolSize;
            private readonly Dictionary<string, Dictionary<DungeonSeedFlavor, Dictionary<int, GenerationMetrics>>> _flows =
                new(StringComparer.Ordinal);

            private readonly List<GenerationMetrics> _medianSample = [];
            private readonly Random _medianRandom = new(42);
            private int _medianItemsSeen;

            internal ShardMergeAccumulator(int poolSize)
            {
                _poolSize = poolSize;
            }

            internal void AddShard(ThreadShardDocument shard)
            {
                foreach (FlowShardCheckpoint flow in shard.Flows)
                {
                    AddFlowCandidates(flow.FlowId, flow.Flavors);
                }
            }

            internal void AddTrackers(IReadOnlyDictionary<string, FlavorSeedTracker[]> trackersByFlow)
            {
                foreach (KeyValuePair<string, FlavorSeedTracker[]> entry in trackersByFlow)
                {
                    var flavorCheckpoints = new List<FlavorScanCheckpoint>(entry.Value.Length);
                    foreach (FlavorSeedTracker tracker in entry.Value)
                    {
                        flavorCheckpoints.Add(tracker.ToCheckpoint());
                    }

                    AddFlowCandidates(entry.Key, flavorCheckpoints);
                }
            }

            private void AddFlowCandidates(string flowId, IReadOnlyList<FlavorScanCheckpoint> flavors)
            {
                Dictionary<DungeonSeedFlavor, Dictionary<int, GenerationMetrics>> byFlavor = GetOrCreateFlow(flowId);
                foreach (FlavorScanCheckpoint flavorCheckpoint in flavors)
                {
                    if (!DungeonSeedFlavorUtil.TryParse(flavorCheckpoint.Flavor, out DungeonSeedFlavor flavor))
                    {
                        continue;
                    }

                    if (!byFlavor.TryGetValue(flavor, out Dictionary<int, GenerationMetrics>? bySeed))
                    {
                        continue;
                    }

                    foreach (SeedMetricsCheckpoint candidate in flavorCheckpoint.Candidates)
                    {
                        GenerationMetrics metrics = SeedMetricsMapper.FromDto(candidate.Metrics);
                        MaybeSampleMedian(metrics);

                        if (!bySeed.TryGetValue(candidate.Seed, out GenerationMetrics existing)
                            || SeedScoring.IsBetter(flavor, metrics, existing))
                        {
                            bySeed[candidate.Seed] = metrics;
                        }
                    }
                }
            }

            internal SeedScanDocument ToDocument(int maxSeed, int poolSize, int seedStride)
            {
                BalancedMedians medians = default;
                SeedScoring.UpdateMedians(_medianSample, ref medians);
                BalancedMedians.Current = medians;

                var merged = new SeedScanDocument
                {
                    MaxSeed = maxSeed,
                    PoolSize = poolSize,
                    SeedStride = seedStride,
                };

                foreach (string flowId in _flows.Keys.OrderBy(flowId => flowId, StringComparer.Ordinal))
                {
                    Dictionary<DungeonSeedFlavor, Dictionary<int, GenerationMetrics>> byFlavor = _flows[flowId];
                    var flowResult = new FlowSeedScanResult { FlowId = flowId };
                    foreach (DungeonSeedFlavor flavor in DungeonSeedFlavorUtil.Curated)
                    {
                        Dictionary<int, GenerationMetrics> bySeed = byFlavor[flavor];
                        List<(int Seed, GenerationMetrics Metrics)> candidates = bySeed
                            .Select(entry => (entry.Key, entry.Value))
                            .ToList();

                        flowResult.Flavors.Add(new FlavorSeedScanResult
                        {
                            Flavor = flavor.ToString(),
                            Seeds = PoolSelector.SelectPool(flavor, candidates, _poolSize),
                        });
                    }

                    merged.Flows.Add(flowResult);
                }

                return merged;
            }

            private Dictionary<DungeonSeedFlavor, Dictionary<int, GenerationMetrics>> GetOrCreateFlow(string flowId)
            {
                if (_flows.TryGetValue(flowId, out Dictionary<DungeonSeedFlavor, Dictionary<int, GenerationMetrics>>? existing))
                {
                    return existing;
                }

                var byFlavor = new Dictionary<DungeonSeedFlavor, Dictionary<int, GenerationMetrics>>();
                foreach (DungeonSeedFlavor flavor in DungeonSeedFlavorUtil.Curated)
                {
                    byFlavor[flavor] = new Dictionary<int, GenerationMetrics>();
                }

                _flows[flowId] = byFlavor;
                return byFlavor;
            }

            private void MaybeSampleMedian(GenerationMetrics metrics)
            {
                if (metrics.GenerationFailed)
                {
                    return;
                }

                if (_medianSample.Count < MedianSampleSize)
                {
                    _medianSample.Add(metrics);
                }
                else
                {
                    int replaceIndex = _medianRandom.Next(_medianItemsSeen);
                    if (replaceIndex < MedianSampleSize)
                    {
                        _medianSample[replaceIndex] = metrics;
                    }
                }

                _medianItemsSeen++;
            }
        }
    }
}
