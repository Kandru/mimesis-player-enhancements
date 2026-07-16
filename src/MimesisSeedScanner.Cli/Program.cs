using Newtonsoft.Json;

namespace MimesisSeedScanner.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return 0;
            }

            string command = args[0];
            string[] commandArgs = args.Skip(1).ToArray();

            if (string.Equals(command, "codegen", StringComparison.OrdinalIgnoreCase))
            {
                return RunCodegen(commandArgs);
            }

            if (string.Equals(command, "scan", StringComparison.OrdinalIgnoreCase))
            {
                return ScanCommand.Run(commandArgs);
            }

            if (string.Equals(command, "merge", StringComparison.OrdinalIgnoreCase))
            {
                return MergeCommand.Run(commandArgs);
            }

            if (string.Equals(command, "verify", StringComparison.OrdinalIgnoreCase))
            {
                return VerifyCommand.Run(commandArgs);
            }

            Console.Error.WriteLine($"Unknown command '{command}'.");
            PrintHelp();
            return 1;
        }

        private static int RunCodegen(string[] args)
        {
            string inputPath = CliArgs.Get(args, "--input") ?? CliArgs.Get(args, "-i") ?? "seed-scan-results.json";
            string? outputPath = CliArgs.Get(args, "--output") ?? CliArgs.Get(args, "-o")
                ?? "src/MimesisPlayerEnhancement/Features/DungeonRandomizer/DungeonSeedPools.Generated.cs";

            if (args.Length >= 1 && !args[0].StartsWith("-", StringComparison.Ordinal))
            {
                inputPath = args[0];
            }

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Input file not found: {inputPath}");
                return 1;
            }

            string json = File.ReadAllText(inputPath);
            SeedScanDocument? document = JsonConvert.DeserializeObject<SeedScanDocument>(json);

            if (document == null || document.Flows.Count == 0)
            {
                Console.Error.WriteLine("Seed scan document is empty or invalid.");
                return 1;
            }

            string generated = SeedPoolCodeGenerator.Generate(document);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, generated);
            Console.WriteLine($"Wrote {outputPath}");
            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("MimesisSeedScanner CLI");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  scan --catalog scan-catalog.json [--output seed-scan-results.json]");
            Console.WriteLine("       [--max-seed N] (default: int.MaxValue) [--pool-size N] (default: 500)");
            Console.WriteLine("       [--seed-stride N] (default: 100000) [--threads N]");
            Console.WriteLine("       [--checkpoint-every N] (default: 0 — in RAM; set e.g. 250 to resume mid-scan)");
            Console.WriteLine("       [--time-budget 4h|30m|3600s] [--shard-dir path]");
            Console.WriteLine("  merge [--shard-dir seed-scan-shards] [--output seed-scan-results.json]");
            Console.WriteLine("        Merge thread shard checkpoints into final scan JSON (no re-scan).");
            Console.WriteLine("  codegen <seed-scan-results.json> [--output DungeonSeedPools.Generated.cs]");
            Console.WriteLine("  verify --catalog scan-catalog.json --flow FlowId --seeds 1,2,3");
        }
    }
}
