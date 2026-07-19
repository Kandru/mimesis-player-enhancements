using MimesisPlayerEnhancement.Features.WebDashboard;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardMinimapMathTests
    {
        [Fact]
        public void Normalize_returns_center_when_span_is_zero()
        {
            float result = WebDashboardMinimapMath.Normalize(10f, 0f, 0f);

            Assert.Equal(0.5f, result);
        }

        [Fact]
        public void Normalize_returns_center_when_span_is_negative()
        {
            float result = WebDashboardMinimapMath.Normalize(10f, 0f, -5f);

            Assert.Equal(0.5f, result);
        }

        [Theory]
        [InlineData(0f, 0f, 10f, 0f)]
        [InlineData(10f, 0f, 10f, 1f)]
        [InlineData(5f, 0f, 10f, 0.5f)]
        public void Normalize_clamps_to_unit_interval(float value, float min, float span, float expected)
        {
            float result = WebDashboardMinimapMath.Normalize(value, min, span);

            Assert.Equal(expected, result, precision: 5);
        }

        [Fact]
        public void Normalize_clamps_below_min_to_zero()
        {
            float result = WebDashboardMinimapMath.Normalize(-5f, 0f, 10f);

            Assert.Equal(0f, result);
        }

        [Fact]
        public void Normalize_clamps_above_max_to_one()
        {
            float result = WebDashboardMinimapMath.Normalize(25f, 0f, 10f);

            Assert.Equal(1f, result);
        }
    }
}
