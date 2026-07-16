using Newtonsoft.Json;

namespace MimesisSeedScanner
{
    public sealed class SeedScanDocument
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 3;

        [JsonProperty("maxSeed")]
        public int MaxSeed { get; set; } = SeedScanDefaults.MaxSeed;

        [JsonProperty("poolSize")]
        public int PoolSize { get; set; } = SeedScanDefaults.PoolSize;

        [JsonProperty("seedStride")]
        public int SeedStride { get; set; } = SeedScanDefaults.SeedStride;

        [JsonProperty("flows")]
        public List<FlowSeedScanResult> Flows { get; set; } = [];

        [JsonProperty("scanComplete")]
        public bool ScanComplete { get; set; }

        [JsonProperty("scanInProgress")]
        public bool ScanInProgress { get; set; }

        [JsonProperty("generationsCompleted")]
        public long GenerationsCompleted { get; set; }

        [JsonProperty("totalGenerations")]
        public long TotalGenerations { get; set; }
    }

    public sealed class FlowSeedScanResult
    {
        [JsonProperty("flowId")]
        public string FlowId { get; set; } = string.Empty;

        [JsonProperty("flavors")]
        public List<FlavorSeedScanResult> Flavors { get; set; } = [];
    }

    public sealed class FlavorSeedScanResult
    {
        [JsonProperty("flavor")]
        public string Flavor { get; set; } = string.Empty;

        [JsonProperty("seeds")]
        public List<int> Seeds { get; set; } = [];
    }

    public sealed class FlavorScanCheckpoint
    {
        [JsonProperty("flavor")]
        public string Flavor { get; set; } = string.Empty;

        [JsonProperty("candidates")]
        public List<SeedMetricsCheckpoint> Candidates { get; set; } = [];
    }

    public sealed class SeedMetricsCheckpoint
    {
        [JsonProperty("seed")]
        public int Seed { get; set; }

        [JsonProperty("metrics")]
        public SeedMetricsDto Metrics { get; set; } = new();
    }

    public sealed class SeedMetricsDto
    {
        [JsonProperty("mainPathRoomCount")]
        public int MainPathRoomCount { get; set; }

        [JsonProperty("branchPathRoomCount")]
        public int BranchPathRoomCount { get; set; }

        [JsonProperty("totalRoomCount")]
        public int TotalRoomCount { get; set; }

        [JsonProperty("maxBranchDepth")]
        public int MaxBranchDepth { get; set; }

        [JsonProperty("totalRetries")]
        public int TotalRetries { get; set; }

        [JsonProperty("connectionCount")]
        public int ConnectionCount { get; set; }

        [JsonProperty("boundsVolume")]
        public float BoundsVolume { get; set; }

        [JsonProperty("unusedDoorwayCount")]
        public int UnusedDoorwayCount { get; set; }

        [JsonProperty("generationFailed")]
        public bool GenerationFailed { get; set; }
    }

    public sealed class ThreadShardDocument
    {
        [JsonProperty("threadId")]
        public int ThreadId { get; set; }

        [JsonProperty("seedStart")]
        public int SeedStart { get; set; }

        [JsonProperty("seedEndExclusive")]
        public int SeedEndExclusive { get; set; }

        [JsonProperty("maxSeed")]
        public int MaxSeed { get; set; }

        [JsonProperty("poolSize")]
        public int PoolSize { get; set; }

        [JsonProperty("seedStride")]
        public int SeedStride { get; set; } = SeedScanDefaults.SeedStride;

        [JsonProperty("threadCount")]
        public int ThreadCount { get; set; }

        [JsonProperty("seedsCompleted")]
        public int SeedsCompleted { get; set; }

        [JsonProperty("generationsCompleted")]
        public long GenerationsCompleted { get; set; }

        [JsonProperty("isComplete")]
        public bool IsComplete { get; set; }

        [JsonProperty("lastSavedAt")]
        public DateTime LastSavedAt { get; set; }

        [JsonProperty("flows")]
        public List<FlowShardCheckpoint> Flows { get; set; } = [];
    }

    public sealed class FlowShardCheckpoint
    {
        [JsonProperty("flowId")]
        public string FlowId { get; set; } = string.Empty;

        [JsonProperty("flavors")]
        public List<FlavorScanCheckpoint> Flavors { get; set; } = [];
    }
}
