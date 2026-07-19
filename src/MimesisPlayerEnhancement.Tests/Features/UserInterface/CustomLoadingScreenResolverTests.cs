using MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class CustomLoadingScreenResolverTests
    {
        public CustomLoadingScreenResolverTests()
        {
            CustomLoadingScreenResolver.InvalidateCatalog();
        }

        [Fact]
        public void ListVariantOptionValues_includes_embedded_themes()
        {
            IReadOnlyList<string> options = CustomLoadingScreenResolver.ListVariantOptionValues();

            Assert.Contains("GTA", options);
            Assert.Contains("StarTrek", options);
            Assert.Contains("StarWars", options);
        }

        [Fact]
        public void NormalizeVariantOptionValue_returns_first_option_for_empty_input()
        {
            IReadOnlyList<string> options = CustomLoadingScreenResolver.ListVariantOptionValues();
            Assert.NotEmpty(options);

            string normalized = CustomLoadingScreenResolver.NormalizeVariantOptionValue("");

            Assert.Equal(options[0], normalized);
        }

        [Fact]
        public void NormalizeVariantOptionValue_matches_case_insensitively()
        {
            string normalized = CustomLoadingScreenResolver.NormalizeVariantOptionValue("startrek");

            Assert.Equal("StarTrek", normalized);
        }

        [Fact]
        public void NormalizeVariantOptionValue_falls_back_to_first_option_for_unknown_value()
        {
            IReadOnlyList<string> options = CustomLoadingScreenResolver.ListVariantOptionValues();

            string normalized = CustomLoadingScreenResolver.NormalizeVariantOptionValue("not_a_real_theme");

            Assert.Equal(options[0], normalized);
        }

        [Fact]
        public void NormalizeRandomPoolValue_trims_and_deduplicates_known_themes()
        {
            string normalized = CustomLoadingScreenResolver.NormalizeRandomPoolValue(" GTA , StarWars , gta ");

            Assert.Equal("GTA,StarWars", normalized);
        }

        [Fact]
        public void NormalizeRandomPoolValue_preserves_known_themes_in_order()
        {
            string normalized = CustomLoadingScreenResolver.NormalizeRandomPoolValue(" StarWars , GTA ");

            Assert.Equal("StarWars,GTA", normalized);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeRandomPoolValue_returns_empty_for_blank_input(string? csv)
        {
            string normalized = CustomLoadingScreenResolver.NormalizeRandomPoolValue(csv);

            Assert.Equal(string.Empty, normalized);
        }
    }
}
