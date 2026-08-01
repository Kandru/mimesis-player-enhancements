using MimesisPlayerEnhancement.Features.SavegamePreparation;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationResolverTests
    {
        [Theory]
        [InlineData(1, 10, 1)]
        [InlineData(0, 10, 1)]
        [InlineData(5, 10, 5)]
        [InlineData(12, 10, 10)]
        public void ClampStartingZone_clamps_to_valid_range(int requested, int maxStage, int expected)
        {
            Assert.Equal(expected, SavegamePreparationResolver.ClampStartingZone(requested, maxStage));
        }
    }
}
