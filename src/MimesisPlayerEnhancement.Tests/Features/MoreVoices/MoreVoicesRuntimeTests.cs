using MimesisPlayerEnhancement.Features.MoreVoices;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class MoreVoicesRuntimeTests
    {
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void ShouldApply_requires_enabled_and_host(bool enabled, bool isHost, bool expected)
        {
            Assert.Equal(expected, MoreVoicesRuntime.ShouldApply(enabled, isHost));
        }
    }
}
