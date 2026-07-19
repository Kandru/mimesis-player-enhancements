using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.ExtendedSaveSlots
{
    public sealed class SaveSlotPickerExtraStatsTests
    {
        [Fact]
        public void ComputeRowHeight_sums_padding_and_line_heights()
        {
            float height = SaveSlotPickerExtraStats.ComputeRowHeight();

            Assert.Equal(84f, height);
        }
    }
}
