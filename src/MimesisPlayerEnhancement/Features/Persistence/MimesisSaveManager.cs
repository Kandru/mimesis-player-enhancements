using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ReluProtocol;
using ReluReplay.Data;
using ReluReplay.Serializer;
using Mimic.Voice.SpeechSystem;
using ReluReplay;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages persistence of Mimesis data per save slot. Only the host saves.
    /// Data stored under Save/{SteamID}/MimesisData/Slot{N}/.
    /// </summary>
    public static class MimesisSaveManager
    {
        private const string MimesisDataFolder = "MimesisData";
        private const string SlotPrefix = "Slot";
        private const string SpeechEventsFile = "speech_events.bin";
        private const string ReplayPlayFile = "replay_play.bin";
        private const string ReplayVoiceFile = "replay_voice.bin";
        private const string MetadataFile = "metadata.json";
        private const int MetadataVersion = 2; // v2: includes CompressedAudioData
        private const string BackupSuffix = ".bak";
        private const string TempSuffix = ".tmp";
        
        // Field info for setting CompressedAudioData via reflection (it's readonly)
        private static readonly FieldInfo CompressedAudioDataField = 
            typeof(SpeechEvent).GetField("CompressedAudioData", BindingFlags.Public | BindingFlags.Instance);

        // ===================== Safe File I/O =====================

        /// <summary>
        /// Write bytes to a file safely: write to .tmp first, then replace the original.
        /// This prevents corrupted files from half-finished writes (crash, power loss).
        /// Also creates a .bak of the previous version before replacing.
        /// </summary>
        private static void SafeWriteAllBytes(string filePath, byte[] data)
        {
            string tmpPath = filePath + TempSuffix;
            string bakPath = filePath + BackupSuffix;

            // 1. Write to temporary file
            File.WriteAllBytes(tmpPath, data);

            // 2. Backup current file (if exists)
            if (File.Exists(filePath))
            {
                try { File.Copy(filePath, bakPath, true); }
                catch (Exception ex)
                {
                    ModLog.Warn("Persistence", $"Backup failed for {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            // 3. Replace original with temp (atomic on most filesystems)
            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tmpPath, filePath);
        }

        /// <summary>
        /// Write text to a file safely (same pattern as SafeWriteAllBytes).
        /// </summary>
        private static void SafeWriteAllText(string filePath, string text)
        {
            SafeWriteAllBytes(filePath, System.Text.Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Read bytes from a file, falling back to .bak if the main file is missing or corrupt.
        /// Returns null if neither file is usable.
        /// </summary>
        private static byte[]? SafeReadAllBytes(string filePath)
        {
            // Try main file first
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(filePath);
                    if (data != null && data.Length > 0)
                        return data;
                }
                catch (Exception ex)
                {
                    ModLog.Warn("Persistence", $"Main file read failed ({Path.GetFileName(filePath)}): {ex.Message}");
                }
            }

            // Fallback to .bak
            string bakPath = filePath + BackupSuffix;
            if (File.Exists(bakPath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(bakPath);
                    if (data != null && data.Length > 0)
                    {
                        ModLog.Warn("Persistence", $"Recovered from backup: {Path.GetFileName(bakPath)}");
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Error("Persistence", $"Backup also failed ({Path.GetFileName(bakPath)}): {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Safe write for player mapping JSON. Used by SpeechEventPoolManager.
        /// </summary>
        public static void SafeWritePlayerMapping(string filePath, string json)
        {
            SafeWriteAllText(filePath, json);
        }

        /// <summary>
        /// Safe read for player mapping JSON with .bak fallback. Used by SpeechEventPoolManager.
        /// Returns null if neither file is usable.
        /// </summary>
        public static string? SafeReadPlayerMapping(string filePath)
        {
            // Try main file
            if (File.Exists(filePath))
            {
                try
                {
                    string text = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(text)) return text;
                }
                catch (Exception ex)
                {
                    ModLog.Warn("Persistence", $"Player mapping read failed: {ex.Message}");
                }
            }

            // Fallback to .bak
            string bakPath = filePath + BackupSuffix;
            if (File.Exists(bakPath))
            {
                try
                {
                    string text = File.ReadAllText(bakPath);
                    if (!string.IsNullOrEmpty(text))
                    {
                        ModLog.Warn("Persistence", $"Recovered player mapping from backup");
                        return text;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Error("Persistence", $"Player mapping backup also failed: {ex.Message}");
                }
            }

            return null;
        }

        public static string? GetMimesisSlotPath(int slotId)
        {
            var platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null) return null;
            string baseFolder = platformMgr.GetSaveFileFolderPath();
            if (string.IsNullOrEmpty(baseFolder)) return null;
            return Path.Combine(baseFolder, MimesisDataFolder, SlotPrefix + slotId);
        }

        public static bool IsHost()
        {
            return IsHostViaNetwork() || IsHostViaVWorld();
        }

        private static bool IsHostViaNetwork()
        {
            try
            {
                var t = Type.GetType("FishNet.Managing.NetworkManager, FishNet.Runtime");
                if (t == null) return false;
                var instanceProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null) return false;
                object nm = instanceProp.GetValue(null);
                if (nm == null) return false;
                var isServerProp = t.GetProperty("IsServer", BindingFlags.Public | BindingFlags.Instance);
                return isServerProp != null && (bool)isServerProp.GetValue(nm);
            }
            catch { return false; }
        }

        private static bool IsHostViaVWorld()
        {
            try
            {
                object? vworld = GetHubMember("vworld");
                if (vworld == null) return false;
                object? sessionMgr = vworld.GetType().GetField("_sessionManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(vworld);
                if (sessionMgr == null) return false;
                var hostField = sessionMgr.GetType().GetField("_hostSessionContext", BindingFlags.NonPublic | BindingFlags.Instance);
                if (hostField == null) return false;
                object? hostCtx = hostField.GetValue(sessionMgr);
                if (hostCtx == null) return false;
                var vplayerField = hostCtx.GetType().GetField("_vPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
                object? vplayer = vplayerField?.GetValue(hostCtx);
                if (vplayer == null) return false;
                var isHostProp = vplayer.GetType().GetProperty("IsHost", BindingFlags.Public | BindingFlags.Instance);
                return isHostProp != null && (bool)isHostProp.GetValue(vplayer);
            }
            catch { return false; }
        }

        public static List<SpeechEvent> CollectAllSpeechEvents()
        {
            var list = new List<SpeechEvent>();
            try
            {
                var seenIds = new HashSet<long>();

                // 1. Collect from live archives (players still connected)
                var archives = UnityEngine.Object.FindObjectsByType<SpeechEventArchive>(FindObjectsSortMode.None);
                if (archives != null)
                {
                    var eventsField = typeof(SpeechEventArchive).GetField("events", BindingFlags.Public | BindingFlags.Instance);
                    if (eventsField != null)
                    {
                        foreach (var arch in archives)
                        {
                            var syncList = eventsField.GetValue(arch);
                            if (syncList == null) continue;
                            var countProp = syncList.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                            var indexer = syncList.GetType().GetProperty("Item", new[] { typeof(int) });
                            if (countProp == null || indexer == null) continue;
                            int count = (int)countProp.GetValue(syncList);
                            for (int i = 0; i < count; i++)
                            {
                                var ev = indexer.GetValue(syncList, new object[] { i }) as SpeechEvent;
                                if (ev != null && seenIds.Add(ev.Id))
                                    list.Add(ev);
                            }
                        }
                    }
                }

                int liveCount = list.Count;

                // 2. Include cached events from disconnected players
                var disconnected = SpeechEventPoolManager.GetDisconnectedEvents();
                int disconnectedAdded = 0;
                foreach (var ev in disconnected)
                {
                    if (ev != null && seenIds.Add(ev.Id))
                    {
                        list.Add(ev);
                        disconnectedAdded++;
                    }
                }

                // 3. Include PENDING events from the pool (loaded from disk but never matched).
                // These belong to players who didn't join this session.
                // Without this, their voices would be lost after one session without them.
                var pending = SpeechEventPoolManager.GetPendingEvents();
                int pendingAdded = 0;
                foreach (var ev in pending)
                {
                    if (ev != null && seenIds.Add(ev.Id))
                    {
                        list.Add(ev);
                        pendingAdded++;
                    }
                }

                if (disconnectedAdded > 0 || pendingAdded > 0)
                {
                    ModLog.Debug("Persistence", $"CollectAllSpeechEvents: " +
                        $"{liveCount} live + {disconnectedAdded} disconnected + {pendingAdded} pending (absent players) = {list.Count} total");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"CollectAllSpeechEvents: {ex.Message}");
            }
            return list;
        }

        private static object? GetReplayData()
        {
            try
            {
                object? replayManager = GetHubMember("replayManager");
                if (replayManager == null) return null;
                var recorderField = typeof(ReplayManager).GetField("_recorder", BindingFlags.NonPublic | BindingFlags.Instance);
                if (recorderField == null) return null;
                object recorder = recorderField.GetValue(replayManager);
                if (recorder == null) return null;
                var replayDataField = recorder.GetType().GetField("_replayData", BindingFlags.NonPublic | BindingFlags.Instance);
                return replayDataField?.GetValue(recorder);
            }
            catch { return null; }
        }

        public static int GetCurrentSaveSlotId()
        {
            try
            {
                object? vworld = GetHubMember("vworld");
                if (vworld == null) return -1;
                var saveSlotProp = vworld.GetType().GetProperty("SaveSlotID", BindingFlags.Public | BindingFlags.Instance);
                if (saveSlotProp == null) return -1;
                return (int)saveSlotProp.GetValue(vworld);
            }
            catch { return -1; }
        }

        public static object? GetHubMember(string name)
        {
            if (Hub.s == null) return null;
            var type = typeof(Hub);
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
                return field.GetValue(Hub.s);

            var prop = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.CanRead)
                return prop.GetValue(Hub.s);

            return null;
        }

        private static List<MsgWithTime>? GetPlayDataFromReplayData(object? replayData)
        {
            if (replayData == null) return null;
            try
            {
                var field = replayData.GetType().GetField("_playData", BindingFlags.NonPublic | BindingFlags.Instance);
                return field?.GetValue(replayData) as List<MsgWithTime>;
            }
            catch { return null; }
        }

        private static List<SndWithTime>? GetVoiceDataFromReplayData(object? replayData)
        {
            if (replayData == null) return null;
            try
            {
                var field = replayData.GetType().GetField("_voiceData", BindingFlags.NonPublic | BindingFlags.Instance);
                return field?.GetValue(replayData) as List<SndWithTime>;
            }
            catch { return null; }
        }

        public static void SaveMimesisData(int slotId)
        {
            if (!IsHost())
            {
                ModLog.Debug("Persistence", "Skip save: not host.");
                return;
            }
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath))
            {
                ModLog.Warn("Persistence", "Skip save: slot path unavailable.");
                return;
            }
            try
            {
                Directory.CreateDirectory(slotPath);

                List<SpeechEvent> speechEvents = CollectAllSpeechEvents();
                if (speechEvents != null && speechEvents.Count > 0)
                {
                    // Serialize events with audio data (v2 format)
                    // Format: [validCount:int32] then for each: [metaLen:int32][metaData:bytes][audioLen:int32][audioData:bytes]
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        // First pass: serialize and count valid events
                        var serializedEvents = new List<(byte[] meta, byte[] audio)>();
                        foreach (var ev in speechEvents)
                        {
                            byte[] metaData = ReplayableSndEvent.GetDataFromSndEvent(ev);
                            if (metaData != null && metaData.Length > 0)
                            {
                                byte[] audioData = ev.CompressedAudioData ?? Array.Empty<byte>();
                                serializedEvents.Add((metaData, audioData));
                            }
                        }
                        
                        // Write count of valid events (fixes the count mismatch bug)
                        bw.Write(serializedEvents.Count);
                        
                        // Write each event: metadata + audio
                        long totalAudioBytes = 0;
                        foreach (var (metaData, audioData) in serializedEvents)
                        {
                            bw.Write(metaData.Length);
                            bw.Write(metaData);
                            bw.Write(audioData.Length);
                            bw.Write(audioData);
                            totalAudioBytes += audioData.Length;
                        }
                        
                        SafeWriteAllBytes(Path.Combine(slotPath, SpeechEventsFile), ms.ToArray());
                        ModLog.Debug("Persistence", $"Serialized {serializedEvents.Count} SpeechEvents, audio={totalAudioBytes / 1024}KB");
                    }
                }

                object? replayData = GetReplayData();
                List<MsgWithTime>? playData = GetPlayDataFromReplayData(replayData);
                if (playData != null && playData.Count > 0)
                {
                    string playPath = Path.Combine(slotPath, ReplayPlayFile);
                    ReplaySerializer.SerializeMsgToFile(in playData, in playPath);
                }

                List<SndWithTime>? voiceData = GetVoiceDataFromReplayData(replayData);
                if (voiceData != null && voiceData.Count > 0)
                {
                    string voicePath = Path.Combine(slotPath, ReplayVoiceFile);
                    ReplaySerializer.SerializeSndToFile(in voiceData, in voicePath);
                }

                // Save player mapping (SteamID -> DissonanceID) for cross-session matching
                SpeechEventPoolManager.SavePlayerMapping(slotId);

                int speechCount = speechEvents?.Count ?? 0;
                int playCount = playData?.Count ?? 0;
                int voiceCount = voiceData?.Count ?? 0;
                SafeWriteAllText(Path.Combine(slotPath, MetadataFile),
                    $"{{\"version\":{MetadataVersion},\"timestamp\":\"{DateTime.UtcNow:O}\",\"speechCount\":{speechCount},\"playCount\":{playCount},\"voiceCount\":{voiceCount}}}");

                ModLog.Info("Persistence", $"Saved slot {slotId}. Speech={speechCount}, Play={playCount}, Voice={voiceCount}");
            }
            catch (Exception ex)
            {
                ModLog.Error("Persistence", $"SaveMimesisData: {ex}");
            }
        }

        public static List<SpeechEvent>? LoadSpeechEvents(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath)) return null;
            string filePath = Path.Combine(slotPath, SpeechEventsFile);
            if (!File.Exists(filePath) && !File.Exists(filePath + BackupSuffix)) return null;
            try
            {
                byte[]? data = SafeReadAllBytes(filePath);
                if (data == null || data.Length < 4) return null;

                var list = new List<SpeechEvent>();
                int count = 0;
                using (var ms = new MemoryStream(data))
                using (var br = new BinaryReader(ms))
                {
                    count = br.ReadInt32();
                    if (count <= 0 || count > 100000)
                        return LoadSpeechEventsOldFormat(data);
                    
                    // Try to detect format: v2 has audio after metadata, v1 only has metadata
                    // We'll try v2 first, fall back to v1 if it fails
                    long totalAudioBytes = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (ms.Position >= data.Length) break;
                        
                        // Read metadata
                        int metaLen = br.ReadInt32();
                        if (metaLen <= 0 || ms.Position + metaLen > data.Length) continue;
                        byte[] metaData = br.ReadBytes(metaLen);
                        
                        // Try to read audio (v2 format)
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
                                // Not v2 format, rewind and try v1
                                ms.Position -= 4;
                            }
                        }
                        
                        var ev = DeserializeSingleSpeechEvent(metaData, audioData);
                        if (ev != null) list.Add(ev);
                    }
                    
                    ModLog.Debug("Persistence", $"Loaded {list.Count}/{count} SpeechEvents from slot {slotId}, audio={totalAudioBytes / 1024}KB");
                }
                return list.Count > 0 ? list : null;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"LoadSpeechEvents: {ex.Message}");
                return null;
            }
        }

        private static List<SpeechEvent>? LoadSpeechEventsOldFormat(byte[] data)
        {
            try
            {
                Type? serializerType = FindMemoryPackSerializerType();
                if (serializerType == null) return null;

                MethodInfo deserializeMethod = serializerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "Deserialize") return false;
                        var p = m.GetParameters();
                        return p.Length >= 2 && p[0].ParameterType == typeof(Type) && p[1].ParameterType == typeof(byte[]);
                    });
                if (deserializeMethod == null) return null;

                var list = (List<SpeechEvent>?)deserializeMethod.Invoke(null, new object?[] { typeof(List<SpeechEvent>), data, null });
                if (list != null && list.Count > 0)
                {
                    ModLog.Debug("Persistence", $"Loaded {list.Count} SpeechEvents (legacy format)");
                    return list;
                }
            }
            catch { }
            return null;
        }

        private static SpeechEvent? DeserializeSingleSpeechEvent(byte[] metaData, byte[]? audioData = null)
        {
            if (metaData == null || metaData.Length == 0) return null;
            try
            {
                // Deserialize the metadata (everything except CompressedAudioData)
                var wrapper = new ReplayableSndEvent(SndEventType.PLAYER, 0, 0, 0, metaData, null);
                var ev = wrapper.GetSndEvent(REPLAY_HEADER_VERSION.V_1_2);
                
                // Inject the audio data via reflection (field is readonly)
                if (ev != null && audioData != null && audioData.Length > 0 && CompressedAudioDataField != null)
                {
                    CompressedAudioDataField.SetValue(ev, audioData);
                }
                
                return ev;
            }
            catch { return null; }
        }

        private static Type? FindMemoryPackSerializerType()
        {
            var t = Type.GetType("MemoryPack.MemoryPackSerializer, MemoryPack");
            if (t != null) return t;
            t = Type.GetType("MemoryPack.MemoryPackSerializer, MemoryPack.Core");
            if (t != null) return t;
            t = Type.GetType("MemoryPack.MemoryPackSerializer, MemoryPack.Runtime");
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType("MemoryPack.MemoryPackSerializer");
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        public static List<MsgWithTime>? LoadReplayPlayData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath)) return null;
            string filePath = Path.Combine(slotPath, ReplayPlayFile);
            if (!File.Exists(filePath)) return null;
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var task = ReplaySerializer.DeserializeMsgFromFileAsync(fs);
                    task.GetAwaiter().GetResult();
                    return task.GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"LoadReplayPlayData: {ex.Message}");
                return null;
            }
        }

        public static List<SndWithTime>? LoadReplayVoiceData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath)) return null;
            string filePath = Path.Combine(slotPath, ReplayVoiceFile);
            if (!File.Exists(filePath)) return null;
            try
            {
                var task = ReplaySerializer.DeserializeSndFromFileAsync(filePath);
                task.GetAwaiter().GetResult();
                return task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"LoadReplayVoiceData: {ex.Message}");
                return null;
            }
        }

        public static bool HasMimesisData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath)) return false;
            // Check both main files and their backups
            return File.Exists(Path.Combine(slotPath, SpeechEventsFile)) ||
                   File.Exists(Path.Combine(slotPath, SpeechEventsFile + BackupSuffix)) ||
                   File.Exists(Path.Combine(slotPath, ReplayPlayFile)) ||
                   File.Exists(Path.Combine(slotPath, ReplayVoiceFile));
        }

        public static void DeleteMimesisData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath) || !Directory.Exists(slotPath)) return;
            try
            {
                Directory.Delete(slotPath, true);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Persistence", $"DeleteMimesisData: {ex.Message}");
            }
        }
    }
}
