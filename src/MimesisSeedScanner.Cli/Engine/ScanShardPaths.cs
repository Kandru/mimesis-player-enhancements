namespace MimesisSeedScanner.Cli.Engine
{
    internal static class ScanShardPaths
    {
        internal static string Directory { get; set; } = "seed-scan-shards";

        internal static string ShardPath(int threadId) =>
            Path.Combine(Directory, $"seed-scan-results.thread-{threadId}.json");
    }
}
