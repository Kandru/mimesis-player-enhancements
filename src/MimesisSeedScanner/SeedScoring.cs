using MimesisPlayerEnhancement.Features.DungeonRandomizer;

namespace MimesisSeedScanner
{
    public static class SeedScoring
    {
        public static readonly DungeonSeedFlavor[] CuratedFlavors = DungeonSeedFlavorUtil.Curated;

        public static bool IsBetter(DungeonSeedFlavor flavor, GenerationMetrics candidate, GenerationMetrics current)
        {
            if (candidate.GenerationFailed)
            {
                return false;
            }

            if (current.GenerationFailed)
            {
                return true;
            }

            return flavor switch
            {
                DungeonSeedFlavor.Compact => Less(candidate.TotalRoomCount, current.TotalRoomCount)
                             || (candidate.TotalRoomCount == current.TotalRoomCount
                                 && Less(candidate.MainPathRoomCount, current.MainPathRoomCount)),
                DungeonSeedFlavor.Expansive => Greater(candidate.TotalRoomCount, current.TotalRoomCount),
                DungeonSeedFlavor.ShortMainPath => Less(candidate.MainPathRoomCount, current.MainPathRoomCount)
                                   || (candidate.MainPathRoomCount == current.MainPathRoomCount
                                       && Less(candidate.TotalRoomCount, current.TotalRoomCount)),
                DungeonSeedFlavor.LongMainPath => Greater(candidate.MainPathRoomCount, current.MainPathRoomCount)
                                  || (candidate.MainPathRoomCount == current.MainPathRoomCount
                                      && Greater(candidate.TotalRoomCount, current.TotalRoomCount)),
                DungeonSeedFlavor.Sprawling => Greater(candidate.BoundsVolume, current.BoundsVolume),
                DungeonSeedFlavor.Dense => Less(Density(candidate), Density(current)),
                DungeonSeedFlavor.Cramped => Greater(candidate.TotalRoomCount, current.TotalRoomCount)
                             && (Less(CrampedScore(candidate), CrampedScore(current))
                                 || (Math.Abs(CrampedScore(candidate) - CrampedScore(current)) < 0.0001f
                                     && Greater(candidate.TotalRoomCount, current.TotalRoomCount))),
                DungeonSeedFlavor.Linear => Greater(LinearRatio(candidate), LinearRatio(current)),
                DungeonSeedFlavor.MinimalBranches => Less(candidate.BranchPathRoomCount, current.BranchPathRoomCount)
                                     || (candidate.BranchPathRoomCount == current.BranchPathRoomCount
                                         && Less(candidate.MaxBranchDepth, current.MaxBranchDepth)),
                DungeonSeedFlavor.Branching => Greater(BranchScore(candidate), BranchScore(current)),
                DungeonSeedFlavor.BroadBranches => Greater(candidate.BranchPathRoomCount, current.BranchPathRoomCount)
                                   && (Less(candidate.MaxBranchDepth, current.MaxBranchDepth)
                                       || (candidate.MaxBranchDepth == current.MaxBranchDepth
                                           && Greater(candidate.BranchPathRoomCount, current.BranchPathRoomCount))),
                DungeonSeedFlavor.Deep => Greater(candidate.MaxBranchDepth, current.MaxBranchDepth)
                          || (candidate.MaxBranchDepth == current.MaxBranchDepth
                              && Greater(candidate.BranchPathRoomCount, current.BranchPathRoomCount)),
                DungeonSeedFlavor.Open => Greater(candidate.ConnectionCount, current.ConnectionCount),
                DungeonSeedFlavor.Maze => Greater(candidate.UnusedDoorwayCount, current.UnusedDoorwayCount),
                DungeonSeedFlavor.Loopy => Greater(LoopyScore(candidate), LoopyScore(current)),
                DungeonSeedFlavor.DeadEnds => Greater(DeadEndScore(candidate), DeadEndScore(current)),
                DungeonSeedFlavor.TightCorridor => Less(candidate.TotalRoomCount, current.TotalRoomCount)
                                   || (candidate.TotalRoomCount == current.TotalRoomCount
                                       && Greater(LinearRatio(candidate), LinearRatio(current))),
                DungeonSeedFlavor.Labyrinth => Greater(candidate.TotalRoomCount, current.TotalRoomCount)
                               || (candidate.TotalRoomCount == current.TotalRoomCount
                                   && Greater(BranchScore(candidate), BranchScore(current))),
                DungeonSeedFlavor.Honeycomb => Greater(OpenDensity(candidate), OpenDensity(current))
                               || (Math.Abs(OpenDensity(candidate) - OpenDensity(current)) < 0.0001f
                                   && Less(candidate.TotalRoomCount, current.TotalRoomCount)),
                DungeonSeedFlavor.WideOpen => Greater(candidate.ConnectionCount, current.ConnectionCount)
                                || (candidate.ConnectionCount == current.ConnectionCount
                                    && Greater(candidate.BoundsVolume, current.BoundsVolume)),
                DungeonSeedFlavor.StableCompact => Less(candidate.TotalRetries, current.TotalRetries)
                                   || (candidate.TotalRetries == current.TotalRetries
                                       && Less(candidate.TotalRoomCount, current.TotalRoomCount)),
                DungeonSeedFlavor.DeepMaze => Greater(candidate.MaxBranchDepth, current.MaxBranchDepth)
                                || (candidate.MaxBranchDepth == current.MaxBranchDepth
                                    && Greater(candidate.UnusedDoorwayCount, current.UnusedDoorwayCount)),
                DungeonSeedFlavor.Reliable => Less(candidate.TotalRetries, current.TotalRetries),
                DungeonSeedFlavor.Balanced => Less(BalancedDistance(candidate), BalancedDistance(current)),
                _ => false,
            };
        }

        internal static float GetScore(DungeonSeedFlavor flavor, GenerationMetrics metrics)
        {
            if (metrics.GenerationFailed)
            {
                return float.MinValue;
            }

            return flavor switch
            {
                DungeonSeedFlavor.Compact => -metrics.TotalRoomCount,
                DungeonSeedFlavor.Expansive => metrics.TotalRoomCount,
                DungeonSeedFlavor.ShortMainPath => -metrics.MainPathRoomCount,
                DungeonSeedFlavor.LongMainPath => metrics.MainPathRoomCount,
                DungeonSeedFlavor.Sprawling => metrics.BoundsVolume,
                DungeonSeedFlavor.Dense => -Density(metrics),
                DungeonSeedFlavor.Cramped => metrics.TotalRoomCount - CrampedScore(metrics) * 1000f,
                DungeonSeedFlavor.Linear => LinearRatio(metrics),
                DungeonSeedFlavor.MinimalBranches => -metrics.BranchPathRoomCount,
                DungeonSeedFlavor.Branching => BranchScore(metrics),
                DungeonSeedFlavor.BroadBranches => metrics.BranchPathRoomCount - metrics.MaxBranchDepth * 10f,
                DungeonSeedFlavor.Deep => metrics.MaxBranchDepth,
                DungeonSeedFlavor.Open => metrics.ConnectionCount,
                DungeonSeedFlavor.Maze => metrics.UnusedDoorwayCount,
                DungeonSeedFlavor.Loopy => LoopyScore(metrics),
                DungeonSeedFlavor.DeadEnds => DeadEndScore(metrics),
                DungeonSeedFlavor.TightCorridor => LinearRatio(metrics) - metrics.TotalRoomCount * 0.01f,
                DungeonSeedFlavor.Labyrinth => metrics.TotalRoomCount + BranchScore(metrics) * 0.1f,
                DungeonSeedFlavor.Honeycomb => OpenDensity(metrics),
                DungeonSeedFlavor.WideOpen => metrics.ConnectionCount + metrics.BoundsVolume * 0.001f,
                DungeonSeedFlavor.StableCompact => -metrics.TotalRetries - metrics.TotalRoomCount * 0.01f,
                DungeonSeedFlavor.DeepMaze => metrics.MaxBranchDepth + metrics.UnusedDoorwayCount * 0.1f,
                DungeonSeedFlavor.Reliable => -metrics.TotalRetries,
                DungeonSeedFlavor.Balanced => -BalancedDistance(metrics),
                _ => float.MinValue,
            };
        }

        public static void UpdateMedians(IReadOnlyList<GenerationMetrics> metrics, ref BalancedMedians medians)
        {
            if (metrics.Count == 0)
            {
                return;
            }

            var rooms = new float[metrics.Count];
            var branches = new float[metrics.Count];
            var connections = new float[metrics.Count];
            for (int i = 0; i < metrics.Count; i++)
            {
                rooms[i] = metrics[i].TotalRoomCount;
                branches[i] = BranchScore(metrics[i]);
                connections[i] = metrics[i].ConnectionCount;
            }

            Array.Sort(rooms);
            Array.Sort(branches);
            Array.Sort(connections);
            int mid = metrics.Count / 2;
            medians.RoomCount = metrics.Count % 2 == 0
                ? (rooms[mid - 1] + rooms[mid]) * 0.5f
                : rooms[mid];
            medians.BranchScore = metrics.Count % 2 == 0
                ? (branches[mid - 1] + branches[mid]) * 0.5f
                : branches[mid];
            medians.ConnectionCount = metrics.Count % 2 == 0
                ? (connections[mid - 1] + connections[mid]) * 0.5f
                : connections[mid];
        }

        private static float BalancedDistance(GenerationMetrics metrics)
        {
            BalancedMedians medians = BalancedMedians.Current;
            float roomDelta = Math.Abs(metrics.TotalRoomCount - medians.RoomCount);
            float branchDelta = Math.Abs(BranchScore(metrics) - medians.BranchScore);
            float connectionDelta = Math.Abs(metrics.ConnectionCount - medians.ConnectionCount);
            return roomDelta + branchDelta + connectionDelta;
        }

        private static float LinearRatio(GenerationMetrics metrics) =>
            metrics.MainPathRoomCount / (float)Math.Max(1, metrics.BranchPathRoomCount);

        private static int BranchScore(GenerationMetrics metrics) =>
            metrics.BranchPathRoomCount + metrics.MaxBranchDepth;

        private static float Density(GenerationMetrics metrics) =>
            metrics.BoundsVolume / Math.Max(1, metrics.TotalRoomCount);

        private static float CrampedScore(GenerationMetrics metrics) =>
            Density(metrics);

        private static float OpenDensity(GenerationMetrics metrics) =>
            metrics.ConnectionCount / Math.Max(1f, metrics.BoundsVolume);

        private static float LoopyScore(GenerationMetrics metrics) =>
            metrics.ConnectionCount / (float)Math.Max(1, metrics.TotalRoomCount);

        private static float DeadEndScore(GenerationMetrics metrics) =>
            metrics.UnusedDoorwayCount / (float)Math.Max(1, metrics.TotalRoomCount)
            - metrics.ConnectionCount * 0.01f;

        private static bool Less(int left, int right) => left < right;

        private static bool Greater(int left, int right) => left > right;

        private static bool Less(float left, float right) => left < right;

        private static bool Greater(float left, float right) => left > right;
    }

    public struct BalancedMedians
    {
        public static BalancedMedians Current;

        public float RoomCount;

        public float BranchScore;

        public float ConnectionCount;
    }
}
