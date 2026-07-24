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

        [Fact]
        public void IsTrapOrMonster_true_for_trap_area()
        {
            SpeechEvent evt = CreateEvent(new SpeechEventAdditionalGameData
            {
                Area = SpeechType_Area.BearTrapING,
            });

            Assert.True(VoiceEventContext.IsTrapOrMonster(evt));
        }

        [Fact]
        public void IsTrapOrMonster_true_when_monsters_present()
        {
            SpeechEvent evt = CreateEvent(new SpeechEventAdditionalGameData
            {
                Area = SpeechType_Area.Indoor,
                Monsters = [1],
            });

            Assert.True(VoiceEventContext.IsTrapOrMonster(evt));
        }

        [Fact]
        public void IsTrapOrMonster_false_for_plain_indoor()
        {
            SpeechEvent evt = CreateEvent(new SpeechEventAdditionalGameData
            {
                Area = SpeechType_Area.Indoor,
            });

            Assert.False(VoiceEventContext.IsTrapOrMonster(evt));
        }

        private static SpeechEvent CreateEvent(SpeechEventAdditionalGameData gameData) =>
            new(
                id: 1,
                playerName: "player",
                recordedTime: 0f,
                channels: 1,
                sampleRate: 48000,
                compressedAudioData: [],
                originalAudioDataLength: 0,
                averageAmplitude: 0f,
                gameData: gameData,
                lastPlayedTime: 0f);
    }
}
