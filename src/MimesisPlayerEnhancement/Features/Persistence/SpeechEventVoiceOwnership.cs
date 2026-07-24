namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Determines which Dissonance voice IDs (SpeechEvent.PlayerName) belong to known save-slot players.
    /// </summary>
    internal static class SpeechEventVoiceOwnership
    {
        internal static HashSet<string> CollectValidVoiceIds(int slotId)
        {
            HashSet<string> validVoiceIds = new(StringComparer.Ordinal);

            foreach (KeyValuePair<ulong, string> kvp in PlayerRegistry.GetVoiceMappings())
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    _ = validVoiceIds.Add(kvp.Value);
                }
            }

            SaveSlotDocumentStore.ApplyPlayerEntries((steamId, entry) =>
            {
                if (!string.IsNullOrWhiteSpace(entry.VoiceId))
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
                    if (steamId != 0 && IsKnownSavePlayer(slotId, steamId))
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

            return FilterOwnedEvents(events, CollectValidVoiceIds(slotId));
        }

        internal static List<SpeechEvent> FilterOwnedEvents(
            List<SpeechEvent> events,
            HashSet<string> validVoiceIds)
        {
            if (events.Count == 0 || !CanPruneOrphans(validVoiceIds))
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

        internal static List<string> FilterOwnedPlayerNames(
            IReadOnlyList<string?> playerNames,
            HashSet<string> validVoiceIds)
        {
            if (playerNames.Count == 0 || !CanPruneOrphans(validVoiceIds))
            {
                List<string> passthrough = [];
                foreach (string? name in playerNames)
                {
                    if (name != null)
                    {
                        passthrough.Add(name);
                    }
                }

                return passthrough;
            }

            List<string> owned = [];
            foreach (string? playerName in playerNames)
            {
                if (IsOwnedVoiceId(playerName, validVoiceIds))
                {
                    owned.Add(playerName!);
                }
            }

            return owned;
        }

        /// <summary>
        /// True when the Steam ID belongs to a player recorded on this save (statistics or roster entry).
        /// </summary>
        internal static bool IsKnownSavePlayer(int slotId, ulong steamId)
        {
            if (steamId == 0)
            {
                return false;
            }

            if (PlayerRegistry.TryGetStatistics(steamId, out _))
            {
                return true;
            }

            return MimesisSaveManager.IsValidSaveSlotId(slotId)
                && (SaveSlotDocumentStore.TryGetName(slotId, steamId, out _)
                    || SaveSlotDocumentStore.TryGetVoiceId(slotId, steamId, out _));
        }
    }
}
