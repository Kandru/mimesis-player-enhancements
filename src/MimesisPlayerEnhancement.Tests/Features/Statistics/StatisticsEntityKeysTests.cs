using MimesisPlayerEnhancement.Features.Statistics;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsEntityKeysTests
    {
        [Fact]
        public void ForMonster_formats_master_id()
        {
            Assert.Equal("monster:42", StatisticsEntityKeys.ForMonster(42));
        }

        [Theory]
        [InlineData(TrapType.Default, "trap:0")]
        [InlineData(TrapType.Corrider, "trap:1")]
        [InlineData(TrapType.Sprinkler, "trap:2")]
        [InlineData(TrapType.Weight_Controller, "trap:3")]
        [InlineData(TrapType.Weight_Repeater, "trap:4")]
        [InlineData(TrapType.Mine_Invisible, "trap:5")]
        public void ForTrap_formats_trap_type_id(TrapType trapType, string expected)
        {
            Assert.Equal(expected, StatisticsEntityKeys.ForTrap(trapType));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData("player", "entities.player")]
        [InlineData("monster:7", "entities.monster_7")]
        [InlineData("trap:0", "entities.trap")]
        [InlineData("trap:1", "entities.corridor_trap")]
        [InlineData("trap:2", "entities.sprinkler")]
        [InlineData("trap:3", "entities.weight_trap")]
        [InlineData("trap:4", "entities.invisible_mine")]
        [InlineData("trap:5", "entities.repeating_weight_trap")]
        [InlineData("trap:99", "entities.trap_99")]
        [InlineData("unknown", "")]
        public void ResolveLocalizationKey_maps_known_keys(string? key, string expected)
        {
            Assert.Equal(expected, StatisticsEntityKeys.ResolveLocalizationKey(key!));
        }
    }
}
