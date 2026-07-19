using MimesisPlayerEnhancement.Features.MoreVoices;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class VoicePickSimilarityTests
    {
        [Theory]
        [InlineData(2f, 2f, 4f, 1f)]
        [InlineData(0f, 4f, 4f, 0f)]
        [InlineData(0f, 8f, 4f, 0f)]
        [InlineData(1f, 2f, 0f, 0f)]
        [InlineData(1f, 2f, -1f, 0f)]
        public void CalcSimTerm_clamps_normalized_similarity(float a, float b, float maxDiff, float expected)
        {
            float similarity = VoicePickSimilarity.CalcSimTerm(a, b, maxDiff);

            Assert.Equal(expected, similarity);
        }

        [Fact]
        public void CountScrap_null_returns_zero_counts()
        {
            VoicePickSimilarity.ScrapCounts counts = VoicePickSimilarity.CountScrap(null);

            Assert.Equal(0, counts.OneHand);
            Assert.Equal(0, counts.TwoHand);
        }

        [Fact]
        public void CountScrap_empty_list_returns_zero_counts()
        {
            VoicePickSimilarity.ScrapCounts counts = VoicePickSimilarity.CountScrap([]);

            Assert.Equal(0, counts.OneHand);
            Assert.Equal(0, counts.TwoHand);
        }
    }
}
