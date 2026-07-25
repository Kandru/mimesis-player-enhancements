using MimesisPlayerEnhancement.Features.WebDashboard.Patches;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class GodModeContaFreezeTests
    {
        [Theory]
        [InlineData(50, 60, true)]
        [InlineData(50, 50, false)]
        [InlineData(50, 0, false)]
        [InlineData(0, 10, true)]
        public void IsContaIncrease_returns_true_only_when_proposed_exceeds_current(
            long current,
            long proposed,
            bool expected)
        {
            Assert.Equal(expected, GodModeContaFreeze.IsContaIncrease(current, proposed));
        }
    }
}
