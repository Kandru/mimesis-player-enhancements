using System.Diagnostics;
using MimesisSeedScanner.Cli.Engine;
using Newtonsoft.Json;

namespace MimesisSeedScanner.Cli
{
    internal static class ScanCommand
    {
        internal static int Run(string[] args)
        {
            string? catalogPath = CliArgs.Get(args, "--catalog") ?? CliArgs.Get(args, "-c");
            string? outputPath = CliArgs.Get(args, "--output") ?? CliArgs.Get(args, "-o") ?? "seed-scan-results.json";
            string? shardDir = CliArgs.Get(args, "--shard-dir");
            int maxSeed = CliArgs.GetInt(args, "--max-seed", SeedScanDefaults.MaxSeed);
            int poolSize = CliArgs.GetInt(args, "--pool-size", SeedScanDefaults.PoolSize);
            int seedStride = CliArgs.GetInt(args, "--seed-stride", SeedScanDefaults.SeedStride);
            int threads = CliArgs.GetInt(args, "--threads", Environment.ProcessorCount);
            TimeSpan? timeBudget = CliArgs.GetTimeBudget(args, "--time-budget");

            if (string.IsNullOrWhiteSpace(catalogPath))
            {
                Console.Error.WriteLine("Missing required --catalog path to scan-catalog.json");
                return 1;
            }

            if (!File.Exists(catalogPath))
            {
                Console.Error.WriteLine($"Catalog file not found: {catalogPath}");
                return 1;
            }

            ScanCatalogDocument? document = JsonConvert.DeserializeObject<ScanCatalogDocument>(File.ReadAllText(catalogPath));
            if (document?.Catalog.Flows.Count == 0)
            {
                Console.Error.WriteLine("Catalog has no flows.");
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(shardDir))
            {
                ScanShardPaths.Directory = shardDir;
            }

            List<ThreadShardDocument> resumeShards = ScanShardMerger.LoadAllShards();
            var scanner = new ParallelOfflineScanner(
                document!.Catalog,
                document.Catalog.Flows,
                maxSeed,
                poolSize,
                seedStride,
                threads,
                timeBudget);

            if (resumeShards.Count > 0
                && !ScanShardMerger.TryValidateResume(resumeShards, threads, maxSeed, poolSize, seedStride, out string resumeError))
            {
                Console.Error.WriteLine($"Cannot resume — {resumeError}. Delete {ScanShardPaths.Directory} to start fresh.");
                return 1;
            }

            if (resumeShards.Count > 0)
            {
                Console.WriteLine($"Resuming from {resumeShards.Count} shard checkpoint(s).");
            }

            Console.WriteLine(
                $"Starting scan — {threads} thread(s), {document.Catalog.Flows.Count} flow(s), "
                + $"seeds 1..{maxSeed - 1} stride {seedStride}, pool {poolSize}."
                + (timeBudget.HasValue ? $" Time budget: {timeBudget.Value}." : string.Empty));

            scanner.Start();
            var stopwatch = Stopwatch.StartNew();
            while (!scanner.IsComplete)
            {
                Thread.Sleep(5000);
                double rate = scanner.OverallGenerationsPerSecond(stopwatch.Elapsed.TotalSeconds);
                long done = scanner.GenerationsCompleted;
                long total = scanner.TotalGenerations;
                double percent = total > 0 ? done * 100.0 / total : 100.0;
                Console.WriteLine($"Progress — {percent:F1}% ({done:N0}/{total:N0}), {rate:F0} layouts/s");
            }

            scanner.Join();
            SeedScanDocument merged = scanner.MergeResults();
            merged.ScanComplete = scanner.GenerationsCompleted >= scanner.TotalGenerations;
            merged.ScanInProgress = !merged.ScanComplete;
            merged.GenerationsCompleted = scanner.GenerationsCompleted;
            merged.TotalGenerations = scanner.TotalGenerations;

            string json = JsonConvert.SerializeObject(merged, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.WriteAllText(outputPath, json);
            if (merged.ScanComplete)
            {
                ScanShardMerger.DeleteShards(threads);
            }

            Console.WriteLine($"Wrote {outputPath} ({merged.Flows.Count} flows, complete={merged.ScanComplete}).");
            return 0;
        }
    }
}
