using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MimesisPlayerEnhancement.Util;
using Mimic.Voice.SpeechSystem;
using ReluReplay.Data;
using ReluReplay.Serializer;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class SpeechEventFileStore
    {
        private const string Feature = "Persistence";
        private const int MetadataVersion = 3;

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

        internal static int TryGetSavedSpeechEventCount(int slotId)
        {
            int fromMetadata = TryReadSpeechCountFromMetadata(slotId);
            if (fromMetadata > 0)
            {
                return fromMetadata;
            }

            return TryReadSpeechCountFromBinary(slotId);
        }

        private static int TryReadSpeechCountFromMetadata(int slotId)
        {
            string? metaPath = SaveSidecarPaths.GetSpeechMetadataPath(slotId);
            if (string.IsNullOrEmpty(metaPath))
            {
                return 0;
            }

            string? json = AtomicFileIO.ReadText(metaPath, Feature);
            if (string.IsNullOrEmpty(json))
            {
                return 0;
            }

            const string marker = "\"speechCount\":";
            int markerIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return 0;
            }

            int start = markerIndex + marker.Length;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            {
                end++;
            }

            return end > start && int.TryParse(json.Substring(start, end - start), out int count) && count > 0
                ? count
                : 0;
        }

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

                if (HasMagicHeader(header))
                {
                    if (read >= 12)
                    {
                        int count = BitConverter.ToInt32(header, 8);
                        return count > 0 ? count : 0;
                    }

                    return ReadCountAfterHeader(stream, skipBytes: 8);
                }

                int legacyCount = BitConverter.ToInt32(header, 0);
                return legacyCount > 0 ? legacyCount : 0;
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

        internal static SpeechEventSaveSnapshot Serialize(int slotId, List<SpeechEvent> speechEvents)
        {
            string? speechPath = SaveSidecarPaths.GetSpeechPath(slotId);
            string? metadataPath = SaveSidecarPaths.GetSpeechMetadataPath(slotId);
            byte[]? speechBytes = null;
            int serializedCount = 0;
            long totalAudioBytes = 0;

            if (speechEvents.Count > 0 && !string.IsNullOrEmpty(speechPath))
            {
                using MemoryStream ms = new();
                using BinaryWriter bw = new(ms);

                bw.Write(FileMagic);
                bw.Write(FileFormatVersion);

                List<(byte[] meta, byte[] audio)> serializedEvents = [];
                foreach (SpeechEvent ev in speechEvents)
                {
                    byte[] metaData = ReplayableSndEvent.GetDataFromSndEvent(ev);
                    if (metaData == null || metaData.Length == 0)
                    {
                        continue;
                    }

                    byte[] audioData = ev.CompressedAudioData ?? [];
                    serializedEvents.Add((metaData, audioData));
                }

                serializedCount = serializedEvents.Count;
                bw.Write(serializedCount);

                foreach ((byte[] metaData, byte[] audioData) in serializedEvents)
                {
                    bw.Write(metaData.Length);
                    bw.Write(metaData);
                    bw.Write(audioData.Length);
                    bw.Write(audioData);
                    totalAudioBytes += audioData.Length;
                }

                speechBytes = ms.ToArray();
                ModLog.Debug(Feature, $"Serialized {serializedCount} SpeechEvents, audio={totalAudioBytes / 1024}KB");
            }

            return new SpeechEventSaveSnapshot
            {
                SpeechPath = speechPath ?? string.Empty,
                SpeechBytes = speechBytes,
                MetadataPath = metadataPath ?? string.Empty,
                MetadataJson = $"{{\"version\":{MetadataVersion},\"timestamp\":\"{DateTime.UtcNow:O}\",\"speechCount\":{serializedCount}}}",
                SerializedCount = serializedCount,
            };
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

                if (HasMagicHeader(data))
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

        private static bool HasMagicHeader(byte[] data)
        {
            return data.Length >= FileMagic.Length
                   && data[0] == FileMagic[0]
                   && data[1] == FileMagic[1]
                   && data[2] == FileMagic[2]
                   && data[3] == FileMagic[3];
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
