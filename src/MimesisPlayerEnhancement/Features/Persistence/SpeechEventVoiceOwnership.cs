namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Determines which Dissonance voice IDs (SpeechEvent.PlayerName) belong to known save-slot players.
    /// </summary>
    internal static class SpeechEventVoiceOwnership
    {
        internal static HashSet<string> CollectValidVoiceIds(int slotId, bool requirePoolEvidence = false)
        {
            HashSet<string> validVoiceIds = new(StringComparer.Ordinal);
            HashSet<string> poolVoiceIds = requirePoolEvidence ? CollectPoolVoiceIds() : [];

            foreach (KeyValuePair<ulong, string> kvp in PlayerRegistry.GetVoiceMappings())
            {
                if (string.IsNullOrEmpty(kvp.Value))
                {
                    continue;
                }

                if (!requirePoolEvidence || poolVoiceIds.Contains(kvp.Value))
                {
                    _ = validVoiceIds.Add(kvp.Value);
                }
            }

            SaveSlotDocumentStore.ApplyPlayerEntries((steamId, entry) =>
            {
                if (string.IsNullOrWhiteSpace(entry.VoiceId))
                {
                    return;
                }

                if (!requirePoolEvidence || poolVoiceIds.Contains(entry.VoiceId))
                {
                    _ = validVoiceIds.Add(entry.VoiceId);
                }
            });

            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                try
                {
                    string playerId = archive.PlayerId;
                    if (string.IsNullOrEmpty(playerId))
                    {
                        continue;
                    }

                    ulong steamId = GameSessionAccess.ResolveSteamId(archive.PlayerUID, archive.IsLocal);
                    if (steamId != 0 && IsKnownSteamId(slotId, steamId))
                    {
                        _ = validVoiceIds.Add(playerId);
                    }
                }
                catch
                {
                    /* archive may be partially destroyed */
                }
            }

            return validVoiceIds;
        }

        private static HashSet<string> CollectPoolVoiceIds()
        {
            HashSet<string> poolVoiceIds = new(StringComparer.Ordinal);
            foreach (SpeechEvent ev in SpeechEventPoolManager.GetPendingEvents())
            {
                if (!string.IsNullOrEmpty(ev.PlayerName))
                {
                    _ = poolVoiceIds.Add(ev.PlayerName);
                }
            }

            return poolVoiceIds;
        }

        internal static bool IsOwnedVoiceId(string? playerName, HashSet<string> validVoiceIds)
        {
            return !string.IsNullOrEmpty(playerName) && validVoiceIds.Contains(playerName);
        }

        internal static bool CanPruneOrphans(HashSet<string> validVoiceIds) => validVoiceIds.Count > 0;

        internal static List<SpeechEvent> FilterOwnedEvents(int slotId, List<SpeechEvent> events)
        {
            if (events.Count == 0)
            {
                return events;
            }

            HashSet<string> validVoiceIds = CollectValidVoiceIds(slotId);
            if (!CanPruneOrphans(validVoiceIds))
            {
                return events;
            }

            List<SpeechEvent> owned = [];
            foreach (SpeechEvent ev in events)
            {
                if (ev != null && IsOwnedVoiceId(ev.PlayerName, validVoiceIds))
                {
                    owned.Add(ev);
                }
            }

            return owned;
        }

        private static bool IsKnownSteamId(int slotId, ulong steamId)
        {
            if (PlayerRegistry.TryGetStatistics(steamId, out _))
            {
                return true;
            }

            return MimesisSaveManager.IsValidSaveSlotId(slotId)
                && SaveSlotDocumentStore.TryGetName(slotId, steamId, out _);
        }
    }
}
