using MimesisPlayerEnhancement.Features.Players;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Players
{
    public sealed class VoiceEventStatsTests
    {
        [Fact]
        public void GetVoiceId_returns_question_mark_for_null_archive()
        {
            string voiceId = VoiceEventStats.GetVoiceId(null);

            Assert.Equal("?", voiceId);
        }

        [Fact]
        public void DescribePlayer_returns_null_marker_for_null_archive()
        {
            string description = VoiceEventStats.DescribePlayer(null);

            Assert.Equal("archive=null", description);
        }

        [Fact]
        public void DescribePlayerBrief_returns_unknown_for_null_archive()
        {
            string description = VoiceEventStats.DescribePlayerBrief(null);

            Assert.Equal("player=unknown", description);
        }

        [Fact]
        public void TryCaptureArchiveIdentity_returns_false_for_null_archive()
        {
            bool captured = VoiceEventStats.TryCaptureArchiveIdentity(
                null,
                out long playerUid,
                out bool isLocal,
                out ulong steamId);

            Assert.False(captured);
            Assert.Equal(0, playerUid);
            Assert.False(isLocal);
            Assert.Equal(0UL, steamId);
        }
    }
}
