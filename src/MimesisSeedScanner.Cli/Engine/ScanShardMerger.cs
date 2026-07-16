using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Newtonsoft.Json;

namespace MimesisSeedScanner.Cli.Engine
{
    internal static class ScanShardMerger
    {
        internal static SeedScanDocument Merge(
            IReadOnlyList<ThreadShardDocument> shards,
            int maxSeed,
            int poolSize,
            int seedStride)
        {
            var merged = new SeedScanDocument
            {
                MaxSeed = maxSeed,
                PoolSize = poolSize,
                SeedStride = seedStride,
            };

            if (shards.Count == 0)
            {
                return merged;
            }

            var allMetrics = new List<GenerationMetrics>();
            foreach (ThreadShardDocument shard in shards)
            {
                foreach (FlowShardCheckpoint flow in shard.Flows)
                {
                    foreach (FlavorScanCheckpoint flavor in flow.Flavors)
                    {
                        foreach (SeedMetricsCheckpoint candidate in flavor.Candidates)
                        {
                            allMetrics.Add(SeedMetricsMapper.FromDto(candidate.Metrics));
                        }
                    }
                }
            }

            BalancedMedians medians = default;
            SeedScoring.UpdateMedians(allMetrics, ref medians);
            BalancedMedians.Current = medians;

            List<string> flowIds = shards
                .SelectMany(shard => shard.Flows)
                .Select(flow => flow.FlowId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(flowId => flowId, StringComparer.Ordinal)
                .ToList();

            foreach (string flowId in flowIds)
            {
                var trackers = DungeonSeedFlavorUtil.Curated
                    .Select(flavor => new FlavorSeedTracker(flavor))
                    .ToArray();

                foreach (ThreadShardDocument shard in shards)
                {
                    FlowShardCheckpoint? flow = shard.Flows.FirstOrDefault(
                        entry => entry.FlowId.Equals(flowId, StringComparison.Ordinal));
                    if (flow == null)
                    {
                        continue;
                    }

                    foreach (FlavorScanCheckpoint flavorCheckpoint in flow.Flavors)
                    {
                        FlavorSeedTracker? tracker = trackers.FirstOrDefault(
                            entry => entry.Flavor.Equals(flavorCheckpoint.Flavor, StringComparison.Ordinal));
                        if (tracker == null)
                        {
                            continue;
                        }

                        foreach (SeedMetricsCheckpoint candidate in flavorCheckpoint.Candidates)
                        {
                            tracker.Consider(
                                candidate.Seed,
                                SeedMetricsMapper.FromDto(candidate.Metrics));
                        }
                    }
                }

                var flowResult = new FlowSeedScanResult { FlowId = flowId };
                foreach (FlavorSeedTracker tracker in trackers)
                {
                    flowResult.Flavors.Add(new FlavorSeedScanResult
                    {
                        Flavor = tracker.Flavor,
                        Seeds = PoolSelector.SelectPool(
                            tracker.FlavorValue,
                            tracker.GetCandidates(),
                            poolSize),
                    });
                }

                merged.Flows.Add(flowResult);
            }

            return merged;
        }

        internal static void SaveShard(ThreadShardDocument shard)
        {
            Directory.CreateDirectory(ScanShardPaths.Directory);
            shard.LastSavedAt = DateTime.UtcNow;
            string json = JsonConvert.SerializeObject(shard, Formatting.Indented);
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
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ThreadShardDocument>(json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not read shard {threadId} — {ex.Message}");
                return null;
            }
        }

        internal static List<ThreadShardDocument> LoadAllShards()
        {
            if (!Directory.Exists(ScanShardPaths.Directory))
            {
                return [];
            }

            var shards = new List<ThreadShardDocument>();
            foreach (string path in Directory.EnumerateFiles(ScanShardPaths.Directory, "seed-scan-results.thread-*.json"))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    ThreadShardDocument? shard = JsonConvert.DeserializeObject<ThreadShardDocument>(json);
                    if (shard != null)
                    {
                        shards.Add(shard);
                    }
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
    }
}
