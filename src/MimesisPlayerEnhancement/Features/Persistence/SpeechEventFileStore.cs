using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Mimic.Voice.SpeechSystem;
using ReluReplay.Data;
using ReluReplay.Serializer;
using ReluReplay.Shared;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class SpeechEventFileStore
    {
        private const string Feature = "Persistence";
        private const string SpeechEventsFile = "speech_events.bin";
        private const string MetadataFile = "metadata.json";
        private const int MetadataVersion = 3;

        private static readonly byte[] FileMagic = Encoding.ASCII.GetBytes("MPEV");
        private const int FileFormatVersion = 3;

        private static readonly FieldInfo? CompressedAudioDataField =
            typeof(SpeechEvent).GetField("CompressedAudioData", BindingFlags.Public | BindingFlags.Instance);

        internal static string GetSpeechEventsPath(string slotPath)
        {
            return Path.Combine(slotPath, SpeechEventsFile);
        }

        internal static bool HasSpeechEventsFile(string slotPath)
        {
            string filePath = GetSpeechEventsPath(slotPath);
            return File.Exists(filePath) || File.Exists(filePath + ".bak");
        }

        internal static SpeechEventSaveSnapshot Serialize(string slotPath, List<SpeechEvent> speechEvents)
        {
            string speechPath = GetSpeechEventsPath(slotPath);
            byte[]? speechBytes = null;
            int serializedCount = 0;
            long totalAudioBytes = 0;

            if (speechEvents.Count > 0)
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
                SpeechPath = speechPath,
                SpeechBytes = speechBytes,
                MetadataPath = Path.Combine(slotPath, MetadataFile),
                MetadataJson = $"{{\"version\":{MetadataVersion},\"timestamp\":\"{DateTime.UtcNow:O}\",\"speechCount\":{serializedCount}}}",
                SerializedCount = serializedCount,
            };
        }

        internal static List<SpeechEvent>? Load(int slotId, string slotPath)
        {
            string filePath = GetSpeechEventsPath(slotPath);
            if (!HasSpeechEventsFile(slotPath))
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
