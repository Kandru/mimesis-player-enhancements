namespace MimesisPlayerEnhancement.Features.Players
{
    internal static class PlayerPresenceEvents
    {
        internal static void OnPlayerRegistered(ulong steamId, int slotId)
        {
            if (steamId == 0)
            {
                return;
            }

            if (MimesisSaveManager.IsHost() && MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                PlayerRegistry.LoadForSlot(slotId);
            }

            StatisticsTracker.OnPlayerRegistered(steamId, slotId);
        }

        internal static void OnPlayerUnregistered(ulong steamId)
        {
            if (steamId == 0 || !ModConfig.EnableStatistics.Value)
            {
                return;
            }

            StatisticsTracker.OnPlayerUnregistered(steamId);
        }

        internal static void OnArchiveStarted(SpeechEventArchive archive, int slotId)
        {
            if (archive == null || !ModConfig.EnableStatistics.Value)
            {
                return;
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId) || slotId != PlayerRegistry.LoadedSlotId)
            {
                return;
            }

            if (!StatisticsArchiveIdentity.IsArchiveIdentityReady(archive))
            {
                return;
            }

            ulong steamId = StatisticsArchiveIdentity.ResolveSteamIdFromArchive(archive);
            if (steamId == 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(archive.PlayerId))
            {
                PlayerRegistry.UpdateVoiceId(steamId, archive.PlayerId);
            }

            StatisticsTracker.HandleArchiveStarted(archive, slotId);
        }
    }
}
