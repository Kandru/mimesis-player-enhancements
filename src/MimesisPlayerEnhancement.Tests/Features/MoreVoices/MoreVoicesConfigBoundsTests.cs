using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class MoreVoicesConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_MoreVoices";

        [Theory]
        [InlineData("MaxIndoorVoiceEvents")]
        [InlineData("MaxDeathMatchVoiceEvents")]
        [InlineData("MaxOutdoorVoiceEvents")]
        [InlineData("VoiceClipCacheMaxEntries")]
        public void int_entries_have_minimum_one(string entryId)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, entryId, out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }
    }
}
