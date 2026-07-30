using MimesisPlayerEnhancement.Features.SpawnScaling;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnSlotFactoryTests
    {
        [Theory]
        [InlineData(10, 1f, 1f, 0, 0)]
        [InlineData(10, 3f, 1f, 0, 20)]
        [InlineData(10, 1f, 3f, 0, 20)]
        [InlineData(10, 3f, 3f, 0, 20)]
        [InlineData(10, 3f, 3f, 110, 10)]
        [InlineData(0, 3f, 3f, 0, 0)]
        public void ComputeAmbientExpandCount_respects_pool_multiplier_and_cap(
            int poolSize,
            float jakoMultiplier,
            float mimicMultiplier,
            int alreadySynthetic,
            int expected)
        {
            int actual = SpawnSlotFactory.ComputeAmbientExpandCount(
                poolSize,
                jakoMultiplier,
                mimicMultiplier,
                alreadySynthetic);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MaySynthesize_excludes_traps()
        {
            Assert.True(SpawnSlotFactory.MaySynthesize(SpawnCategory.Boss));
            Assert.True(SpawnSlotFactory.MaySynthesize(SpawnCategory.Special));
            Assert.False(SpawnSlotFactory.MaySynthesize(SpawnCategory.Trap));
        }

        [Fact]
        public void IsTooClose_rejects_points_within_min_separation()
        {
            bool tooClose = SpawnSlotFactory.IsTooClose(
                new UnityEngine.Vector3(1f, 0f, 0f),
                [new UnityEngine.Vector3(0f, 0f, 0f)]);

            Assert.True(tooClose);
        }

        [Fact]
        public void IsTooClose_allows_points_beyond_min_separation()
        {
            bool tooClose = SpawnSlotFactory.IsTooClose(
                new UnityEngine.Vector3(3f, 0f, 0f),
                [new UnityEngine.Vector3(0f, 0f, 0f)]);

            Assert.False(tooClose);
        }
    }
}
