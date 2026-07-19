using MimesisPlayerEnhancement.Features.Weather;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Weather
{
    public sealed class WeatherTimeContextTests
    {
        [Theory]
        [InlineData(36_000L, 36_000L, true)]
        [InlineData(36_000L, 0L, false)]
        [InlineData(36_000L, 18_000L, false)]
        [InlineData(0L, 0L, false)]
        public void ShouldOverrideConvertResult_matches_room_start_only_when_positive_and_equal(
            long vanillaSeconds,
            long roomVanillaStart,
            bool expected)
        {
            bool result = WeatherTimeContext.ShouldOverrideConvertResult(vanillaSeconds, roomVanillaStart);

            Assert.Equal(expected, result);
        }
    }
}
