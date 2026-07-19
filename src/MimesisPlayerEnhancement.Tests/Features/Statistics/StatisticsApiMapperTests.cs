using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsApiMapperTests
    {
        [Fact]
        public void MapEntityCounts_returns_empty_for_null()
        {
            List<EntityCountEntry> result = StatisticsApiMapper.MapEntityCounts(null);

            Assert.Empty(result);
        }

        [Fact]
        public void MapEntityCounts_returns_empty_for_empty_dictionary()
        {
            List<EntityCountEntry> result = StatisticsApiMapper.MapEntityCounts([]);

            Assert.Empty(result);
        }

        [Fact]
        public void MapEntityCounts_filters_non_positive_counts()
        {
            Dictionary<string, long> counts = new()
            {
                ["player"] = 3,
                ["monster:1"] = 0,
                ["trap:1"] = -1,
            };

            List<EntityCountEntry> result = StatisticsApiMapper.MapEntityCounts(counts);

            Assert.Single(result);
            Assert.Equal("player", result[0].Key);
            Assert.Equal(3, result[0].Count);
        }

        [Fact]
        public void MapEntityCounts_orders_descending_by_count()
        {
            Dictionary<string, long> counts = new()
            {
                ["player"] = 1,
                ["monster:2"] = 5,
                ["monster:1"] = 3,
            };

            List<EntityCountEntry> result = StatisticsApiMapper.MapEntityCounts(counts);

            Assert.Equal(3, result.Count);
            Assert.Equal("monster:2", result[0].Key);
            Assert.Equal("monster:1", result[1].Key);
            Assert.Equal("player", result[2].Key);
        }

        [Fact]
        public void MapEntityCounts_populates_localization_keys()
        {
            Dictionary<string, long> counts = new()
            {
                ["monster:7"] = 2,
            };

            List<EntityCountEntry> result = StatisticsApiMapper.MapEntityCounts(counts);

            Assert.Single(result);
            Assert.Equal("entities.monster_7", result[0].LocalizationKey);
        }
    }
}
