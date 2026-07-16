using System.Linq;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    public enum DungeonSeedFlavor
    {
        Vanilla,
        Compact,
        Expansive,
        ShortMainPath,
        LongMainPath,
        Sprawling,
        Dense,
        Cramped,
        Linear,
        MinimalBranches,
        Branching,
        BroadBranches,
        Deep,
        Open,
        Maze,
        Loopy,
        DeadEnds,
        TightCorridor,
        Labyrinth,
        Honeycomb,
        WideOpen,
        StableCompact,
        DeepMaze,
        Reliable,
        Balanced,
    }

    public static class DungeonSeedFlavorUtil
    {
        public static readonly string[] AllNames = Enum.GetNames(typeof(DungeonSeedFlavor));

        public static readonly DungeonSeedFlavor[] Curated =
            Enum.GetValues(typeof(DungeonSeedFlavor)).Cast<DungeonSeedFlavor>()
                .Where(flavor => flavor != DungeonSeedFlavor.Vanilla)
                .ToArray();

        public static bool TryParse(string? value, out DungeonSeedFlavor flavor) =>
            Enum.TryParse(value, ignoreCase: true, out flavor)
            && Enum.IsDefined(typeof(DungeonSeedFlavor), flavor);

        public static string ToConfigValue(DungeonSeedFlavor flavor) => flavor.ToString();
    }
}
