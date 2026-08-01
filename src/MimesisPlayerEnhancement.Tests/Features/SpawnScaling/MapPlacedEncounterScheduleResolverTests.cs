using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class MapPlacedEncounterScheduleResolverTests
    {
        private static SpawnScalingSceneConfig Config(string trapRespawnMode = "Vanilla") =>
            new(
                enableSpawnScaling: true,
                spawnScalingBaselinePlayerCount: ScalingMath.VanillaPlayerBaseline,
                mimicSpawnMultiplier: 1f,
                mimicSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                bossSpawnMultiplier: 1f,
                bossSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                jakoSpawnMultiplier: 1f,
                jakoSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                specialSpawnMultiplier: 1f,
                specialSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                trapSpawnMultiplier: 1f,
                trapSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                trapRespawnMode: trapRespawnMode,
                trapRespawnDelaySeconds: 5f,
                trapRespawnDelayMinSeconds: 5f,
                trapRespawnDelayMaxSeconds: 30f,
                trapRespawnMinPlayerDistanceMeters: 10f,
                otherSpawnMultiplier: 1f,
                otherSpawnPerPlayerMultiplier: ScalingMath.DefaultPerPlayerMultiplier,
                ambientMonsterWaveMode: "Vanilla",
                ambientMonsterWaveInitialDelaySeconds: 60f,
                ambientMonsterWaveInitialDelayMinSeconds: 30f,
                ambientMonsterWaveInitialDelayMaxSeconds: 90f,
                ambientMonsterWaveIntervalSeconds: 30f,
                ambientMonsterWaveIntervalMinSeconds: 20f,
                ambientMonsterWaveIntervalMaxSeconds: 45f,
                bonusEncounterDelayMinSeconds: 5f,
                bonusEncounterDelayMaxSeconds: 30f,
                bonusEncounterMinPlayerDistanceMeters: 10f);

        [Fact]
        public void ShouldScheduleEncounter_returns_true_when_respawn_budget_available()
        {
            bool shouldSchedule = MapPlacedEncounterScheduleResolver.ShouldScheduleEncounter(
                Config(),
                SpawnCategory.Boss,
                hasRespawnBudget: true);

            Assert.True(shouldSchedule);
        }

        [Fact]
        public void ShouldScheduleEncounter_returns_false_without_budget_or_trap_mode()
        {
            bool shouldSchedule = MapPlacedEncounterScheduleResolver.ShouldScheduleEncounter(
                Config(),
                SpawnCategory.Boss,
                hasRespawnBudget: false);

            Assert.False(shouldSchedule);
        }

        [Fact]
        public void ShouldScheduleEncounter_returns_false_for_vanilla_trap_without_budget()
        {
            bool shouldSchedule = MapPlacedEncounterScheduleResolver.ShouldScheduleEncounter(
                Config("Vanilla"),
                SpawnCategory.Trap,
                hasRespawnBudget: false);

            Assert.False(shouldSchedule);
        }

        [Theory]
        [InlineData("Fixed")]
        [InlineData("Random")]
        public void ShouldScheduleEncounter_returns_true_for_non_vanilla_trap_without_budget(string trapRespawnMode)
        {
            bool shouldSchedule = MapPlacedEncounterScheduleResolver.ShouldScheduleEncounter(
                Config(trapRespawnMode),
                SpawnCategory.Trap,
                hasRespawnBudget: false);

            Assert.True(shouldSchedule);
        }

        [Fact]
        public void HasRespawnBudget_rejects_on_start_map_once()
        {
            bool hasBudget = MapPlacedEncounterScheduleResolver.HasRespawnBudget(
                SpawnType.OnStartMap,
                maxRespawnCount: 5,
                currentSpawnCount: 0,
                enableReset: false);

            Assert.False(hasBudget);
        }

        [Fact]
        public void HasRespawnBudget_allows_event_action_with_remaining_count()
        {
            bool hasBudget = MapPlacedEncounterScheduleResolver.HasRespawnBudget(
                SpawnType.EventAction,
                maxRespawnCount: 3,
                currentSpawnCount: 2,
                enableReset: false);

            Assert.True(hasBudget);
        }
    }
}
