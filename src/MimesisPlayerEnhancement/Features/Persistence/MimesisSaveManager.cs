using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;
using Mimic.Voice.SpeechSystem;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Manages persistence of Mimesis data per save slot. Only the host saves.
    /// Data stored as flat sidecar files in Save/{SteamID}/ (MMGameData{N}.mpe-*.sav)
    /// so Steam Auto-Cloud syncs them alongside vanilla saves.
    /// </summary>
    public static class MimesisSaveManager
    {
        private const string Feature = "Persistence";

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

            if (string.IsNullOrEmpty(SaveSidecarPaths.GetSpeechPath(slotId)))
            {
                ModLog.Warn(Feature, "Skip save: sidecar path unavailable.");
                return;
            }

            try
            {
                List<SpeechEvent> speechEvents = CollectAllSpeechEvents();
                if (!SpeechEventPoolManager.TryBuildPlayerMappingJson(slotId, out string playerMappingPath, out string playerMappingJson))
                {
                    ModLog.Warn(Feature, "Skip save: player mapping path unavailable.");
                    return;
                }

                PersistenceWriteQueue.EnqueueSave(slotId, speechEvents, playerMappingPath, playerMappingJson);
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"SaveMimesisData: {ex}");
            }
        }

        public static List<SpeechEvent>? LoadSpeechEvents(int slotId)
        {
            return SpeechEventFileStore.Load(slotId);
        }

        public static bool HasMimesisData(int slotId)
        {
            return SpeechEventFileStore.HasSpeechEventsFile(slotId);
        }

        public static void DeleteMimesisData(int slotId)
        {
            try
            {
                SaveSidecarPaths.DeleteSidecars(
                    slotId,
                    SidecarKind.Speech,
                    SidecarKind.SpeechMetadata,
                    SidecarKind.SpeechMapping,
                    SidecarKind.Overrides);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DeleteMimesisData: {ex.Message}");
            }
        }
    }
}
