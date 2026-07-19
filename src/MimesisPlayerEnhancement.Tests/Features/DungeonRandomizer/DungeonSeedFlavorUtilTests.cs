using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonSeedFlavorUtilTests
    {
        [Theory]
        [InlineData("Vanilla", DungeonSeedFlavor.Vanilla)]
        [InlineData("compact", DungeonSeedFlavor.Compact)]
        [InlineData("DeepMaze", DungeonSeedFlavor.DeepMaze)]
        public void TryParse_accepts_defined_flavor_names(string value, DungeonSeedFlavor expected)
        {
            bool parsed = DungeonSeedFlavorUtil.TryParse(value, out DungeonSeedFlavor flavor);

            Assert.True(parsed);
            Assert.Equal(expected, flavor);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NotAFlavor")]
        public void TryParse_rejects_unknown_values(string? value)
        {
            bool parsed = DungeonSeedFlavorUtil.TryParse(value, out DungeonSeedFlavor flavor);

            Assert.False(parsed);
            Assert.Equal(default, flavor);
        }

        [Fact]
        public void Curated_excludes_Vanilla()
        {
            Assert.DoesNotContain(DungeonSeedFlavor.Vanilla, DungeonSeedFlavorUtil.Curated);
            Assert.NotEmpty(DungeonSeedFlavorUtil.Curated);
        }

        [Fact]
        public void ToConfigValue_round_trips_enum_names()
        {
            foreach (DungeonSeedFlavor flavor in DungeonSeedFlavorUtil.Curated)
            {
                string configValue = DungeonSeedFlavorUtil.ToConfigValue(flavor);

                Assert.True(DungeonSeedFlavorUtil.TryParse(configValue, out DungeonSeedFlavor parsed));
                Assert.Equal(flavor, parsed);
            }
        }
    }
}
