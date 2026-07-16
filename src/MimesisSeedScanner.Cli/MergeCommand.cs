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

            List<ThreadShardDocument> shards = ScanShardMerger.LoadAllShards();
            if (shards.Count == 0)
            {
                Console.Error.WriteLine($"No shard files found in {ScanShardPaths.Directory} (expected seed-scan-results.thread-*.json).");
                return 1;
            }

            ThreadShardDocument first = shards[0];
            int resolvedMaxSeed = maxSeed ?? first.MaxSeed;
            int resolvedPoolSize = poolSize ?? first.PoolSize;
            int resolvedSeedStride = seedStride ?? first.SeedStride;

            if (!ScanShardMerger.TryValidateResume(
                    shards,
                    shards.Count,
                    resolvedMaxSeed,
                    resolvedPoolSize,
                    resolvedSeedStride,
                    out string resumeError))
            {
                Console.Error.WriteLine($"Cannot merge — {resumeError}");
                return 1;
            }

            int flowCount = shards
                .SelectMany(shard => shard.Flows)
                .Select(flow => flow.FlowId)
                .Distinct(StringComparer.Ordinal)
                .Count();

            SeedScanDocument merged = ScanShardMerger.Merge(
                shards,
                resolvedMaxSeed,
                resolvedPoolSize,
                resolvedSeedStride);

            long totalGenerations = ParallelOfflineScanner.ComputeTotalGenerations(
                resolvedMaxSeed,
                resolvedSeedStride,
                flowCount);
            long generationsCompleted = shards.Sum(shard => shard.GenerationsCompleted);
            bool scanComplete = shards.All(shard => shard.IsComplete);

            merged.ScanComplete = scanComplete;
            merged.ScanInProgress = !scanComplete;
            merged.GenerationsCompleted = generationsCompleted;
            merged.TotalGenerations = totalGenerations;

            string json = JsonConvert.SerializeObject(merged, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.WriteAllText(outputPath, json);

            Console.WriteLine(
                $"Merged {shards.Count} shard(s) → {outputPath} ({merged.Flows.Count} flows, complete={scanComplete}, "
                + $"generations {generationsCompleted:N0}/{totalGenerations:N0}).");
            Console.WriteLine("Next: dotnet run --project src/MimesisSeedScanner.Cli -- codegen " + outputPath);
            return 0;
        }
    }
}
