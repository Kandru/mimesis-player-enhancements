using MimesisSeedScanner;
using MimesisSeedScanner.Cli.Engine;
using Newtonsoft.Json;

namespace MimesisSeedScanner.Cli
{
    internal static class MergeCommand
    {
        internal static int Run(string[] args)
        {
            string? shardDir = CliArgs.Get(args, "--shard-dir");
            string outputPath = CliArgs.Get(args, "--output") ?? CliArgs.Get(args, "-o") ?? "seed-scan-results.json";
            int? maxSeed = CliArgs.TryGetInt(args, "--max-seed");
            int? poolSize = CliArgs.TryGetInt(args, "--pool-size");
            int? seedStride = CliArgs.TryGetInt(args, "--seed-stride");

            if (!string.IsNullOrWhiteSpace(shardDir))
            {
                ScanShardPaths.Directory = shardDir;
            }

            List<string> shardPaths = ScanShardMerger.ListShardPaths();
            if (shardPaths.Count == 0)
            {
                Console.Error.WriteLine($"No shard files found in {ScanShardPaths.Directory} (expected seed-scan-results.thread-*.json).");
                return 1;
            }

            ThreadShardDocument? first = null;
            int resolvedMaxSeed = 0;
            int resolvedPoolSize = 0;
            int resolvedSeedStride = 0;
            long generationsCompleted = 0;
            bool scanComplete = true;
            var seenThreadIds = new HashSet<int>();
            var accumulator = new ScanShardMerger.ShardMergeAccumulator(SeedScanDefaults.PoolSize);

            for (int i = 0; i < shardPaths.Count; i++)
            {
                string path = shardPaths[i];
                Console.WriteLine($"Loading shard {i + 1}/{shardPaths.Count} — {Path.GetFileName(path)}");
                ThreadShardDocument shard;
                try
                {
                    shard = ScanShardMerger.LoadShardFile(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Cannot read shard — {ex.Message}");
                    return 1;
                }

                if (first == null)
                {
                    first = shard;
                    resolvedMaxSeed = maxSeed ?? first.MaxSeed;
                    resolvedPoolSize = poolSize ?? first.PoolSize;
                    resolvedSeedStride = seedStride ?? first.SeedStride;
                    accumulator = new ScanShardMerger.ShardMergeAccumulator(resolvedPoolSize);
                }
                else if (!ShardParametersMatch(first, shard, resolvedMaxSeed, resolvedPoolSize, resolvedSeedStride, out string mismatch))
                {
                    Console.Error.WriteLine($"Cannot merge — {mismatch}");
                    return 1;
                }

                if (!seenThreadIds.Add(shard.ThreadId))
                {
                    Console.Error.WriteLine($"Cannot merge — duplicate shard for thread {shard.ThreadId}");
                    return 1;
                }

                generationsCompleted += shard.GenerationsCompleted;
                scanComplete &= shard.IsComplete;

                Console.WriteLine($"Merging shard {i + 1}/{shardPaths.Count} — {Path.GetFileName(path)}");
                accumulator.AddShard(shard);
            }

            if (first!.ThreadCount > 0 && seenThreadIds.Count != first.ThreadCount)
            {
                Console.WriteLine(
                    $"Warning — expected {first.ThreadCount} shard(s), found {seenThreadIds.Count}. Merging available shards.");
            }

            Console.WriteLine("Selecting pools per flow and flavor…");
            SeedScanDocument merged = accumulator.ToDocument(resolvedMaxSeed, resolvedPoolSize, resolvedSeedStride);

            int flowCount = merged.Flows.Count;
            long totalGenerations = ParallelOfflineScanner.ComputeTotalGenerations(
                resolvedMaxSeed,
                resolvedSeedStride,
                flowCount);

            merged.ScanComplete = scanComplete;
            merged.ScanInProgress = !scanComplete;
            merged.GenerationsCompleted = generationsCompleted;
            merged.TotalGenerations = totalGenerations;

            Console.WriteLine($"Writing {outputPath}…");
            string json = JsonConvert.SerializeObject(merged, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.WriteAllText(outputPath, json);

            Console.WriteLine(
                $"Merged {shardPaths.Count} shard(s) → {outputPath} ({merged.Flows.Count} flows, complete={scanComplete}, "
                + $"generations {generationsCompleted:N0}/{totalGenerations:N0}).");
            Console.WriteLine("Next: dotnet run --project src/MimesisSeedScanner.Cli -- codegen " + outputPath);
            return 0;
        }

        private static bool ShardParametersMatch(
            ThreadShardDocument first,
            ThreadShardDocument shard,
            int maxSeed,
            int poolSize,
            int seedStride,
            out string reason)
        {
            if (shard.MaxSeed != maxSeed)
            {
                reason = $"maxSeed mismatch in thread {shard.ThreadId} (checkpoint {shard.MaxSeed}, expected {maxSeed})";
                return false;
            }

            if (shard.PoolSize != poolSize)
            {
                reason = $"poolSize mismatch in thread {shard.ThreadId}";
                return false;
            }

            if (shard.SeedStride != seedStride)
            {
                reason = $"seedStride mismatch in thread {shard.ThreadId}";
                return false;
            }

            if (first.ThreadCount > 0 && shard.ThreadCount > 0 && shard.ThreadCount != first.ThreadCount)
            {
                reason = $"thread count mismatch in thread {shard.ThreadId}";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
