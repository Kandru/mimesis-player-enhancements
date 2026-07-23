using System.Globalization;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ModConfigFloatHelperTests
    {
        [Theory]
        [InlineData(1f, 1f)]
        [InlineData(1.224f, 1.22f)]
        [InlineData(1.225f, 1.23f)]
        [InlineData(1.999f, 2f)]
        public void Round_rounds_away_from_zero_at_two_decimal_places(float input, float expected)
        {
            Assert.Equal(expected, ModConfigFloatHelper.Round(input));
        }

        [Theory]
        [InlineData(1f, "1.0")]
        [InlineData(1.222f, "1.22")]
        [InlineData(1.2f, "1.2")]
        public void Format_uses_invariant_culture_with_at_least_one_decimal(float input, string expected)
        {
            Assert.Equal(expected, ModConfigFloatHelper.Format(input));
            _ = float.Parse(expected, CultureInfo.InvariantCulture);
        }
    }
}
