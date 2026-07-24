using System.IO;
using System.Reflection;
using System.Text;
using ReluReplay.Data;
using ReluReplay.Serializer;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal sealed class SpeechEventCapturedRecord
    {
        internal byte[] Meta = [];
        internal byte[] Audio = [];
    }

    internal static class SpeechEventFileStore
    {
        private const string Feature = "Persistence";

        private static readonly byte[] FileMagic = Encoding.ASCII.GetBytes("MPEV");
        private const int FileFormatVersion = 3;

        private static readonly FieldInfo? CompressedAudioDataField =
            typeof(SpeechEvent).GetField("CompressedAudioData", BindingFlags.Public | BindingFlags.Instance);

        internal static bool HasSpeechEventsFile(int slotId)
        {
            string? filePath = SaveSidecarPaths.GetSpeechPath(slotId);
            return !string.IsNullOrEmpty(filePath)
                && (File.Exists(filePath) || File.Exists(filePath + ".bak"));
        }

        internal static int TryGetSavedSpeechEventCount(int slotId) => TryReadSpeechCountFromBinary(slotId);

        private static int TryReadSpeechCountFromBinary(int slotId)
        {
            string? filePath = SaveSidecarPaths.GetSpeechPath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return 0;
            }

            if (!File.Exists(filePath))
            {
                filePath += ".bak";
                if (!File.Exists(filePath))
                {
                    return 0;
                }
            }

            try
            {
                byte[] header = new byte[12];
                using FileStream stream = File.OpenRead(filePath);
                int read = stream.Read(header, 0, header.Length);
                if (read < 4)
                {
                    return 0;
                }

                if (SpeechEventFileHeader.TryReadEventCount(header.AsSpan(0, read), out int count))
                {
                    return count;
                }

                if (SpeechEventFileHeader.HasMagicHeader(header.AsSpan(0, read)) && read < 12)
                {
                    return ReadCountAfterHeader(stream, skipBytes: 8);
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int ReadCountAfterHeader(FileStream stream, int skipBytes)
        {
            if (stream.Length < skipBytes + 4)
            {
                return 0;
            }

            stream.Seek(skipBytes, SeekOrigin.Begin);
            byte[] countBytes = new byte[4];
            return stream.Read(countBytes, 0, 4) == 4 ? BitConverter.ToInt32(countBytes, 0) : 0;
        }

        /// <summary>
        /// Copies meta/audio off live <see cref="SpeechEvent"/> instances (call on the game thread).
        /// </summary>
        internal static List<SpeechEventCapturedRecord> CaptureRecords(IReadOnlyList<SpeechEvent> speechEvents)
        {
            List<SpeechEventCapturedRecord> records = [];
            if (speechEvents.Count == 0)
            {
                return records;
            }

            foreach (SpeechEvent ev in speechEvents)
            {
                if (ev == null)
                {
                    continue;
                }

                byte[]? metaData = ReplayableSndEvent.GetDataFromSndEvent(ev);
                if (metaData == null || metaData.Length == 0)
                {
                    continue;
                }

                byte[] audioData = ev.CompressedAudioData ?? [];
                records.Add(new SpeechEventCapturedRecord
                {
                    Meta = (byte[])metaData.Clone(),
                    Audio = audioData.Length == 0 ? [] : (byte[])audioData.Clone(),
                });
            }

            return records;
        }

        /// <summary>
        /// Packs already-captured records into a v3 speech sidecar payload (safe off the game thread).
        /// </summary>
        internal static SpeechEventSaveSnapshot BuildSnapshot(
            string speechPath,
            IReadOnlyList<SpeechEventCapturedRecord> records)
        {
            byte[]? speechBytes = null;
            int serializedCount = 0;

            if (records.Count > 0 && !string.IsNullOrEmpty(speechPath))
            {
                using MemoryStream ms = new();
                using BinaryWriter bw = new(ms);

                bw.Write(FileMagic);
                bw.Write(FileFormatVersion);
                bw.Write(records.Count);

                foreach (SpeechEventCapturedRecord record in records)
                {
                    byte[] metaData = record.Meta ?? [];
                    byte[] audioData = record.Audio ?? [];
                    bw.Write(metaData.Length);
                    bw.Write(metaData);
                    bw.Write(audioData.Length);
                    bw.Write(audioData);
                }

                serializedCount = records.Count;
                speechBytes = ms.ToArray();
            }

            return new SpeechEventSaveSnapshot
            {
                SpeechPath = speechPath ?? string.Empty,
                SpeechBytes = speechBytes,
                SerializedCount = serializedCount,
            };
        }

        internal static SpeechEventSaveSnapshot Serialize(int slotId, List<SpeechEvent> speechEvents)
        {
            string speechPath = SaveSidecarPaths.GetSpeechPath(slotId) ?? string.Empty;
            return BuildSnapshot(speechPath, CaptureRecords(speechEvents));
        }

        internal static List<SpeechEvent>? Load(int slotId)
        {
            if (!HasSpeechEventsFile(slotId))
            {
                return null;
            }

            string? filePath = SaveSidecarPaths.GetSpeechPath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            try
            {
                byte[]? data = AtomicFileIO.ReadBytes(filePath, Feature);
                if (data == null || data.Length < 4)
                {
                    return null;
                }

                if (SpeechEventFileHeader.HasMagicHeader(data))
                {
                    return LoadV3(data, slotId);
                }

                return LoadLegacy(data, slotId);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"LoadSpeechEvents: {ex.Message}");
                return null;
            }
        }

        private static List<SpeechEvent>? LoadV3(byte[] data, int slotId)
        {
            List<SpeechEvent> list = [];
            using MemoryStream ms = new(data);
            using BinaryReader br = new(ms);

            _ = br.ReadBytes(FileMagic.Length);
            _ = br.ReadInt32();
            int count = br.ReadInt32();
            if (count is <= 0 or > 100000)
            {
                return null;
            }

            long totalAudioBytes = ReadEventRecords(br, data, ms, count, list);
            ModLog.Debug(Feature, $"Loaded {list.Count}/{count} SpeechEvents (v3) from slot {slotId}, audio={totalAudioBytes / 1024}KB");
            return list.Count > 0 ? list : null;
        }

        private static List<SpeechEvent>? LoadLegacy(byte[] data, int slotId)
        {
            List<SpeechEvent> list = [];
            using MemoryStream ms = new(data);
            using BinaryReader br = new(ms);

            int count = br.ReadInt32();
            if (count is <= 0 or > 100000)
            {
                return LoadV1MemoryPack(data);
            }

            long totalAudioBytes = ReadEventRecords(br, data, ms, count, list);
            ModLog.Debug(Feature, $"Loaded {list.Count}/{count} SpeechEvents (legacy v2) from slot {slotId}, audio={totalAudioBytes / 1024}KB");
            return list.Count > 0 ? list : null;
        }

        private static long ReadEventRecords(
            BinaryReader br,
            byte[] data,
            MemoryStream ms,
            int count,
            List<SpeechEvent> list)
        {
            long totalAudioBytes = 0;
            for (int i = 0; i < count; i++)
            {
                if (ms.Position >= data.Length)
                {
                    break;
                }

                int metaLen = br.ReadInt32();
                if (metaLen <= 0 || ms.Position + metaLen > data.Length)
                {
                    continue;
                }

                byte[] metaData = br.ReadBytes(metaLen);

                byte[]? audioData = null;
                if (ms.Position + 4 <= data.Length)
                {
                    int audioLen = br.ReadInt32();
                    if (audioLen >= 0 && ms.Position + audioLen <= data.Length)
                    {
                        audioData = audioLen > 0 ? br.ReadBytes(audioLen) : null;
                        totalAudioBytes += audioLen;
                    }
                    else
                    {
                        ms.Position -= 4;
                    }
                }

                SpeechEvent? ev = DeserializeSingleSpeechEvent(metaData, audioData);
                if (ev != null)
                {
                    list.Add(ev);
                }
            }

            return totalAudioBytes;
        }

        private static List<SpeechEvent>? LoadV1MemoryPack(byte[] data)
        {
            try
            {
                List<SpeechEvent>? list = SerializerUtil.Deserialize<List<SpeechEvent>>(data);
                if (list != null && list.Count > 0)
                {
                    ModLog.Debug(Feature, $"Loaded {list.Count} SpeechEvents (legacy v1 MemoryPack)");
                    return list;
                }
            }
            catch
            {
                /* fall through */
            }

            return null;
        }

        private static SpeechEvent? DeserializeSingleSpeechEvent(byte[] metaData, byte[]? audioData = null)
        {
            if (metaData == null || metaData.Length == 0)
            {
                return null;
            }

            try
            {
                ReplayableSndEvent wrapper = new(SndEventType.PLAYER, 0, 0, 0, metaData, null);
                SpeechEvent? ev = wrapper.GetSndEvent(REPLAY_HEADER_VERSION.V_1_2);

                if (ev != null && audioData != null && audioData.Length > 0 && CompressedAudioDataField != null)
                {
                    CompressedAudioDataField.SetValue(ev, audioData);
                }

                return ev;
            }
            catch
            {
                return null;
            }
        }
    }
}
