using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonIdListParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_returns_empty_set_for_blank_input(string? csv)
        {
            HashSet<int> ids = DungeonIdListParser.Parse(csv);

            Assert.Empty(ids);
        }

        [Fact]
        public void Parse_trims_and_splits_comma_separated_ids()
        {
            HashSet<int> ids = DungeonIdListParser.Parse(" 10 , 20,30 ,40 ");

            Assert.Equal(4, ids.Count);
            Assert.Contains(10, ids);
            Assert.Contains(20, ids);
            Assert.Contains(30, ids);
            Assert.Contains(40, ids);
        }

        [Fact]
        public void Parse_deduplicates_ids()
        {
            HashSet<int> ids = DungeonIdListParser.Parse("5,7,5,7");

            Assert.Equal(2, ids.Count);
            Assert.Contains(5, ids);
            Assert.Contains(7, ids);
        }

        [Theory]
        [InlineData("WidenVanilla", 0)]
        [InlineData("widenVanilla", 0)]
        [InlineData("AllActiveUniform", 1)]
        [InlineData("allactiveuniform", 1)]
        [InlineData(null, 0)]
        [InlineData("Unknown", 0)]
        public void ParsePoolMode_maps_known_values_and_defaults_to_widen_vanilla(
            string? value,
            int expectedModeOrdinal)
        {
            DungeonPickPoolMode mode = DungeonIdListParser.ParsePoolMode(value);

            Assert.Equal((DungeonPickPoolMode)expectedModeOrdinal, mode);
        }
    }
}
