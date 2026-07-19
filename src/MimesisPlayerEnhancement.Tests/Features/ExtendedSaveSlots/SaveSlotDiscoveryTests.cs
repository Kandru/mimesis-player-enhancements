using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.ExtendedSaveSlots
{
    public sealed class SaveSlotDiscoveryTests
    {
        [Theory]
        [InlineData(true, SaveSlotLimits.MaxManualSlotId)]
        [InlineData(false, SaveSlotLimits.VanillaMaxManualSlotId)]
        public void GetMaxManualSlots_returns_expected_limit(bool extendedEnabled, int expectedMax)
        {
            int maxManual = SaveSlotDiscovery.GetMaxManualSlots(extendedEnabled);

            Assert.Equal(expectedMax, maxManual);
        }

        [Fact]
        public void GetMaxManualSlots_extended_exceeds_vanilla_limit()
        {
            Assert.True(
                SaveSlotDiscovery.GetMaxManualSlots(extendedEnabled: true)
                    > SaveSlotDiscovery.GetMaxManualSlots(extendedEnabled: false));
        }
    }
}
