using System.Globalization;
using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnScalingConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_SpawnScaling";

        [Fact]
        public void SpawnScalingPlayerCountScaleRate_has_minimum_zero()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "SpawnScalingPlayerCountScaleRate", out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("MimicSpawnMultiplier")]
        [InlineData("BossSpawnMultiplier")]
        [InlineData("JakoSpawnMultiplier")]
        [InlineData("SpecialSpawnMultiplier")]
        [InlineData("TrapSpawnMultiplier")]
        [InlineData("OtherSpawnMultiplier")]
        public void Category_spawn_multipliers_have_minimum_zero(string entryId)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, entryId, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("MapPlacedEncounterDelayMinSeconds")]
        [InlineData("MapPlacedEncounterDelayMaxSeconds")]
        [InlineData("MapPlacedEncounterMinPlayerDistanceMeters")]
        public void Map_placed_encounter_entries_have_minimum_zero(string entryId)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, entryId, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }
    }
}
