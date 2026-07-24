using System.Text;
using MimesisPlayerEnhancement.Features.Persistence;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class SpeechEventFileStoreTests
    {
        [Fact]
        public void BuildSnapshot_empty_records_yields_delete_payload()
        {
            SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.BuildSnapshot(
                "/tmp/MMGameData0.mpe-speech.sav",
                []);

            Assert.Equal("/tmp/MMGameData0.mpe-speech.sav", snapshot.SpeechPath);
            Assert.Null(snapshot.SpeechBytes);
            Assert.Equal(0, snapshot.SerializedCount);
        }

        [Fact]
        public void BuildSnapshot_packs_v3_header_and_records()
        {
            List<SpeechEventCapturedRecord> records =
            [
                new SpeechEventCapturedRecord { Meta = [1, 2, 3], Audio = [9, 8] },
                new SpeechEventCapturedRecord { Meta = [4], Audio = [] },
            ];

            SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.BuildSnapshot(
                "speech.sav",
                records);

            Assert.Equal(2, snapshot.SerializedCount);
            Assert.NotNull(snapshot.SpeechBytes);
            Assert.True(SpeechEventFileHeader.HasMagicHeader(snapshot.SpeechBytes));
            Assert.True(SpeechEventFileHeader.TryReadEventCount(snapshot.SpeechBytes, out int count));
            Assert.Equal(2, count);

            using MemoryStream ms = new(snapshot.SpeechBytes);
            using BinaryReader br = new(ms);
            Assert.Equal("MPEV", Encoding.ASCII.GetString(br.ReadBytes(4)));
            Assert.Equal(3, br.ReadInt32());
            Assert.Equal(2, br.ReadInt32());

            Assert.Equal(3, br.ReadInt32());
            Assert.Equal(new byte[] { 1, 2, 3 }, br.ReadBytes(3));
            Assert.Equal(2, br.ReadInt32());
            Assert.Equal(new byte[] { 9, 8 }, br.ReadBytes(2));

            Assert.Equal(1, br.ReadInt32());
            Assert.Equal(new byte[] { 4 }, br.ReadBytes(1));
            Assert.Equal(0, br.ReadInt32());
        }

        [Fact]
        public void BuildSnapshot_with_empty_path_skips_bytes()
        {
            SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.BuildSnapshot(
                string.Empty,
                [new SpeechEventCapturedRecord { Meta = [1], Audio = [2] }]);

            Assert.Equal(string.Empty, snapshot.SpeechPath);
            Assert.Null(snapshot.SpeechBytes);
            Assert.Equal(0, snapshot.SerializedCount);
        }

        [Fact]
        public void CaptureRecords_returns_empty_for_empty_input()
        {
            List<SpeechEventCapturedRecord> records = SpeechEventFileStore.CaptureRecords([]);

            Assert.Empty(records);
        }
    }
}
