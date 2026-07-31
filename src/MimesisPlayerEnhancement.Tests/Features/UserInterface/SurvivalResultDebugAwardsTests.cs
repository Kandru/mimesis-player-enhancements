using MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList;
using ReluProtocol.Enum;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class SurvivalResultDebugAwardsTests
    {
        [Theory]
        [InlineData(0, AwardType.None)]
        [InlineData(1, AwardType.BestCarryItem)]
        [InlineData(2, AwardType.BestDamageToAlly)]
        [InlineData(3, AwardType.BestMimicEncounter)]
        [InlineData(4, AwardType.BestCamper)]
        [InlineData(5, AwardType.None)]
        [InlineData(7, AwardType.BestDamageToAlly)]
        public void Resolve_maps_roll_onto_award_pool(int roll, AwardType expected)
        {
            Assert.Equal(expected, SurvivalResultDebugAwards.Resolve(roll));
        }

        [Fact]
        public void PoolSize_matches_AwardType_enum_length()
        {
            Assert.Equal(Enum.GetValues(typeof(AwardType)).Length, SurvivalResultDebugAwards.PoolSize);
        }
    }
}
