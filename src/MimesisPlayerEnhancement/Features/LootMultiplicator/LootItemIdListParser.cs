using System;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootItemIdListParser
    {
        private const string Feature = "LootMultiplicator";

        internal static HashSet<int> Parse(string? csv)
        {
            return CsvIdSetParser.Parse(csv, Feature, "loot item ID");
        }

        internal static LootItemFilterMode ParseMode(string? value)
        {
            if (string.Equals(value, "AllowlistOnly", StringComparison.OrdinalIgnoreCase))
            {
                return LootItemFilterMode.AllowlistOnly;
            }

            if (string.Equals(value, "BlocklistOnly", StringComparison.OrdinalIgnoreCase))
            {
                return LootItemFilterMode.BlocklistOnly;
            }

            return LootItemFilterMode.All;
        }
    }
}
