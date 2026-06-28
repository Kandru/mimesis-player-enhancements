using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ReluProtocol;
using ReluReplay.Data;
using ReluReplay.Serializer;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages persistence of Mimesis data per save slot. Only the host saves.
    /// Data stored under Save/{SteamID}/MimesisData/Slot{N}/ where N matches SaveSlotID
    /// (0 = autosave, 1..3 = manual save slots).
    /// </summary>
    public static class MimesisSaveManager
    {
        private const string MimesisDataFolder = "MimesisData";
        private const string SlotPrefix = "Slot";
        private const string SpeechEventsFile = "speech_events.bin";
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
            {
                File.Delete(filePath);
            }

            File.Move(tmpPath, filePath);
        }

        /// <summary>
        /// Write text to a file safely (same pattern as SafeWriteAllBytes).
        /// </summary>
        private static void SafeWriteAllText(string filePath, string text)
        {
            SafeWriteAllBytes(filePath, System.Text.Encoding.UTF8.GetBytes(text));
        }

        private static void SafeDeleteFile(string filePath)
        {
            foreach (string path in new[] { filePath, filePath + BackupSuffix, filePath + TempSuffix })
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    File.Delete(path);
                    ModLog.Debug("Persistence", $"Deleted stale file: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    ModLog.Warn("Persistence", $"Failed to delete {Path.GetFileName(path)}: {ex.Message}");
                }
            }
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
                    {
                        return data;
                    }
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
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
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
            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return null;
            }

            string baseFolder = platformMgr.GetSaveFileFolderPath();
            return string.IsNullOrEmpty(baseFolder) ? null : Path.Combine(baseFolder, MimesisDataFolder, SlotPrefix + slotId);
        }

        public static bool IsHost()
        {
            return HostStatusCache.IsHostFast();
        }

        public static List<SpeechEvent> CollectAllSpeechEvents()
        {
            List<SpeechEvent> list = [];
            try
            {
                HashSet<long> seenIds = [];

                // 1. Collect from live archives (players still connected)
                foreach (SpeechEventArchive arch in SpeechEventArchiveRegistry.EnumerateActive())
                {
                    _ = SpeechEventSyncListHelper.CollectFromArchive(arch, seenIds, list);
                }

                int liveCount = list.Count;

                // 2. Include cached events from disconnected players
                List<SpeechEvent> disconnected = SpeechEventPoolManager.GetDisconnectedEvents();
                int disconnectedAdded = 0;
                foreach (SpeechEvent ev in disconnected)
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
                List<SpeechEvent> pending = SpeechEventPoolManager.GetPendingEvents();
                int pendingAdded = 0;
                foreach (SpeechEvent ev in pending)
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

        public static int GetCurrentSaveSlotId()
        {
            try
            {
                object? vworld = GetHubMember("vworld");
                if (vworld == null)
                {
                    return -1;
                }

                PropertyInfo saveSlotProp = vworld.GetType().GetProperty("SaveSlotID", BindingFlags.Public | BindingFlags.Instance);
                return saveSlotProp == null ? -1 : (int)saveSlotProp.GetValue(vworld);
            }
            catch { return -1; }
        }

        public static bool IsValidSaveSlotId(int slotId)
        {
            return MMSaveGameData.CheckSaveSlotID(slotId, true);
        }

        public static bool TryGetActiveSaveSlotId(out int slotId)
        {
            slotId = GetCurrentSaveSlotId();
            return IsHost() && IsValidSaveSlotId(slotId);
        }

        public static object? GetHubMember(string name)
        {
            if (Hub.s == null)
            {
                return null;
            }

            Type type = typeof(Hub);
            FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                return field.GetValue(Hub.s);
            }

            PropertyInfo prop = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            return prop != null && prop.CanRead ? prop.GetValue(Hub.s) : null;
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
                _ = Directory.CreateDirectory(slotPath);

                List<SpeechEvent> speechEvents = CollectAllSpeechEvents();
                string speechPath = Path.Combine(slotPath, SpeechEventsFile);
                int serializedCount = 0;
                if (speechEvents != null && speechEvents.Count > 0)
                {
                    using (MemoryStream ms = new())
                    using (BinaryWriter bw = new(ms))
                    {
                        List<(byte[] meta, byte[] audio)> serializedEvents = [];
                        foreach (SpeechEvent ev in speechEvents)
                        {
                            byte[] metaData = ReplayableSndEvent.GetDataFromSndEvent(ev);
                            if (metaData != null && metaData.Length > 0)
                            {
                                byte[] audioData = ev.CompressedAudioData ?? [];
                                serializedEvents.Add((metaData, audioData));
                            }
                        }

                        serializedCount = serializedEvents.Count;
                        bw.Write(serializedCount);

                        long totalAudioBytes = 0;
                        foreach ((byte[]? metaData, byte[]? audioData) in serializedEvents)
                        {
                            bw.Write(metaData.Length);
                            bw.Write(metaData);
                            bw.Write(audioData.Length);
                            bw.Write(audioData);
                            totalAudioBytes += audioData.Length;
                        }

                        SafeWriteAllBytes(speechPath, ms.ToArray());
                        ModLog.Debug("Persistence", $"Serialized {serializedCount} SpeechEvents, audio={totalAudioBytes / 1024}KB");
                    }
                }
                else
                {
                    SafeDeleteFile(speechPath);
                }

                // Save player mapping (SteamID -> DissonanceID) for cross-session matching
                SpeechEventPoolManager.SavePlayerMapping(slotId);

                SafeWriteAllText(Path.Combine(slotPath, MetadataFile),
                    $"{{\"version\":{MetadataVersion},\"timestamp\":\"{DateTime.UtcNow:O}\",\"speechCount\":{serializedCount}}}");

                ModLog.Info("Persistence", $"Saved slot {slotId} — speechEvents={serializedCount}");
            }
            catch (Exception ex)
            {
                ModLog.Error("Persistence", $"SaveMimesisData: {ex}");
            }
        }

        public static List<SpeechEvent>? LoadSpeechEvents(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath))
            {
                return null;
            }

            string filePath = Path.Combine(slotPath, SpeechEventsFile);
            if (!File.Exists(filePath) && !File.Exists(filePath + BackupSuffix))
            {
                return null;
            }

            try
            {
                byte[]? data = SafeReadAllBytes(filePath);
                if (data == null || data.Length < 4)
                {
                    return null;
                }

                List<SpeechEvent> list = [];
                int count = 0;
                using (MemoryStream ms = new(data))
                using (BinaryReader br = new(ms))
                {
                    count = br.ReadInt32();
                    if (count is <= 0 or > 100000)
                    {
                        return LoadSpeechEventsOldFormat(data);
                    }

                    // Try to detect format: v2 has audio after metadata, v1 only has metadata
                    // We'll try v2 first, fall back to v1 if it fails
                    long totalAudioBytes = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (ms.Position >= data.Length)
                        {
                            break;
                        }

                        // Read metadata
                        int metaLen = br.ReadInt32();
                        if (metaLen <= 0 || ms.Position + metaLen > data.Length)
                        {
                            continue;
                        }

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

                        SpeechEvent? ev = DeserializeSingleSpeechEvent(metaData, audioData);
                        if (ev != null)
                        {
                            list.Add(ev);
                        }
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
                if (serializerType == null)
                {
                    return null;
                }

                MethodInfo deserializeMethod = serializerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "Deserialize")
                        {
                            return false;
                        }

                        ParameterInfo[] p = m.GetParameters();
                        return p.Length >= 2 && p[0].ParameterType == typeof(Type) && p[1].ParameterType == typeof(byte[]);
                    });
                if (deserializeMethod == null)
                {
                    return null;
                }

                List<SpeechEvent>? list = (List<SpeechEvent>?)deserializeMethod.Invoke(null, [typeof(List<SpeechEvent>), data, null]);
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
            if (metaData == null || metaData.Length == 0)
            {
                return null;
            }

            try
            {
                // Deserialize the metadata (everything except CompressedAudioData)
                ReplayableSndEvent wrapper = new(SndEventType.PLAYER, 0, 0, 0, metaData, null);
                SpeechEvent? ev = wrapper.GetSndEvent(REPLAY_HEADER_VERSION.V_1_2);

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
            Type t = Type.GetType("MemoryPack.MemoryPackSerializer, MemoryPack");
            if (t != null)
            {
                return t;
            }

            t = Type.GetType("MemoryPack.MemoryPackSerializer, MemoryPack.Core");
            if (t != null)
            {
                return t;
            }

            t = Type.GetType("MemoryPack.MemoryPackSerializer, MemoryPack.Runtime");
            if (t != null)
            {
                return t;
            }

            foreach (Assembly? asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType("MemoryPack.MemoryPackSerializer");
                    if (t != null)
                    {
                        return t;
                    }
                }
                catch { }
            }
            return null;
        }


        public static bool HasMimesisData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath))
            {
                return false;
            }
            // Check both main files and their backups
            return File.Exists(Path.Combine(slotPath, SpeechEventsFile)) ||
                   File.Exists(Path.Combine(slotPath, SpeechEventsFile + BackupSuffix));
        }

        public static void DeleteMimesisData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath) || !Directory.Exists(slotPath))
            {
                return;
            }

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
