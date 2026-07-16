using MimesisSeedScanner.Cli.Engine;
using Newtonsoft.Json;

namespace MimesisSeedScanner.Cli
{
    internal static class VerifyCommand
    {
        internal static int Run(string[] args)
        {
            string? catalogPath = CliArgs.Get(args, "--catalog") ?? CliArgs.Get(args, "-c");
            string? flowId = CliArgs.Get(args, "--flow");
            string? seedsArg = CliArgs.Get(args, "--seeds");

            if (string.IsNullOrWhiteSpace(catalogPath) || string.IsNullOrWhiteSpace(flowId) || string.IsNullOrWhiteSpace(seedsArg))
            {
                Console.Error.WriteLine("Usage: verify --catalog scan-catalog.json --flow FlowId --seeds 1,2,3");
                return 1;
            }

            ScanCatalogDocument? document = JsonConvert.DeserializeObject<ScanCatalogDocument>(File.ReadAllText(catalogPath));
            BakedFlow? flow = document?.Catalog.Flows.FirstOrDefault(
                entry => entry.FlowId.Equals(flowId, StringComparison.Ordinal));
            if (flow == null)
            {
                Console.Error.WriteLine($"Flow '{flowId}' not found in catalog.");
                return 1;
            }

            foreach (string part in seedsArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(part, out int seed))
                {
                    continue;
                }

                if (!OfflineDungeonGenerator.TryGenerateMetrics(document!.Catalog, flow, seed, out GenerationMetrics metrics))
                {
                    Console.WriteLine($"seed={seed} FAILED");
                    continue;
                }

                Console.WriteLine(
                    $"seed={seed} main={metrics.MainPathRoomCount} branch={metrics.BranchPathRoomCount} "
                    + $"total={metrics.TotalRoomCount} depth={metrics.MaxBranchDepth} "
                    + $"conn={metrics.ConnectionCount} unused={metrics.UnusedDoorwayCount} "
                    + $"volume={metrics.BoundsVolume:F0} retries={metrics.TotalRetries}");
            }

            return 0;
        }
    }
}
