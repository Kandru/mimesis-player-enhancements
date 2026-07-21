using MimesisPlayerEnhancement.Features.Persistence;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class SpeechEventVoiceOwnershipTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("voice-a", true)]
        [InlineData("voice-b", false)]
        public void IsOwnedVoiceId_matches_valid_voice_set(string? playerName, bool expected)
        {
            HashSet<string> validVoiceIds = new(StringComparer.Ordinal) { "voice-a" };

            bool owned = SpeechEventVoiceOwnership.IsOwnedVoiceId(playerName, validVoiceIds);

            Assert.Equal(expected, owned);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(3, true)]
        public void CanPruneOrphans_requires_non_empty_valid_set(int voiceIdCount, bool expected)
        {
            HashSet<string> validVoiceIds = [];
            for (int i = 0; i < voiceIdCount; i++)
            {
                _ = validVoiceIds.Add($"voice-{i}");
            }

            bool canPrune = SpeechEventVoiceOwnership.CanPruneOrphans(validVoiceIds);

            Assert.Equal(expected, canPrune);
        }
    }
}
