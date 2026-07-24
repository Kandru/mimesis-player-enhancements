using MimesisPlayerEnhancement.Features.SpawnScaling;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnTimingScaleResolverTests
    {
        [Theory]
        [InlineData(0, 2f, 0)]
        [InlineData(3, 1f, 3)]
        [InlineData(3, 2f, 6)]
        [InlineData(1, 1.4f, 1)]
        [InlineData(2, 1.6f, 3)]
        public void ScaleTryCount_matches_ScalingMath_ScaleCount(int vanilla, float multiplier, int expected)
        {
            Assert.Equal(expected, SpawnTimingScaleResolver.ScaleTryCount(vanilla, multiplier));
        }

        [Theory]
        [InlineData(0, 2f, 0)]
        [InlineData(-1, 2f, -1)]
        [InlineData(100, 1f, 100)]
        [InlineData(100, 0.5f, 100)]
        [InlineData(100, 1.5f, 150)]
        [InlineData(8000, 2f, 10000)]
        public void ScaleRate_clamps_and_skips_non_increase(int vanilla, float multiplier, int expected)
        {
            Assert.Equal(expected, SpawnTimingScaleResolver.ScaleRate(vanilla, multiplier));
        }
    }
}
