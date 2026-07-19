using MimesisPlayerEnhancement.Features.WebDashboard;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardLocaleResolverTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveAcceptLanguage_returns_en_for_missing_header(string? header)
        {
            string locale = WebDashboardLocaleResolver.ResolveAcceptLanguage(header);

            Assert.Equal("en", locale);
        }

        [Fact]
        public void ResolveAcceptLanguage_returns_supported_locale_for_single_tag()
        {
            string locale = WebDashboardLocaleResolver.ResolveAcceptLanguage("de");

            Assert.Equal("de", locale);
        }

        [Fact]
        public void ResolveAcceptLanguage_prefers_higher_quality_value()
        {
            string locale = WebDashboardLocaleResolver.ResolveAcceptLanguage("de;q=0.5, en;q=0.9");

            Assert.Equal("en", locale);
        }

        [Fact]
        public void ResolveAcceptLanguage_normalizes_region_subtag()
        {
            string locale = WebDashboardLocaleResolver.ResolveAcceptLanguage("de-DE, en-US;q=0.8");

            Assert.Equal("de", locale);
        }

        [Fact]
        public void ResolveAcceptLanguage_falls_back_to_en_for_unsupported_locale()
        {
            string locale = WebDashboardLocaleResolver.ResolveAcceptLanguage("xx-YY");

            Assert.Equal("en", locale);
        }

        [Fact]
        public void ResolveAcceptLanguage_preserves_client_preference_order_on_equal_quality()
        {
            string locale = WebDashboardLocaleResolver.ResolveAcceptLanguage("de, en");

            Assert.Equal("de", locale);
        }
    }
}
