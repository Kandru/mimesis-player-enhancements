using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnScalingConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_SpawnScaling";

        [Fact]
        public void SpawnScalingBaselinePlayerCount_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "SpawnScalingBaselinePlayerCount", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void MimicSpawnPerPlayerMultiplier_has_minimum_zero()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "MimicSpawnPerPlayerMultiplier", out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("MimicSpawnMultiplier")]
        [InlineData("BossSpawnMultiplier")]
        [InlineData("GruntSpawnMultiplier")]
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
        [InlineData("BonusEncounterDelayMinSeconds")]
        [InlineData("BonusEncounterDelayMaxSeconds")]
        [InlineData("BonusEncounterMinPlayerDistanceMeters")]
        [InlineData("TrapRespawnDelaySeconds")]
        [InlineData("TrapRespawnDelayMinSeconds")]
        [InlineData("TrapRespawnDelayMaxSeconds")]
        [InlineData("TrapRespawnMinPlayerDistanceMeters")]
        public void Map_placed_encounter_entries_have_minimum_zero(string entryId)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, entryId, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Theory]
        [InlineData("MimicWaveInitialDelaySeconds")]
        [InlineData("MimicWaveInitialDelayMinSeconds")]
        [InlineData("MimicWaveInitialDelayMaxSeconds")]
        [InlineData("MimicWaveIntervalSeconds")]
        [InlineData("MimicWaveIntervalMinSeconds")]
        [InlineData("MimicWaveIntervalMaxSeconds")]
        [InlineData("GruntWaveInitialDelaySeconds")]
        [InlineData("GruntWaveInitialDelayMinSeconds")]
        [InlineData("GruntWaveInitialDelayMaxSeconds")]
        [InlineData("GruntWaveIntervalSeconds")]
        [InlineData("GruntWaveIntervalMinSeconds")]
        [InlineData("GruntWaveIntervalMaxSeconds")]
        public void Ambient_wave_entries_have_minimum_zero(string entryId)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, entryId, out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }
    }
}
