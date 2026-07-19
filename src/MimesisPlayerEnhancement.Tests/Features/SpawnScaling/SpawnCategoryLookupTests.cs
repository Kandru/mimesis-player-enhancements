using Bifrost.ConstEnum;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class SpawnCategoryLookupTests
    {
        [Fact]
        public void GetCategory_returns_mimic_when_info_is_mimic()
        {
            MonsterInfo info = MonsterInfoTestFactory.Create(MonsterType.Mimic);

            SpawnCategory category = SpawnCategoryLookup.GetCategory(info);

            Assert.Equal(SpawnCategory.Mimic, category);
            Assert.True(info.IsMimic());
        }

        [Theory]
        [InlineData("floor_trap")]
        [InlineData("SpikeTrap")]
        [InlineData("hidden TRAP")]
        public void GetCategory_returns_trap_when_name_contains_trap_hint(string name)
        {
            MonsterInfo info = MonsterInfoTestFactory.Create(MonsterType.Boss, name: name);

            SpawnCategory category = SpawnCategoryLookup.GetCategory(info);

            Assert.Equal(SpawnCategory.Trap, category);
        }

        [Fact]
        public void GetCategory_returns_trap_when_puppet_name_contains_trap_hint()
        {
            MonsterInfo info = MonsterInfoTestFactory.Create(
                MonsterType.Jako,
                name: "plain_enemy",
                puppetName: "trap_puppet");

            SpawnCategory category = SpawnCategoryLookup.GetCategory(info);

            Assert.Equal(SpawnCategory.Trap, category);
        }

        [Fact]
        public void GetCategory_returns_trap_when_bt_name_contains_trap_hint()
        {
            MonsterInfo info = MonsterInfoTestFactory.Create(
                MonsterType.Special,
                name: "plain_enemy",
                btName: "BT_Trap_Aggro");

            SpawnCategory category = SpawnCategoryLookup.GetCategory(info);

            Assert.Equal(SpawnCategory.Trap, category);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        public void GetCategory_maps_monster_type_when_not_trap_or_mimic(
            int monsterTypeValue,
            int expectedCategoryValue)
        {
            MonsterType monsterType = monsterTypeValue switch
            {
                1 => MonsterType.Boss,
                2 => MonsterType.Jako,
                3 => MonsterType.Special,
                _ => default,
            };
            var expected = (SpawnCategory)expectedCategoryValue;
            MonsterInfo info = MonsterInfoTestFactory.Create(monsterType, name: "plain_enemy");

            SpawnCategory category = SpawnCategoryLookup.GetCategory(info);

            Assert.Equal(expected, category);
        }

        [Fact]
        public void GetCategory_returns_other_for_unclassified_monster()
        {
            MonsterInfo info = MonsterInfoTestFactory.Create(default, name: "generic_creature");

            SpawnCategory category = SpawnCategoryLookup.GetCategory(info);

            Assert.Equal(SpawnCategory.Other, category);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetCategory_by_master_id_returns_other_without_excel(int masterId)
        {
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);

            Assert.Equal(SpawnCategory.Other, category);
        }

        [Theory]
        [InlineData(0, "Mimic")]
        [InlineData(4, "Trap")]
        [InlineData(5, "Other")]
        public void Format_returns_category_name(int categoryValue, string expected)
        {
            var category = (SpawnCategory)categoryValue;
            Assert.Equal(expected, SpawnCategoryLookup.Format(category));
        }
    }
}
