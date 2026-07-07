namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonIdListParser
    {
        private const string Feature = "DungeonRandomizer";

        internal static HashSet<int> Parse(string? csv)
        {
            return CsvIdSetParser.Parse(csv, Feature, "dungeon ID");
        }

        internal static DungeonPickPoolMode ParsePoolMode(string? value)
        {
            return string.Equals(value, "AllActiveUniform", StringComparison.OrdinalIgnoreCase)
                ? DungeonPickPoolMode.AllActiveUniform
                : DungeonPickPoolMode.WidenVanilla;
        }
    }
}
