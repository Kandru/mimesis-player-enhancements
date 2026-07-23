using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class UiConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_Ui";

        [Fact]
        public void ModToastDurationSeconds_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "ModToastDurationSeconds", out ModConfigEntryBound bound));
            Assert.Equal("1.0", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void FloatingDamageDurationSeconds_has_one_to_three_range()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "FloatingDamageDurationSeconds",
                out ModConfigEntryBound bound));
            Assert.Equal(1f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Equal(3f, float.Parse(bound.MaxValue!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void RoundStartSoundVolume_has_zero_to_one_range()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "RoundStartSoundVolume",
                out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Equal(1f, float.Parse(bound.MaxValue!, CultureInfo.InvariantCulture));
        }
    }
}
