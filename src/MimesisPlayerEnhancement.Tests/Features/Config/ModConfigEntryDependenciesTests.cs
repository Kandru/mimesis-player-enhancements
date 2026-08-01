using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.UserInterface;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ModConfigEntryDependenciesTests
    {
        private static WebDashboardConfigSectionDto Section(string id) => new() { Id = id };

        private static WebDashboardConfigEntryDto Entry(string key) => new() { Key = key };

        [Fact]
        public void ApplyToEntry_spawn_scaling_periodic_wait_fixed_mode_dependency()
        {
            WebDashboardConfigSectionDto section = Section("MimesisPlayerEnhancement_SpawnScaling");
            WebDashboardConfigEntryDto entry = Entry("GruntWaveInitialDelaySeconds");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("GruntWaveMode", entry.DependsOnKey);
            Assert.Equal("Fixed", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_spawn_scaling_mimic_wave_random_mode_dependency()
        {
            WebDashboardConfigSectionDto section = Section("MimesisPlayerEnhancement_SpawnScaling");
            WebDashboardConfigEntryDto entry = Entry("MimicWaveIntervalMinSeconds");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("MimicWaveMode", entry.DependsOnKey);
            Assert.Equal("Random", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_economy_discount_dependency()
        {
            WebDashboardConfigSectionDto section = Section("MimesisPlayerEnhancement_Economy");
            WebDashboardConfigEntryDto entry = Entry("ShopDiscountMinPercent");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("ShopDiscountChancePercent", entry.DependsOnKey);
            Assert.Equal(">0", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_dungeon_randomizer_pick_dependency()
        {
            WebDashboardConfigSectionDto section = Section("MimesisPlayerEnhancement_DungeonRandomizer");
            WebDashboardConfigEntryDto entry = Entry("DungeonAllowlist");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("RandomizeDungeonPick", entry.DependsOnKey);
            Assert.Null(entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_ui_round_start_sound_variant_dependency()
        {
            WebDashboardConfigSectionDto section = Section(UiConfig.SectionId);
            WebDashboardConfigEntryDto entry = Entry("RoundStartSoundVariant");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("RoundStartSoundMode", entry.DependsOnKey);
            Assert.Equal("Specific", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_ui_disco_ball_sound_variant_dependency()
        {
            WebDashboardConfigSectionDto section = Section(UiConfig.SectionId);
            WebDashboardConfigEntryDto entry = Entry("DiscoBallSoundVariant");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("DiscoBallSoundMode", entry.DependsOnKey);
            Assert.Equal("Specific", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_ui_spectator_voice_duck_level_dependency()
        {
            WebDashboardConfigSectionDto section = Section(UiConfig.SectionId);
            WebDashboardConfigEntryDto entry = Entry("SpectatorVoiceDuckLevel");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("SpectatorVoiceBalanceMode", entry.DependsOnKey);
            Assert.Equal("SpeechDucking", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_ui_spectator_voice_attenuation_dependency()
        {
            WebDashboardConfigSectionDto section = Section(UiConfig.SectionId);
            WebDashboardConfigEntryDto entry = Entry("SpectatorVoiceAttenuation");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("SpectatorVoiceBalanceMode", entry.DependsOnKey);
            Assert.Equal("StaticAttenuation", entry.DependsOnValue);
        }

        [Fact]
        public void ApplyToEntry_spawn_scaling_trap_respawn_min_distance_dependency()
        {
            WebDashboardConfigSectionDto section = Section("MimesisPlayerEnhancement_SpawnScaling");
            WebDashboardConfigEntryDto entry = Entry("TrapRespawnMinPlayerDistanceMeters");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("TrapRespawnMode", entry.DependsOnKey);
            Assert.Equal("!=Vanilla", entry.DependsOnValue);
        }

        [Theory]
        [InlineData("MimicRunawayChance")]
        [InlineData("JumpCopyChancePercent")]
        [InlineData("SlotFollowChangeChancePercent")]
        public void ApplyToEntry_mimic_social_custom_keys_have_no_dashboard_visibility_dependency(string key)
        {
            WebDashboardConfigSectionDto section = Section(MimicTuningConfig.SectionId);
            WebDashboardConfigEntryDto entry = Entry(key);

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Null(entry.DependsOnKey);
            Assert.Null(entry.DependsOnValue);
        }
    }
}
