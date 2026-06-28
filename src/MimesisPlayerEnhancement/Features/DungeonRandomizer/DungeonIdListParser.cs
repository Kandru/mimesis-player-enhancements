using System;
using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonIdListParser
{
    internal static HashSet<int> Parse(string? csv)
    {
        var ids = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(csv))
            return ids;

        foreach (string token in csv.Split(','))
        {
            string trimmed = token.Trim();
            if (trimmed.Length == 0)
                continue;

            if (int.TryParse(trimmed, out int id) && id > 0)
                ids.Add(id);
            else
                DungeonRandomizerLog.Debug($"Ignoring invalid dungeon ID token: '{trimmed}'");
        }

        return ids;
    }

    internal static DungeonPickPoolMode ParsePoolMode(string? value)
    {
        if (string.Equals(value, "AllActiveUniform", StringComparison.OrdinalIgnoreCase))
            return DungeonPickPoolMode.AllActiveUniform;

        return DungeonPickPoolMode.WidenVanilla;
    }
}
