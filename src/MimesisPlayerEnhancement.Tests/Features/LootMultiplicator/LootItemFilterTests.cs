using System.Reflection;
using MimesisPlayerEnhancement.Features.LootMultiplicator;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.LootMultiplicator
{
    public sealed class LootItemFilterTests
    {
        private static readonly Type FilterType = typeof(LootItemFilter);

        [Fact]
        public void IsSpawnAllowed_blocklist_excludes_listed_ids()
        {
            ConfigureFilter(
                LootItemFilterMode.BlocklistOnly,
                allowlist: [],
                blocklist: [100, 200]);

            Assert.False(LootItemFilter.IsSpawnAllowed(100));
            Assert.False(LootItemFilter.IsSpawnAllowed(200));
            Assert.True(LootItemFilter.IsSpawnAllowed(50));
        }

        [Fact]
        public void IsSpawnAllowed_allowlist_only_permits_listed_ids()
        {
            ConfigureFilter(
                LootItemFilterMode.AllowlistOnly,
                allowlist: [10, 20],
                blocklist: []);

            Assert.True(LootItemFilter.IsSpawnAllowed(10));
            Assert.False(LootItemFilter.IsSpawnAllowed(99));
        }

        [Fact]
        public void ApplyToDropList_blocklist_removes_denied_ids()
        {
            ConfigureFilter(
                LootItemFilterMode.BlocklistOnly,
                allowlist: [],
                blocklist: [20]);

            List<int> drops = [10, 20, 30];
            LootItemFilter.ApplyToDropList(drops, dropInfo: null, shouldApply: true);

            Assert.Equal([10, 30], drops);
        }

        [Fact]
        public void ApplyToDropList_allowlist_replaces_denied_ids()
        {
            ConfigureFilter(
                LootItemFilterMode.AllowlistOnly,
                allowlist: [30],
                blocklist: [],
                validAllowlist: [30]);

            List<int> drops = [10, 20];
            LootItemFilter.ApplyToDropList(drops, dropInfo: null, shouldApply: true);

            Assert.Equal([30, 30], drops);
        }

        private static void ConfigureFilter(
            LootItemFilterMode mode,
            HashSet<int> allowlist,
            HashSet<int> blocklist,
            List<int>? validAllowlist = null)
        {
            SetFilterField("_cachedMode", mode);
            SetFilterField("_cachedAllowlist", allowlist);
            SetFilterField("_cachedBlocklist", blocklist);
            SetFilterField("_validAllowlistIds", validAllowlist ?? [.. allowlist]);
            SetFilterField("_masterdataValidated", true);
        }

        private static void SetFilterField(string name, object value)
        {
            FieldInfo? field = FilterType.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(null, value);
        }
    }
}
