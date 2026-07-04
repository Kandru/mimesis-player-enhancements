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
                TrapType.Mine_Invisible => "Invisible Mine",
                TrapType.Sprinkler => "Sprinkler",
                TrapType.Weight_Controller => "Weight Trap",
                TrapType.Weight_Repeater => "Repeating Weight Trap",
                TrapType.Corrider => "Corridor Trap",
                TrapType.Default => "Trap",
                _ => EntityDisplayNameFormatter.Humanize(trapType.ToString()),
            };
        }
    }
}
