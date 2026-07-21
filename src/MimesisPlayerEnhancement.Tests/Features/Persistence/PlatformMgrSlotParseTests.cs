using MimesisPlayerEnhancement.Features.Persistence.Patches;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class PlatformMgrSlotParseTests
    {
        [Theory]
        [InlineData("MMGameData3.sav", 3)]
        [InlineData("MMGameData0", 0)]
        public void TryParseSlotIdFromGameDataFile_parses_valid_names(string fileName, int expectedSlotId)
        {
            bool ok = PlatformMgrSlotParser.TryParseSlotIdFromGameDataFile(fileName, out int slotId);

            Assert.True(ok);
            Assert.Equal(expectedSlotId, slotId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("OtherSave3.sav")]
        [InlineData("MMGameDataX.sav")]
        public void TryParseSlotIdFromGameDataFile_rejects_invalid_names(string? fileName)
        {
            bool ok = PlatformMgrSlotParser.TryParseSlotIdFromGameDataFile(fileName!, out int slotId);

            Assert.False(ok);
            Assert.Equal(0, slotId);
        }
    }
}
