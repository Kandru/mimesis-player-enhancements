using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsJson
    {
        internal static string SerializeSlot(SlotStatisticsDocument slot)
        {
            return ModJson.Serialize(slot);
        }

        internal static SlotStatisticsDocument? DeserializeSlot(string json)
        {
            return ModJson.Deserialize<SlotStatisticsDocument>(json);
        }
    }
}
