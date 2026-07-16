namespace MimesisSeedScanner
{
    public static class SeedScanDefaults
    {
        /// <summary>Exclusive upper bound — scans seeds 1 .. MaxSeed-1 (full int range).</summary>
        public const int MaxSeed = int.MaxValue;

        public const int SeedStride = 100_000;

        public const int PoolSize = 500;
    }
}
