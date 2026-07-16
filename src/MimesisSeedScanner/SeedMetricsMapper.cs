namespace MimesisSeedScanner
{
    public static class SeedMetricsMapper
    {
        public static SeedMetricsDto ToDto(GenerationMetrics metrics) =>
            new()
            {
                MainPathRoomCount = metrics.MainPathRoomCount,
                BranchPathRoomCount = metrics.BranchPathRoomCount,
                TotalRoomCount = metrics.TotalRoomCount,
                MaxBranchDepth = metrics.MaxBranchDepth,
                TotalRetries = metrics.TotalRetries,
                ConnectionCount = metrics.ConnectionCount,
                BoundsVolume = metrics.BoundsVolume,
                UnusedDoorwayCount = metrics.UnusedDoorwayCount,
                GenerationFailed = metrics.GenerationFailed,
            };

        public static GenerationMetrics FromDto(SeedMetricsDto dto) =>
            new(
                dto.MainPathRoomCount,
                dto.BranchPathRoomCount,
                dto.TotalRoomCount,
                dto.MaxBranchDepth,
                dto.TotalRetries,
                dto.ConnectionCount,
                dto.BoundsVolume,
                dto.UnusedDoorwayCount,
                dto.GenerationFailed);
    }
}
