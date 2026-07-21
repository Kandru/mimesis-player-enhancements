using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerAnnouncements
{
    public sealed class AnnouncementMultiplierFormatTests
    {
        [Theory]
        [InlineData(1f)]
        [InlineData(0.999f)]
        [InlineData(1.001f)]
        public void IsDefaultMultiplier_returns_true_near_one(float multiplier)
        {
            Assert.True(AnnouncementMultiplierFormat.IsDefaultMultiplier(multiplier));
        }

        [Theory]
        [InlineData(0.99f)]
        [InlineData(1.01f)]
        [InlineData(1.5f)]
        public void IsDefaultMultiplier_returns_false_outside_band(float multiplier)
        {
            Assert.False(AnnouncementMultiplierFormat.IsDefaultMultiplier(multiplier));
        }

        [Fact]
        public void FormatMultiplier_formats_with_times_prefix()
        {
            Assert.Equal("×1.5", AnnouncementMultiplierFormat.FormatMultiplier(1.5f));
        }

        [Fact]
        public void FormatBonusSeconds_formats_fractional_seconds()
        {
            Assert.Equal("90.5", AnnouncementMultiplierFormat.FormatBonusSeconds(90.5d));
        }
    }
}
