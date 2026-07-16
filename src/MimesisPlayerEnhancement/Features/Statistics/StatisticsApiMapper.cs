using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsApiMapper
    {
        internal static List<EntityCountEntry> MapEntityCounts(Dictionary<string, long>? counts)
        {
            if (counts == null || counts.Count == 0)
            {
                return [];
            }

            List<EntityCountEntry> entries = new(counts.Count);
            foreach (KeyValuePair<string, long> pair in counts.OrderByDescending(static kvp => kvp.Value))
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                entries.Add(new EntityCountEntry
                {
                    Key = pair.Key,
                    DisplayName = StatisticsEntityKeys.ResolveDisplayName(pair.Key),
                    LocalizationKey = StatisticsEntityKeys.ResolveLocalizationKey(pair.Key),
                    Count = pair.Value,
                });
            }

            return entries;
        }
    }
}
