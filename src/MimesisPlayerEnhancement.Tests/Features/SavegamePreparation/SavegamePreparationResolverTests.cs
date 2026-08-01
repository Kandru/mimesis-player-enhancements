using MimesisPlayerEnhancement.Features.SavegamePreparation;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationResolverTests
    {
        private const int ConfigStartingZoneMax = 99;

        [Theory]
        [InlineData(1, 10, false, 1)]
        [InlineData(0, 10, false, 1)]
        [InlineData(5, 10, false, 5)]
        [InlineData(12, 10, false, 10)]
        [InlineData(16, 7, false, 7)]
        [InlineData(16, 7, true, 16)]
        [InlineData(100, 7, true, 99)]
        [InlineData(0, 7, true, 1)]
        [InlineData(50, 7, true, 50)]
        public void ClampStartingZone_respects_override_cap(
            int requested,
            int maxStage,
            bool overrideMaxStageCount,
            int expected)
        {
            int cap = overrideMaxStageCount ? ConfigStartingZoneMax : maxStage;
            Assert.Equal(expected, SavegamePreparationResolver.ClampStartingZone(requested, cap));
        }
    }
}
