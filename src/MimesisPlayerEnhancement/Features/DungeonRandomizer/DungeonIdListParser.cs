using System;
using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonIdListParser
    {
        private const string Feature = "DungeonRandomizer";

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
                else
                {
                    ModLog.Debug(Feature, $"Ignoring invalid dungeon ID token: '{trimmed}'");
                }
            }

            return ids;
        }

        internal static DungeonPickPoolMode ParsePoolMode(string? value)
        {
            return string.Equals(value, "AllActiveUniform", StringComparison.OrdinalIgnoreCase)
                ? DungeonPickPoolMode.AllActiveUniform
                : DungeonPickPoolMode.WidenVanilla;
        }
    }
}
