using MimesisPlayerEnhancement.Features.MoreVoices;
using Mimic.Voice.SpeechSystem;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class SpeechEventArchiveUnifiedEvictionTests
    {
        [Fact]
        public void EvaluateValue_matches_vanilla_negated_play_count()
        {
            SpeechEvent speechEvent = CreateEvent(id: 1, played: 4);

            Assert.Equal(-4f, SpeechEventArchiveUnifiedEviction.EvaluateValue(speechEvent));
        }

        [Fact]
        public void CollectRemovals_keeps_least_played_and_removes_overflow()
        {
            List<SpeechEvent> bucket =
            [
                CreateEvent(id: 1, played: 0),
                CreateEvent(id: 2, played: 5),
                CreateEvent(id: 3, played: 1),
                CreateEvent(id: 4, played: 9),
            ];
            List<long> removed = [];

            SpeechEventArchiveUnifiedEviction.CollectRemovals(bucket, cap: 2, removed);

            Assert.Equal(2, removed.Count);
            Assert.Contains(2L, removed);
            Assert.Contains(4L, removed);
            Assert.DoesNotContain(1L, removed);
            Assert.DoesNotContain(3L, removed);
        }

        [Fact]
        public void CollectRemovals_noop_when_at_or_under_cap()
        {
            List<SpeechEvent> bucket =
            [
                CreateEvent(id: 1, played: 0),
                CreateEvent(id: 2, played: 1),
            ];
            List<long> removed = [];

            SpeechEventArchiveUnifiedEviction.CollectRemovals(bucket, cap: 2, removed);

            Assert.Empty(removed);
        }

        private static SpeechEvent CreateEvent(long id, int played)
        {
            var speechEvent = new SpeechEvent(
                id,
                "player",
                recordedTime: 0f,
                channels: 1,
                sampleRate: 48000,
                compressedAudioData: [],
                originalAudioDataLength: 0,
                averageAmplitude: 0f,
                gameData: new SpeechEventAdditionalGameData(),
                lastPlayedTime: 0f);
            speechEvent.AudioPlayedCount = played;
            return speechEvent;
        }
    }
}
