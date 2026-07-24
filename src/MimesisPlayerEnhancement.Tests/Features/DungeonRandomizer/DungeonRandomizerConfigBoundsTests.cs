using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonRandomizerConfigBoundsTests
    {
        [Theory]
        [InlineData("WidenVanilla")]
        [InlineData("widenVanilla")]
        [InlineData("AllActiveUniform")]
        [InlineData("allactiveuniform")]
        public void IsValidPoolMode_accepts_known_modes(string value)
        {
            Assert.True(DungeonRandomizerConfig.IsValidPoolMode(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Uniform")]
        [InlineData("Vanilla")]
        public void IsValidPoolMode_rejects_unknown_modes(string? value)
        {
            Assert.False(DungeonRandomizerConfig.IsValidPoolMode(value));
        }

        [Fact]
        public void DefaultPoolMode_is_WidenVanilla()
        {
            Assert.Equal("WidenVanilla", DungeonRandomizerConfig.DefaultPoolMode);
        }

        [Theory]
        [InlineData("Vanilla", "Vanilla")]
        [InlineData("compact", "compact")]
        [InlineData("DeepMaze", "DeepMaze")]
        public void NormalizeSeedFlavor_keeps_valid_values(string value, string expected)
        {
            Assert.Equal(expected, DungeonRandomizerConfig.NormalizeSeedFlavor(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NotAFlavor")]
        public void NormalizeSeedFlavor_resets_invalid_values_to_Vanilla(string? value)
        {
            Assert.Equal("Vanilla", DungeonRandomizerConfig.NormalizeSeedFlavor(value));
        }
    }
}
