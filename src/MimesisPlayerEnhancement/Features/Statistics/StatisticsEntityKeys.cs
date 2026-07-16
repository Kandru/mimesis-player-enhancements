namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsEntityKeys
    {
        internal const string PlayerKey = "player";

        internal static string ForMonster(int masterId) => $"monster:{masterId}";

        internal static string ForTrap(TrapType trapType) => $"trap:{(int)trapType}";

        internal static string ResolveLocalizationKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (key == PlayerKey)
            {
                return "entities.player";
            }

            if (key.StartsWith("monster:", StringComparison.Ordinal))
            {
                return $"entities.monster_{key["monster:".Length..]}";
            }

            if (key.StartsWith("trap:", StringComparison.Ordinal))
            {
                return ResolveTrapLocalizationKey(key["trap:".Length..]);
            }

            return "";
        }

        internal static string ResolveDisplayName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (key == PlayerKey)
            {
                return ModL10n.Get("entities.player");
            }

            if (key.StartsWith("monster:", StringComparison.Ordinal)
                && int.TryParse(key["monster:".Length..], out int masterId))
            {
                return MonsterTypeLookup.GetDisplayName(masterId);
            }

            if (key.StartsWith("trap:", StringComparison.Ordinal)
                && int.TryParse(key["trap:".Length..], out int trapId)
                && Enum.IsDefined(typeof(TrapType), trapId))
            {
                return ResolveTrapDisplayName((TrapType)trapId);
            }

            return EntityDisplayNameFormatter.Humanize(key);
        }

        private static string ResolveTrapLocalizationKey(string trapId) =>
            trapId switch
            {
                "4" => "entities.invisible_mine",
                "2" => "entities.sprinkler",
                "3" => "entities.weight_trap",
                "5" => "entities.repeating_weight_trap",
                "1" => "entities.corridor_trap",
                "0" => "entities.trap",
                _ => $"entities.trap_{trapId}",
            };

        private static string ResolveTrapDisplayName(TrapType trapType) =>
            ModL10n.Get(ResolveTrapLocalizationKey(((int)trapType).ToString()));
    }
}
