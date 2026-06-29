using System;
using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootItemIdListParser
    {
        internal static HashSet<int> Parse(string? csv)
        {
            HashSet<int> ids = [];
            if (string.IsNullOrWhiteSpace(csv))
            {
                return ids;
            }

            foreach (string token in csv.Split(','))
            {
                string trimmed = token.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (int.TryParse(trimmed, out int id) && id > 0)
                {
                    _ = ids.Add(id);
                }
                else if (ModConfig.EnableDebugLogging.Value)
                {
                    ModLog.Debug("LootMultiplicator", $"Ignoring invalid loot item ID token: '{trimmed}'");
                }
            }

            return ids;
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
