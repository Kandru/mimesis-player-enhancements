using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Util
{
    public sealed class GameSessionAccessSaveSlotTests
    {
        /// <summary>
        /// Mirrors <see cref="MimesisPlayerEnhancement.Util.GameSessionAccess.IsValidSaveSlotId"/>
        /// extended-mode branch without MelonPreferences.
        /// </summary>
        private static bool IsValidExtendedSaveSlotId(int slotId, bool extendedEnabled)
        {
            if (slotId == -1)
            {
                return false;
            }

            if (slotId == SaveSlotLimits.AutosaveSlotId)
            {
                return true;
            }

            int maxManual = SaveSlotDiscovery.GetMaxManualSlots(extendedEnabled);
            return slotId >= SaveSlotLimits.MinManualSlotId && slotId <= maxManual;
        }

        [Theory]
        [InlineData(0, true, true)]
        [InlineData(0, false, true)]
        [InlineData(1, true, true)]
        [InlineData(1, false, true)]
        [InlineData(3, true, true)]
        [InlineData(3, false, true)]
        [InlineData(4, true, true)]
        [InlineData(4, false, false)]
        [InlineData(99, true, true)]
        [InlineData(99, false, false)]
        [InlineData(100, true, false)]
        [InlineData(100, false, false)]
        [InlineData(-1, true, false)]
        [InlineData(-1, false, false)]
        public void Extended_slot_range_matches_discovery_limits(int slotId, bool extendedEnabled, bool expectedValid)
        {
            bool isValid = IsValidExtendedSaveSlotId(slotId, extendedEnabled);

            Assert.Equal(expectedValid, isValid);
        }

        [Theory]
        [InlineData(true, SaveSlotLimits.MaxManualSlotId)]
        [InlineData(false, SaveSlotLimits.VanillaMaxManualSlotId)]
        public void Manual_slot_ceiling_follows_extended_toggle(bool extendedEnabled, int expectedMaxManual)
        {
            int maxManual = SaveSlotDiscovery.GetMaxManualSlots(extendedEnabled);

            Assert.Equal(expectedMaxManual, maxManual);
            Assert.True(IsValidExtendedSaveSlotId(expectedMaxManual, extendedEnabled));
            Assert.False(IsValidExtendedSaveSlotId(expectedMaxManual + 1, extendedEnabled));
        }
    }
}
