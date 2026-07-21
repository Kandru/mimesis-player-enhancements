using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Logging
{
    public sealed class LoggingConfigTests
    {
        [Fact]
        public void EnableDebugLogging_has_local_effect_only()
        {
            bool hasLocalEffect = ModConfigEntryLocalEffect.HasLocalEffect(
                "MimesisPlayerEnhancement",
                "EnableDebugLogging");

            Assert.True(hasLocalEffect);
        }
    }
}
