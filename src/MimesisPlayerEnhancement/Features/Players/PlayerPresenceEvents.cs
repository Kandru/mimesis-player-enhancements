namespace MimesisPlayerEnhancement.Features.Players
{
    internal static class PlayerPresenceEvents
    {
        internal static void OnPlayerRegistered(ulong steamId, int slotId)
        {
            if (steamId == 0 || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                if (!PlayerRegistry.TryGetLoadedSlotId(out slotId))
                {
                    return;
                }
            }

            PlayerRegistry.LoadForSlot(slotId);

            if (PlayerRegistry.IsConnected(steamId))
            {
                return;
            }

            PlayerRecord record = PlayerRegistry.GetOrCreate(steamId);
            string displayName = PlayerRegistry.ApplyResolvedDisplayName(steamId, record.DisplayName);
            if (SaveSlotDocumentStore.IsUsableName(displayName, steamId))
            {
                SaveSlotDocumentStore.UpsertPlayer(steamId, displayName);
            }

            PlayerRegistry.SetConnectedSince(steamId, DateTime.UtcNow);
            PlayerRegistry.BumpRevision();
            WebDashboardSnapshotCache.MarkDirty();

            if (ModConfig.EnableStatistics.Value)
            {
                StatisticsTracker.OnPlayerRegistered(steamId, slotId);
            }
        }

        internal static void OnPlayerUnregistered(ulong steamId)
        {
            if (steamId == 0 || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            if (!PlayerRegistry.IsConnected(steamId))
            {
                return;
            }

            if (ModConfig.EnableStatistics.Value)
            {
                StatisticsTracker.OnPlayerUnregistered(steamId);
            }

            if (PlayerRegistry.IsConnected(steamId))
            {
                PlayerRegistry.MarkDisconnected(steamId);
                PlayerRegistry.BumpRevision();
                WebDashboardSnapshotCache.MarkDirty();
            }
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
