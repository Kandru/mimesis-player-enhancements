using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ConfigConfigBoundsTests
    {
        [Theory]
        [InlineData("MimesisPlayerEnhancement_Economy", "StartupMoneyMultiplier", "0.0", null)]
        [InlineData("MimesisPlayerEnhancement_Economy", "ShopDiscountChancePercent", "0", "100")]
        [InlineData("MimesisPlayerEnhancement_LootMultiplicator", "ConvertFakeActorDyingDropChancePercent", "0", "100")]
        [InlineData("MimesisPlayerEnhancement_MorePlayers", "MaxPlayers", "1", null)]
        [InlineData("MimesisPlayerEnhancement_WebDashboard", "WebDashboardListenPort", "1", "65535")]
        public void TryGet_returns_expected_bounds(
            string sectionId,
            string key,
            string minValue,
            string? maxValue)
        {
            Assert.True(ModConfigEntryBounds.TryGet(sectionId, key, out ModConfigEntryBound bound));
            Assert.Equal(minValue, bound.MinValue);
            Assert.Equal(maxValue, bound.MaxValue);
        }

        [Fact]
        public void TryGet_is_case_insensitive_for_section_and_key()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                "mimesisplayerenhancement_economy",
                "startupmoneymultiplier",
                out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void TryGet_returns_false_for_unknown_entry()
        {
            Assert.False(ModConfigEntryBounds.TryGet(
                "MimesisPlayerEnhancement_Economy",
                "NotARealKey",
                out _));
        }
    }
}
