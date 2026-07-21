using MimesisPlayerEnhancement.Features.MimicTuning.MimicInventoryCopy;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MimicTuning
{
    public sealed class MimicInventoryCopyResolverTests
    {
        [Theory]
        [InlineData("Vanilla", "Vanilla")]
        [InlineData("vanilla", "Vanilla")]
        [InlineData("Custom", "Custom")]
        [InlineData("custom", "Custom")]
        [InlineData(null, "Vanilla")]
        [InlineData("invalid", "Vanilla")]
        public void ParseMode_maps_values(string? value, string expectedName)
        {
            Assert.Equal(expectedName, MimicInventoryCopyResolver.ParseMode(value).ToString());
        }

        [Theory]
        [InlineData("MinDistance", BTTargetPickRule.MinDistance)]
        [InlineData("mindistance", BTTargetPickRule.MinDistance)]
        [InlineData("MaxDistance", BTTargetPickRule.MaxDistance)]
        [InlineData("maxdistance", BTTargetPickRule.MaxDistance)]
        [InlineData("Random", BTTargetPickRule.Random)]
        [InlineData("random", BTTargetPickRule.Random)]
        [InlineData(null, BTTargetPickRule.MinDistance)]
        [InlineData("invalid", BTTargetPickRule.MinDistance)]
        public void ParsePickRule_maps_values(string? value, BTTargetPickRule expected)
        {
            Assert.Equal(expected, MimicInventoryCopyResolver.ParsePickRule(value));
        }
    }
}
