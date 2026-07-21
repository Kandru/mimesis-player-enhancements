using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerAnnouncements
{
    public sealed class BossSpawnMessageFormatterTests
    {
        [Fact]
        public void Format_returns_empty_for_no_spawns()
        {
            string result = BossSpawnMessageFormatter.Format(
                new Dictionary<int, int>(),
                _ => "Foo");

            Assert.Equal("", result);
        }

        [Fact]
        public void Format_returns_empty_when_all_counts_are_zero()
        {
            string result = BossSpawnMessageFormatter.Format(
                new Dictionary<int, int> { [1] = 0 },
                _ => "Foo");

            Assert.Equal("", result);
        }

        [Fact]
        public void Format_single_spawn_uses_article()
        {
            string result = BossSpawnMessageFormatter.Format(
                new Dictionary<int, int> { [1] = 1 },
                _ => "Egg");

            string expected = L10n("announce.spawn_appeared", new Dictionary<string, object>
            {
                ["entities"] = EntityDisplayNameFormatter.FormatWithArticle("Egg", capitalizeArticle: true),
            });
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Format_plural_spawn_uses_count_and_plural_name()
        {
            string result = BossSpawnMessageFormatter.Format(
                new Dictionary<int, int> { [1] = 3 },
                _ => "Foo");

            string segment = $"3 {EntityDisplayNameFormatter.Pluralize("Foo")}";
            string expected = L10n("announce.spawn_appeared", new Dictionary<string, object> { ["entities"] = segment });
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Format_two_spawns_joins_with_and()
        {
            string result = BossSpawnMessageFormatter.Format(
                new Dictionary<int, int>
                {
                    [1] = 1,
                    [2] = 1,
                },
                id => id == 1 ? "Foo" : "Bar");

            string first = EntityDisplayNameFormatter.FormatWithArticle("Foo", capitalizeArticle: true);
            string second = EntityDisplayNameFormatter.FormatWithArticle("Bar", capitalizeArticle: false);
            string joined = L10n("announce.spawn_join_two", new Dictionary<string, object>
            {
                ["first"] = first,
                ["second"] = second,
            });
            string expected = L10n("announce.spawn_appeared", new Dictionary<string, object> { ["entities"] = joined });
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Format_three_spawns_joins_with_oxford_comma()
        {
            string result = BossSpawnMessageFormatter.Format(
                new Dictionary<int, int>
                {
                    [1] = 1,
                    [2] = 1,
                    [3] = 1,
                },
                id => id switch
                {
                    1 => "Foo",
                    2 => "Bar",
                    _ => "Baz",
                });

            string first = EntityDisplayNameFormatter.FormatWithArticle("Foo", capitalizeArticle: true);
            string second = EntityDisplayNameFormatter.FormatWithArticle("Bar", capitalizeArticle: false);
            string third = EntityDisplayNameFormatter.FormatWithArticle("Baz", capitalizeArticle: false);
            string comma = L10n("announce.spawn_join_comma");
            string joined = L10n("announce.spawn_join_many", new Dictionary<string, object>
            {
                ["rest"] = string.Join(comma, first, second),
                ["last"] = third,
            });
            string expected = L10n("announce.spawn_appeared", new Dictionary<string, object> { ["entities"] = joined });
            Assert.Equal(expected, result);
        }

        private static string L10n(string key) =>
            ModL10n.GetForLocale("en", key);

        private static string L10n(string key, Dictionary<string, object> args) =>
            ModL10n.GetForLocale("en", key, args);
    }
}
