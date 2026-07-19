using MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class RoundStartSoundResolverTests
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
            string displayName = RoundStartSoundResolver.FormatVariantDisplayName(input);

            Assert.Equal(expected, displayName);
        }

        [Fact]
        public void NormalizeVariantOptionValue_returns_first_option_for_empty_input()
        {
            IReadOnlyList<string> options = RoundStartSoundResolver.ListVariantOptionValues();
            Assert.NotEmpty(options);

            string normalized = RoundStartSoundResolver.NormalizeVariantOptionValue("");

            Assert.Equal(options[0], normalized);
        }

        [Fact]
        public void NormalizeVariantOptionValue_matches_case_insensitively()
        {
            IReadOnlyList<string> options = RoundStartSoundResolver.ListVariantOptionValues();
            string target = options.First(option => string.Equals(option, "anime_1", StringComparison.OrdinalIgnoreCase));

            string normalized = RoundStartSoundResolver.NormalizeVariantOptionValue("ANIME_1");

            Assert.Equal(target, normalized);
        }

        [Fact]
        public void NormalizeVariantOptionValue_falls_back_to_first_option_for_unknown_value()
        {
            IReadOnlyList<string> options = RoundStartSoundResolver.ListVariantOptionValues();

            string normalized = RoundStartSoundResolver.NormalizeVariantOptionValue("not_a_real_variant");

            Assert.Equal(options[0], normalized);
        }

        [Fact]
        public void NormalizeRandomPoolValue_trims_and_deduplicates_known_variants()
        {
            string normalized = RoundStartSoundResolver.NormalizeRandomPoolValue(" anime_1 , rocky_2 , anime_1 ");

            Assert.Equal("anime_1,rocky_2", normalized);
        }

        [Fact]
        public void NormalizeRandomPoolValue_preserves_known_variants_in_order()
        {
            string normalized = RoundStartSoundResolver.NormalizeRandomPoolValue(" rocky_2 , anime_1 ");

            Assert.Equal("rocky_2,anime_1", normalized);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeRandomPoolValue_returns_empty_for_blank_input(string? csv)
        {
            string normalized = RoundStartSoundResolver.NormalizeRandomPoolValue(csv);

            Assert.Equal(string.Empty, normalized);
        }
    }
}
