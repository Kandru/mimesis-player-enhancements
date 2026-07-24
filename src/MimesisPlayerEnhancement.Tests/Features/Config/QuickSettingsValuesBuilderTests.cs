using MimesisPlayerEnhancement.Config.QuickSettings;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class QuickSettingsValuesBuilderTests
    {
        [Fact]
        public void Set_creates_case_insensitive_section_and_key()
        {
            Dictionary<string, Dictionary<string, string>> map = QuickSettingsValuesBuilder.CreateMap();

            QuickSettingsValuesBuilder.Set(map, "MimesisPlayerEnhancement_Economy", "StartupMoneyMultiplier", "2.0");

            Assert.True(map.TryGetValue("mimesisplayerenhancement_economy", out Dictionary<string, string>? keys));
            Assert.NotNull(keys);
            Assert.Equal("2.0", keys["startupmoneymultiplier"]);
        }

        [Fact]
        public void SetBool_writes_lowercase_true_false()
        {
            Dictionary<string, Dictionary<string, string>> map = QuickSettingsValuesBuilder.CreateMap();

            QuickSettingsValuesBuilder.SetBool(map, "MimesisPlayerEnhancement_Economy", "EnableEconomy", true);
            QuickSettingsValuesBuilder.SetBool(map, "MimesisPlayerEnhancement_Economy", "EnableEconomy", false);

            Assert.Equal("false", map["MimesisPlayerEnhancement_Economy"]["EnableEconomy"]);
        }

        [Fact]
        public void SetEconomyMultipliers_formats_floats_invariant()
        {
            Dictionary<string, Dictionary<string, string>> map = QuickSettingsValuesBuilder.CreateMap();

            QuickSettingsValuesBuilder.SetEconomyMultipliers(map, 1.5f, 2f, 0.8f, 1.25f);

            Dictionary<string, string> economy = map["MimesisPlayerEnhancement_Economy"];
            Assert.Equal("1.5", economy["StartupMoneyMultiplier"]);
            Assert.Equal("2.0", economy["ScrapSellValueMultiplier"]);
            Assert.Equal("0.8", economy["ShopBuyPriceMultiplier"]);
            Assert.Equal("1.25", economy["ReinforcePriceMultiplier"]);
        }

        [Fact]
        public void SetLootMultipliers_and_spawn_multipliers_write_expected_keys()
        {
            Dictionary<string, Dictionary<string, string>> map = QuickSettingsValuesBuilder.CreateMap();

            QuickSettingsValuesBuilder.SetLootMultipliers(map, mapLoot: 3f, dropLoot: 2f);
            QuickSettingsValuesBuilder.SetSpawnMultipliers(map, 0.25f);

            Assert.Equal("3.0", map["MimesisPlayerEnhancement_LootMultiplicator"]["MapLootMultiplier"]);
            Assert.Equal("2.0", map["MimesisPlayerEnhancement_LootMultiplicator"]["DropLootMultiplier"]);
            Assert.Equal("0.25", map["MimesisPlayerEnhancement_SpawnScaling"]["MimicSpawnMultiplier"]);
            Assert.Equal("0.25", map["MimesisPlayerEnhancement_SpawnScaling"]["BossSpawnMultiplier"]);
        }

        [Fact]
        public void CloneValues_copies_nested_dictionaries()
        {
            Dictionary<string, Dictionary<string, string>> source = QuickSettingsValuesBuilder.CreateMap();
            QuickSettingsValuesBuilder.Set(source, "Section", "Key", "value");

            Dictionary<string, Dictionary<string, string>> clone = QuickSettingsValuesBuilder.CloneValues(source);
            QuickSettingsValuesBuilder.Set(clone, "Section", "Key", "changed");

            Assert.Equal("value", source["Section"]["Key"]);
            Assert.Equal("changed", clone["Section"]["Key"]);
        }

        [Fact]
        public void SetPeriodicSpawnWaitRandom_sets_mode_and_bounds()
        {
            Dictionary<string, Dictionary<string, string>> map = QuickSettingsValuesBuilder.CreateMap();

            QuickSettingsValuesBuilder.SetPeriodicSpawnWaitRandom(map, 1f, 2f, 3f, 4f);

            Dictionary<string, string> spawn = map["MimesisPlayerEnhancement_SpawnScaling"];
            Assert.Equal("Random", spawn["PeriodicSpawnWaitMode"]);
            Assert.Equal("1.0", spawn["InitialPeriodicSpawnWaitMinSeconds"]);
            Assert.Equal("2.0", spawn["InitialPeriodicSpawnWaitMaxSeconds"]);
            Assert.Equal("3.0", spawn["PeriodicSpawnIntervalMinSeconds"]);
            Assert.Equal("4.0", spawn["PeriodicSpawnIntervalMaxSeconds"]);
        }
    }
}
