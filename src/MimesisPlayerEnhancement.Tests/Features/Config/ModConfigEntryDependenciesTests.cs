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
            WebDashboardConfigEntryDto entry = Entry("InitialPeriodicSpawnWaitSeconds");

            ModConfigEntryDependencies.ApplyToEntry(section, entry);

            Assert.Equal("PeriodicSpawnWaitMode", entry.DependsOnKey);
            Assert.Equal("Fixed", entry.DependsOnValue);
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
    }
}
