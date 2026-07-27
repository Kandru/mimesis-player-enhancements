using MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class RoundStartSoundGateTests
    {
        [Theory]
        [InlineData("Sound_UI_TramStopBGM_01", true)]
        [InlineData("sound_ui_tramstopbgm_01", true)]
        [InlineData("Sound_UI_TramStopBGM", true)]
        [InlineData("Sound_UI_TramStopBGM_02", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void MatchesLandingMelodySfxId_recognizes_primary_and_alt_ids(string? sfxId, bool expected)
        {
            Assert.Equal(expected, RoundStartSoundGate.MatchesLandingMelodySfxId(sfxId));
        }

        [Fact]
        public void MatchesLandingMelodySfxId_trims_whitespace()
        {
            Assert.True(RoundStartSoundGate.MatchesLandingMelodySfxId("  Sound_UI_TramStopBGM  "));
        }
    }
}
