using System.Text;
using MimesisPlayerEnhancement.Features.Persistence;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class SpeechEventFileHeaderTests
    {
        [Fact]
        public void TryReadEventCount_reads_v3_header_count()
        {
            byte[] data = BuildV3Header(version: 3, count: 42);

            bool ok = SpeechEventFileHeader.TryReadEventCount(data, out int count);

            Assert.True(ok);
            Assert.Equal(42, count);
        }

        [Fact]
        public void TryReadEventCount_reads_legacy_v2_leading_count()
        {
            byte[] data = BitConverter.GetBytes(17);

            bool ok = SpeechEventFileHeader.TryReadEventCount(data, out int count);

            Assert.True(ok);
            Assert.Equal(17, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(100001)]
        public void TryReadEventCount_rejects_invalid_counts(int invalidCount)
        {
            byte[] data = BitConverter.GetBytes(invalidCount);

            bool ok = SpeechEventFileHeader.TryReadEventCount(data, out int count);

            Assert.False(ok);
            Assert.Equal(0, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void TryReadEventCount_returns_false_for_short_buffer(int length)
        {
            byte[] data = new byte[length];

            bool ok = SpeechEventFileHeader.TryReadEventCount(data, out int count);

            Assert.False(ok);
            Assert.Equal(0, count);
        }

        [Fact]
        public void HasMagicHeader_detects_MPEV_prefix()
        {
            byte[] data = BuildV3Header(version: 3, count: 1);

            Assert.True(SpeechEventFileHeader.HasMagicHeader(data));
            Assert.False(SpeechEventFileHeader.HasMagicHeader(BitConverter.GetBytes(1)));
        }

        private static byte[] BuildV3Header(int version, int count)
        {
            byte[] magic = Encoding.ASCII.GetBytes("MPEV");
            byte[] data = new byte[12];
            Buffer.BlockCopy(magic, 0, data, 0, magic.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(version), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(count), 0, data, 8, 4);
            return data;
        }
    }
}
