using MimesisPlayerEnhancement.Util.Players;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Util.Players
{
    public sealed class PlayerPresenceEventsTests
    {
        [Theory]
        [InlineData(0UL, true, true, false)]
        [InlineData(0x6001UL, false, true, false)]
        [InlineData(0x6001UL, true, false, false)]
        [InlineData(0x6001UL, true, true, true)]
        public void ShouldLoadRegistryOnRegister_matches_host_and_slot_gate(
            ulong steamId,
            bool isHost,
            bool validSlot,
            bool expected)
        {
            bool result = PlayerPresenceEvents.ShouldLoadRegistryOnRegister(steamId, isHost, validSlot);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0UL, true, false)]
        [InlineData(0x6002UL, false, false)]
        [InlineData(0x6002UL, true, true)]
        public void ShouldHandleUnregister_requires_steam_id_and_statistics(
            ulong steamId,
            bool statisticsEnabled,
            bool expected)
        {
            bool result = PlayerPresenceEvents.ShouldHandleUnregister(steamId, statisticsEnabled);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(false, true, true, 1, 1, false)]
        [InlineData(true, false, true, 1, 1, false)]
        [InlineData(true, true, false, 1, 1, false)]
        [InlineData(true, true, true, 1, 2, false)]
        [InlineData(true, true, true, 3, 3, true)]
        public void CanStartArchivePresence_requires_stats_valid_slot_and_match(
            bool archivePresent,
            bool statisticsEnabled,
            bool validSlot,
            int slotId,
            int loadedSlotId,
            bool expected)
        {
            bool result = PlayerPresenceEvents.CanStartArchivePresence(
                archivePresent,
                statisticsEnabled,
                validSlot,
                slotId,
                loadedSlotId);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0UL, false)]
        [InlineData(0x6003UL, true)]
        public void ShouldApplyArchivePresence_rejects_zero_steam_id(ulong steamId, bool expected)
        {
            bool result = PlayerPresenceEvents.ShouldApplyArchivePresence(steamId);

            Assert.Equal(expected, result);
        }
    }
}
