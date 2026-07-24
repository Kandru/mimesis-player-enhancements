using MimesisPlayerEnhancement.Features.MoreVoices;
using Mimic.Voice.SpeechSystem;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class VoicePickBestMatchTests
    {
        [Theory]
        [InlineData(SpeechType_Area.Indoor, 0f, 60f, 3f, 60f)]
        [InlineData(SpeechType_Area.Outdoor, 1.5f, 45f, 2f, 45f)]
        [InlineData(SpeechType_Area.DeathMatch, 1.5f, 60f, 3f, 4.5f)]
        [InlineData(SpeechType_Area.DeathMatch, 0f, 60f, 7f, 7f)]
        public void ResolvePlayTimeInterval_uses_deathmatch_or_standard_cooldown(
            SpeechType_Area area,
            float random,
            float clipReuse,
            float deathMatchReuse,
            float expected)
        {
            float interval = VoicePickBestMatch.ResolvePlayTimeInterval(
                area,
                random,
                clipReuse,
                deathMatchReuse);

            Assert.Equal(expected, interval);
        }
    }
}
