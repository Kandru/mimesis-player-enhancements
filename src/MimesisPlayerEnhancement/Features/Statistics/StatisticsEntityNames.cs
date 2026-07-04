using MimesisPlayerEnhancement.Features.SpawnScaling;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsEntityNames
    {
        internal static string FormatMonsterName(int masterId)
        {
            string displayName = MonsterTypeLookup.GetDisplayName(masterId);
            string humanized = EntityDisplayNameFormatter.Humanize(displayName);
            return string.IsNullOrWhiteSpace(humanized) ? masterId.ToString() : humanized;
        }

        internal static string FormatTrapName(TrapType trapType)
        {
            return trapType switch
            {
                TrapType.Mine_Invisible => ModL10n.Get("entities.invisible_mine"),
                TrapType.Sprinkler => ModL10n.Get("entities.sprinkler"),
                TrapType.Weight_Controller => ModL10n.Get("entities.weight_trap"),
                TrapType.Weight_Repeater => ModL10n.Get("entities.repeating_weight_trap"),
                TrapType.Corrider => ModL10n.Get("entities.corridor_trap"),
                TrapType.Default => ModL10n.Get("entities.trap"),
                _ => EntityDisplayNameFormatter.Humanize(trapType.ToString()),
            };
        }
    }
}
