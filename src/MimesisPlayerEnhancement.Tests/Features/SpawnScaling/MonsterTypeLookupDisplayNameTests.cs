using Bifrost.Cooked;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SpawnScaling
{
    public sealed class MonsterTypeLookupDisplayNameTests
    {
        [Fact]
        public void GetDisplayName_uses_info_name_when_present()
        {
            MonsterInfo info = MonsterInfoTestFactory.CreateNamed("Goblin");

            string displayName = MonsterTypeLookup.GetDisplayName(masterId: 42, info);

            Assert.Equal("Goblin", displayName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetDisplayName_falls_back_to_master_id_when_name_blank(string? name)
        {
            MonsterInfo info = MonsterInfoTestFactory.CreateNamed(name ?? string.Empty);

            string displayName = MonsterTypeLookup.GetDisplayName(masterId: 99, info);

            Assert.Equal("99", displayName);
        }

        [Fact]
        public void GetDisplayName_falls_back_to_master_id_when_lookup_fails_without_info()
        {
            string displayName = MonsterTypeLookup.GetDisplayName(masterId: 123);

            Assert.Equal("123", displayName);
        }
    }
}
