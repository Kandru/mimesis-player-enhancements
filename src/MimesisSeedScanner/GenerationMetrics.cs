namespace MimesisSeedScanner
{
    public readonly struct GenerationMetrics
    {
        public GenerationMetrics(
            int mainPathRoomCount,
            int branchPathRoomCount,
            int totalRoomCount,
            int maxBranchDepth,
            int totalRetries,
            int connectionCount,
            float boundsVolume,
            int unusedDoorwayCount,
            bool generationFailed)
        {
            MainPathRoomCount = mainPathRoomCount;
            BranchPathRoomCount = branchPathRoomCount;
            TotalRoomCount = totalRoomCount;
            MaxBranchDepth = maxBranchDepth;
            TotalRetries = totalRetries;
            ConnectionCount = connectionCount;
            BoundsVolume = boundsVolume;
            UnusedDoorwayCount = unusedDoorwayCount;
            GenerationFailed = generationFailed;
        }

        public int MainPathRoomCount { get; }

        public int BranchPathRoomCount { get; }

        public int TotalRoomCount { get; }

        public int MaxBranchDepth { get; }

        public int TotalRetries { get; }

        public int ConnectionCount { get; }

        public float BoundsVolume { get; }

        public int UnusedDoorwayCount { get; }

        public bool GenerationFailed { get; }
    }
}
