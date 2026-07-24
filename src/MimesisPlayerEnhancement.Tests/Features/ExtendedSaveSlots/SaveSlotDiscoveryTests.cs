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

        [Theory]
        [InlineData(new int[0], 3, 1)]
        [InlineData(new[] { 1 }, 3, 2)]
        [InlineData(new[] { 1, 2 }, 3, 3)]
        [InlineData(new[] { 1, 2, 3 }, 3, -1)]
        [InlineData(new[] { 1, 2, 3 }, 99, 4)]
        [InlineData(new[] { 2, 3 }, 3, 1)]
        [InlineData(new[] { 1, 3 }, 3, 2)]
        public void FindFirstFreeManualSlot_returns_lowest_unoccupied(
            int[] occupied,
            int maxManual,
            int expected)
        {
            int free = SaveSlotDiscovery.FindFirstFreeManualSlot(occupied, maxManual);

            Assert.Equal(expected, free);
        }

        [Fact]
        public void FindFirstFreeManualSlot_returns_minus_one_when_max_below_minimum()
        {
            int free = SaveSlotDiscovery.FindFirstFreeManualSlot([], maxManual: 0);

            Assert.Equal(-1, free);
        }
    }
}
