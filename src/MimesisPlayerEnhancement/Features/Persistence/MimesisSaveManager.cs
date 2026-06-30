using System;
using System.Collections.Generic;
using System.IO;
using ReluProtocol;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages persistence of Mimesis data per save slot. Only the host saves.
    /// Data stored under Save/{SteamID}/MimesisData/Slot{N}/ where N is the campaign slot
    /// (1..99 manual slots when extended save slots are enabled; 0 for autosave sidecar data).
    /// </summary>
    public static class MimesisSaveManager
    {
        private const string Feature = "Persistence";
        private const string MimesisDataFolder = "MimesisData";
        private const string SlotPrefix = "Slot";

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

                foreach (SpeechEventArchive arch in SpeechEventArchiveRegistry.EnumerateActive())
                {
                    _ = SpeechEventInjector.CollectFromArchive(arch, seenIds, list);
                }

                int liveCount = list.Count;

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
                    ModLog.Debug(Feature, $"CollectAllSpeechEvents: " +
                        $"{liveCount} live + {disconnectedAdded} disconnected + {pendingAdded} pending (absent players) = {list.Count} total");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"CollectAllSpeechEvents: {ex.Message}");
            }

            return list;
        }

        public static int GetCurrentSaveSlotId()
        {
            return GameSessionAccess.GetSaveSlotId();
        }

        public static bool IsValidSaveSlotId(int slotId)
        {
            return GameSessionAccess.IsValidSaveSlotId(slotId);
        }

        public static bool TryGetActiveSaveSlotId(out int slotId)
        {
            return GameSessionAccess.TryGetActiveSaveSlotId(out slotId);
        }

        public static void SaveMimesisData(int slotId)
        {
            if (!IsHost())
            {
                ModLog.Debug(Feature, "Skip save: not host.");
                return;
            }

            string? slotPath = GetMimesisSlotPath(slotId);
            if (string.IsNullOrEmpty(slotPath))
            {
                ModLog.Warn(Feature, "Skip save: slot path unavailable.");
                return;
            }

            try
            {
                _ = Directory.CreateDirectory(slotPath);

                List<SpeechEvent> speechEvents = CollectAllSpeechEvents();
                SpeechEventSaveSnapshot snapshot = SpeechEventFileStore.Serialize(slotPath, speechEvents);
                if (!SpeechEventPoolManager.TryBuildPlayerMappingJson(slotId, out string playerMappingPath, out string playerMappingJson))
                {
                    ModLog.Warn(Feature, "Skip save: player mapping path unavailable.");
                    return;
                }

                PersistenceWriteQueue.EnqueueSave(slotId, snapshot, playerMappingPath, playerMappingJson);
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"SaveMimesisData: {ex}");
            }
        }

        public static List<SpeechEvent>? LoadSpeechEvents(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            return string.IsNullOrEmpty(slotPath) ? null : SpeechEventFileStore.Load(slotId, slotPath);
        }

        public static bool HasMimesisData(int slotId)
        {
            string? slotPath = GetMimesisSlotPath(slotId);
            return !string.IsNullOrEmpty(slotPath) && SpeechEventFileStore.HasSpeechEventsFile(slotPath);
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
                ModLog.Warn(Feature, $"DeleteMimesisData: {ex.Message}");
            }
        }
    }
}
