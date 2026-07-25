using MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class DiscoBallSoundResolverTests
    {
        [Theory]
        [InlineData("dark_melody", "Dark Melody")]
        [InlineData("teen_pop_1", "Teen Pop 1")]
        [InlineData("ROCKY_2", "Rocky 2")]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public void FormatVariantDisplayName_converts_underscores_to_title_case(
            string input,
            string expected)
        {
            string displayName = DiscoBallSoundResolver.FormatVariantDisplayName(input);

            Assert.Equal(expected, displayName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeRandomPoolValue_returns_empty_for_blank_input(string? csv)
        {
            string normalized = DiscoBallSoundResolver.NormalizeRandomPoolValue(csv);

            Assert.Equal(string.Empty, normalized);
        }

        [Fact]
        public void NormalizeVariantOptionValue_returns_trimmed_input_when_catalog_is_empty()
        {
            var catalog = new EmbeddedAudioVariantCatalog("DiscoBallSoundTestEmpty", "Ui", "sound variant");

            string normalized = catalog.NormalizeVariantOptionValue("  custom_track  ");

            Assert.Equal("custom_track", normalized);
        }
    }
}
