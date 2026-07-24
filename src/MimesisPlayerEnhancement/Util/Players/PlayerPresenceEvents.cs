namespace MimesisPlayerEnhancement.Util.Players
{
    internal static class PlayerPresenceEvents
    {
        internal static void OnPlayerRegistered(ulong steamId, int slotId)
        {
            if (steamId == 0)
            {
                return;
            }

            if (ShouldLoadRegistryOnRegister(
                    steamId,
                    MimesisSaveManager.IsHost(),
                    MimesisSaveManager.IsValidSaveSlotId(slotId)))
            {
                PlayerRegistry.LoadForSlot(slotId);
            }

            StatisticsTracker.OnPlayerRegistered(steamId, slotId);
        }

        internal static void OnPlayerUnregistered(ulong steamId)
        {
            if (!ShouldHandleUnregister(steamId, ModConfig.EnableStatistics.Value))
            {
                return;
            }

            StatisticsTracker.OnPlayerUnregistered(steamId);
        }

        internal static void OnArchiveStarted(SpeechEventArchive? archive, int slotId)
        {
            if (!CanStartArchivePresence(
                    archive != null,
                    ModConfig.EnableStatistics.Value,
                    MimesisSaveManager.IsValidSaveSlotId(slotId),
                    slotId,
                    PlayerRegistry.LoadedSlotId))
            {
                return;
            }

            SpeechEventArchive readyArchive = archive!;
            if (!StatisticsArchiveIdentity.IsArchiveIdentityReady(readyArchive))
            {
                return;
            }

            ulong steamId = StatisticsArchiveIdentity.ResolveSteamIdFromArchive(readyArchive);
            if (!ShouldApplyArchivePresence(steamId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(readyArchive.PlayerId))
            {
                PlayerRegistry.UpdateVoiceId(steamId, readyArchive.PlayerId);
            }

            StatisticsTracker.HandleArchiveStarted(readyArchive, slotId);
        }

        internal static bool ShouldLoadRegistryOnRegister(ulong steamId, bool isHost, bool validSlot)
        {
            return steamId != 0 && isHost && validSlot;
        }

        internal static bool ShouldHandleUnregister(ulong steamId, bool statisticsEnabled)
        {
            return steamId != 0 && statisticsEnabled;
        }

        internal static bool CanStartArchivePresence(
            bool archivePresent,
            bool statisticsEnabled,
            bool validSlot,
            int slotId,
            int loadedSlotId)
        {
            return archivePresent
                && statisticsEnabled
                && validSlot
                && slotId == loadedSlotId;
        }

        internal static bool ShouldApplyArchivePresence(ulong steamId)
        {
            return steamId != 0;
        }
    }
}
