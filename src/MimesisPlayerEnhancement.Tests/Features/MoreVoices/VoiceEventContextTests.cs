using MimesisPlayerEnhancement.Features.MoreVoices;
using Mimic.Voice.SpeechSystem;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class VoiceEventContextTests
    {
        [Theory]
        [InlineData(SpeechType_Area.DeathMatch, true)]
        [InlineData(SpeechType_Area.Outdoor, false)]
        [InlineData(SpeechType_Area.Tram, false)]
        [InlineData(SpeechType_Area.Indoor, false)]
        public void IsDeathMatch_only_matches_deathmatch_area(SpeechType_Area area, bool expected)
        {
            Assert.Equal(expected, VoiceEventContext.IsDeathMatch(area));
        }

        [Theory]
        [InlineData(SpeechType_Area.Outdoor, true)]
        [InlineData(SpeechType_Area.Tram, true)]
        [InlineData(SpeechType_Area.DeathMatch, false)]
        [InlineData(SpeechType_Area.Indoor, false)]
        public void IsOutdoorArea_matches_outdoor_and_tram(SpeechType_Area area, bool expected)
        {
            Assert.Equal(expected, VoiceEventContext.IsOutdoorArea(area));
        }
    }
}
