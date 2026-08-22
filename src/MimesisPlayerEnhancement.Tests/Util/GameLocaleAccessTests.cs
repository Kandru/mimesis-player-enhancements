using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Util
{
    public sealed class GameLocaleAccessTests
    {
        [Theory]
        [InlineData("de", "de")]
        [InlineData("DE", "de")]
        [InlineData("de-DE", "de")]
        [InlineData("de_DE", "de")]
        [InlineData("en", "en")]
        [InlineData("en-US", "en")]
        public void TryResolveSupportedLocale_maps_supported_tags(string input, string expected)
        {
            bool resolved = GameLocaleAccess.TryResolveSupportedLocale(input, out string locale);

            Assert.True(resolved);
            Assert.Equal(expected, locale);
        }

        [Theory]
        [InlineData("fr")]
        [InlineData("ko")]
        [InlineData("zh_cn")]
        [InlineData("pt_br")]
        [InlineData("xx-YY")]
        public void TryResolveSupportedLocale_rejects_unsupported_tags(string input)
        {
            bool resolved = GameLocaleAccess.TryResolveSupportedLocale(input, out string locale);

            Assert.False(resolved);
            Assert.Equal("en", locale);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryResolveSupportedLocale_rejects_empty(string? input)
        {
            bool resolved = GameLocaleAccess.TryResolveSupportedLocale(input, out string locale);

            Assert.False(resolved);
            Assert.Equal("en", locale);
        }

        [Theory]
        [InlineData("de", "de")]
        [InlineData("de-DE", "de")]
        [InlineData("fr", "en")]
        [InlineData("ko", "en")]
        [InlineData("zh_cn", "en")]
        [InlineData(null, "en")]
        [InlineData("", "en")]
        public void NormalizeLanguageCode_falls_back_to_english(string? input, string expected)
        {
            Assert.Equal(expected, GameLocaleAccess.NormalizeLanguageCode(input));
        }

        [Fact]
        public void ModL10n_german_host_button_is_translated()
        {
            Assert.Equal("Spielstände", ModL10n.GetForLocale("de", "saveslots.host_button"));
            Assert.Equal("Savegames", ModL10n.GetForLocale("en", "saveslots.host_button"));
        }

        [Fact]
        public void ModL10n_management_button_is_translated()
        {
            Assert.Equal("Verwaltung", ModL10n.GetForLocale("de", "dashboard.management_button"));
            Assert.Equal("Management", ModL10n.GetForLocale("en", "dashboard.management_button"));
        }
    }
}
