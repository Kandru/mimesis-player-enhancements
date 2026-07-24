using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicTrust;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicTrustResolverTests
    {
        [Theory]
        [InlineData(MimicTuningValueMode.Fixed, 50f, 65f, 65f, 65f, 65f)]
        [InlineData(MimicTuningValueMode.Vanilla, 50f, 65f, 10f, 20f, 50f)]
        public void ResolveInitialTrust_uses_mode(
            MimicTuningValueMode mode,
            float vanilla,
            float fixedValue,
            float randomMin,
            float randomMax,
            float expected)
        {
            float result = MimicTuningModeHelpers.ResolveTrustScore(
                mode,
                vanilla,
                fixedValue,
                randomMin,
                randomMax);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Vanilla_constants_match_game_defaults()
        {
            Assert.Equal(50f, MimicTrustResolver.VanillaInitialTrust);
            Assert.Equal(70f, MimicTrustResolver.VanillaBehaviorTrust);
            Assert.Equal(8f, MimicTrustResolver.VanillaChaseActivationDistance);
            Assert.Equal(10f, MimicTrustResolver.VanillaChaseForceRunDistance);
        }
    }
}
