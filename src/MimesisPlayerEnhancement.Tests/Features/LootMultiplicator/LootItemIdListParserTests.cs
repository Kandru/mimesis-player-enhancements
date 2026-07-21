using MimesisPlayerEnhancement.Features.LootMultiplicator;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootItemIdListParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_returns_empty_set_for_blank_input(string? csv)
        {
            HashSet<int> ids = LootItemIdListParser.Parse(csv);

            Assert.Empty(ids);
        }

        [Fact]
        public void Parse_trims_and_splits_comma_separated_ids()
        {
            HashSet<int> ids = LootItemIdListParser.Parse(" 10 , 20,30 ,40 ");

            Assert.Equal(4, ids.Count);
            Assert.Contains(10, ids);
            Assert.Contains(20, ids);
            Assert.Contains(30, ids);
            Assert.Contains(40, ids);
        }

        [Fact]
        public void Parse_deduplicates_ids()
        {
            HashSet<int> ids = LootItemIdListParser.Parse("5,7,7");

            Assert.Equal(2, ids.Count);
            Assert.Contains(5, ids);
            Assert.Contains(7, ids);
        }

        [Theory]
        [InlineData("All", 0)]
        [InlineData("all", 0)]
        [InlineData("AllowlistOnly", 1)]
        [InlineData("allowlistonly", 1)]
        [InlineData("BlocklistOnly", 2)]
        [InlineData("blocklistonly", 2)]
        [InlineData(null, 0)]
        [InlineData("Unknown", 0)]
        public void ParseMode_maps_known_values_and_defaults_to_all(string? value, int expectedModeValue)
        {
            LootItemFilterMode mode = LootItemIdListParser.ParseMode(value);

            Assert.Equal((LootItemFilterMode)expectedModeValue, mode);
        }
    }
}
