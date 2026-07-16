namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonSeedFlavorNames
    {
        internal static readonly string[] All =
        [
            "Vanilla",
            "Compact",
            "Expansive",
            "ShortMainPath",
            "LongMainPath",
            "Sprawling",
            "Dense",
            "Cramped",
            "Linear",
            "MinimalBranches",
            "Branching",
            "BroadBranches",
            "Deep",
            "Open",
            "Maze",
            "Loopy",
            "DeadEnds",
            "TightCorridor",
            "Labyrinth",
            "Honeycomb",
            "WideOpen",
            "StableCompact",
            "DeepMaze",
            "Reliable",
            "Balanced",
        ];

        internal static bool TryParse(string? value, out DungeonSeedFlavor flavor)
        {
            flavor = DungeonSeedFlavor.Vanilla;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    flavor = (DungeonSeedFlavor)i;
                    return true;
                }
            }

            return false;
        }

        internal static string ToConfigValue(DungeonSeedFlavor flavor)
        {
            int index = (int)flavor;
            if (index < 0 || index >= All.Length)
            {
                return "Vanilla";
            }

            return All[index];
        }
    }
}
