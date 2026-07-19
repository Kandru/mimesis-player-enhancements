using System.Globalization;
using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Weather
{
    public sealed class WeatherConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_Weather";

        [Fact]
        public void WeatherCycleMinDelaySeconds_has_minimum_zero()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "WeatherCycleMinDelaySeconds",
                out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void WeatherCycleMaxDelaySeconds_has_minimum_zero()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "WeatherCycleMaxDelaySeconds",
                out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }
    }
}
