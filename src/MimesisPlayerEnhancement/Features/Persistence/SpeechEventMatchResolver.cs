namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Pure voice-ID matching helpers for pool claim / disconnect reclaim.
    /// </summary>
    internal static class SpeechEventMatchResolver
    {
        internal static void AddDirectPlayerId(
            HashSet<string> matchedPlayerNames,
            string? playerId,
            IReadOnlyCollection<string> poolVoiceIds,
            bool useDisconnectedMapping)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }

            if (ContainsVoiceId(poolVoiceIds, playerId) || useDisconnectedMapping)
            {
                _ = matchedPlayerNames.Add(playerId);
            }
        }

        internal static void AddMappedVoiceId(
            HashSet<string> matchedPlayerNames,
            ulong steamId,
            IReadOnlyDictionary<ulong, string> mappingSource,
            IReadOnlyCollection<string> poolVoiceIds,
            bool requirePoolMatch)
        {
            if (steamId == 0 || !mappingSource.TryGetValue(steamId, out string? mappedVoiceId)
                || string.IsNullOrEmpty(mappedVoiceId))
            {
                return;
            }

            if (!requirePoolMatch || ContainsVoiceId(poolVoiceIds, mappedVoiceId))
            {
                _ = matchedPlayerNames.Add(mappedVoiceId);
            }
        }

        internal static HashSet<string> ResolveMatchedPlayerNames(
            string? playerId,
            ulong steamId,
            IReadOnlyDictionary<ulong, string> registryMappings,
            IReadOnlyDictionary<ulong, string> disconnectedMappings,
            IReadOnlyCollection<string> poolVoiceIds,
            bool useDisconnectedMapping)
        {
            HashSet<string> matchedPlayerNames = [];

            AddDirectPlayerId(matchedPlayerNames, playerId, poolVoiceIds, useDisconnectedMapping);
            // Disconnected map: requirePoolMatch tracks useDisconnectedMapping (legacy claim path).
            AddMappedVoiceId(
                matchedPlayerNames,
                steamId,
                disconnectedMappings,
                poolVoiceIds,
                requirePoolMatch: useDisconnectedMapping);
            AddMappedVoiceId(
                matchedPlayerNames,
                steamId,
                registryMappings,
                poolVoiceIds,
                requirePoolMatch: !useDisconnectedMapping);

            return matchedPlayerNames;
        }

        internal static string? ResolveDominantVoiceId(
            IReadOnlyList<string> poolVoiceIds,
            IReadOnlyDictionary<string, int> eventCountsByVoiceId)
        {
            string? dominant = null;
            int dominantCount = 0;

            foreach (string poolVoiceId in poolVoiceIds)
            {
                if (string.IsNullOrEmpty(poolVoiceId))
                {
                    continue;
                }

                if (!eventCountsByVoiceId.TryGetValue(poolVoiceId, out int count) || count <= dominantCount)
                {
                    continue;
                }

                dominantCount = count;
                dominant = poolVoiceId;
            }

            return dominant;
        }

        internal static string? ResolveDominantPlayerName(IReadOnlyList<string?> playerNames)
        {
            Dictionary<string, int> counts = [];
            string? dominant = null;
            int max = 0;

            foreach (string? playerName in playerNames)
            {
                if (string.IsNullOrEmpty(playerName))
                {
                    continue;
                }

                counts.TryGetValue(playerName, out int count);
                count++;
                counts[playerName] = count;
                if (count > max)
                {
                    max = count;
                    dominant = playerName;
                }
            }

            return dominant;
        }

        /// <summary>
        /// When no Steam→VoiceId mapping was persisted, assign unmapped pool voice IDs
        /// to a connecting player known on this save.
        /// </summary>
        internal static void InferUnmappedPoolVoiceIds(
            HashSet<string> matchedPlayerNames,
            ulong steamId,
            bool isKnownSavePlayer,
            bool isSoloSaveForSteam,
            IReadOnlyCollection<string> poolVoiceIds,
            IReadOnlyCollection<string> mappedToOtherSteamIds,
            IReadOnlyDictionary<string, int> eventCountsByVoiceId)
        {
            if (steamId == 0 || !isKnownSavePlayer || poolVoiceIds.Count == 0)
            {
                return;
            }

            List<string> unmapped = [];
            foreach (string poolName in poolVoiceIds)
            {
                if (!string.IsNullOrEmpty(poolName) && !ContainsVoiceId(mappedToOtherSteamIds, poolName))
                {
                    unmapped.Add(poolName);
                }
            }

            if (unmapped.Count == 0)
            {
                return;
            }

            if (isSoloSaveForSteam)
            {
                foreach (string poolName in unmapped)
                {
                    _ = matchedPlayerNames.Add(poolName);
                }

                return;
            }

            string? dominant = ResolveDominantVoiceId(unmapped, eventCountsByVoiceId);
            if (!string.IsNullOrEmpty(dominant))
            {
                _ = matchedPlayerNames.Add(dominant);
            }
        }

        internal static bool ShouldCacheDisconnectEvents(bool persistenceEnabled, bool isHost, bool isLocal) =>
            persistenceEnabled && isHost && !isLocal;

        private static bool ContainsVoiceId(IReadOnlyCollection<string> voiceIds, string voiceId)
        {
            foreach (string candidate in voiceIds)
            {
                if (string.Equals(candidate, voiceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
