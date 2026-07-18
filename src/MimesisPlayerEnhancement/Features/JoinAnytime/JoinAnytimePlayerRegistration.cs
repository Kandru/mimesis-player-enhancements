namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Defers statistics and name sidecar registration until a JoinAnytime joiner
    /// reaches the same "fully ready" state as ConnectingTracker, and cleans up
    /// incomplete attempts without persisting sidecar data.
    /// </summary>
    internal static class JoinAnytimePlayerRegistration
    {
        private const string Feature = "JoinAnytime";

        private static readonly HashSet<long> FullyReadyUids = [];
        private static readonly HashSet<ulong> DeferredSteamIds = [];
        private static readonly Dictionary<long, bool> HadStoredStatsBeforeConnect = [];

        internal static void Reset()
        {
            FullyReadyUids.Clear();
            DeferredSteamIds.Clear();
            HadStoredStatsBeforeConnect.Clear();
        }

        internal static void NoteDeferredConnect(long playerUid, ulong steamId)
        {
            if (playerUid == 0 || !ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            HadStoredStatsBeforeConnect[playerUid] = steamId != 0
                && PlayerRegistry.TryGetStatistics(steamId, out _);

            if (steamId != 0)
            {
                _ = DeferredSteamIds.Add(steamId);
            }
        }

        internal static bool ShouldDeferRegistration(long playerUid)
        {
            if (!ModConfig.EnableJoinAnytime.Value || playerUid == 0)
            {
                return false;
            }

            if (SessionContextAccess.TryGetHostPlayerUid(out long hostUid) && playerUid == hostUid)
            {
                return false;
            }

            if (SessionContextAccess.TryGetPlayerByUid(playerUid, out VPlayer? player) && player!.IsHost)
            {
                return false;
            }

            return !FullyReadyUids.Contains(playerUid);
        }

        internal static bool ShouldDeferRegistration(VPlayer? player)
        {
            if (player == null)
            {
                return false;
            }

            return ShouldDeferRegistration(player.UID);
        }

        internal static bool ShouldDeferRegistrationBySteamId(ulong steamId)
        {
            if (steamId == 0)
            {
                return false;
            }

            if (steamId != 0 && DeferredSteamIds.Contains(steamId))
            {
                return true;
            }

            return ShouldDeferRegistration(ResolvePlayerUidFromSteamId(steamId));
        }

        private static long ResolvePlayerUidFromSteamId(ulong steamId)
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata?.actorUIDToSteamID == null)
            {
                return 0;
            }

            foreach (KeyValuePair<long, ulong> kvp in pdata.actorUIDToSteamID)
            {
                if (kvp.Value == steamId)
                {
                    return kvp.Key;
                }
            }

            return 0;
        }

        internal static void MarkFullyReady(VPlayer player)
        {
            if (player == null || player.IsHost)
            {
                return;
            }

            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            long uid = player.UID;
            if (uid == 0 || !FullyReadyUids.Add(uid))
            {
                return;
            }

            _ = HadStoredStatsBeforeConnect.Remove(uid);

            ulong steamId = GameSessionAccess.ResolveSteamId(player.UID, player.IsHost);
            if (steamId == 0)
            {
                ModLog.Debug(Feature, $"Registration complete deferred — uid={uid}, steamId unresolved");
                return;
            }

            int slotId = GameSessionAccess.GetSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId)
                && !PlayerRegistry.TryGetLoadedSlotId(out slotId))
            {
                ModLog.Debug(Feature, $"Registration complete deferred — uid={uid}, no save slot");
                return;
            }

            _ = DeferredSteamIds.Remove(steamId);

            string displayName = StatisticsDisplayNameResolver.Resolve(steamId, string.Empty);
            if (SaveSlotDocumentStore.IsUsableName(displayName, steamId))
            {
                PlayerRegistry.UpdateDisplayName(steamId, displayName);
            }

            string? voiceId = TryResolveVoiceId(steamId);
            if (!string.IsNullOrWhiteSpace(voiceId))
            {
                PlayerRegistry.UpdateVoiceId(steamId, voiceId);
            }

            PlayerPresenceEvents.OnPlayerRegistered(steamId, slotId);
            ModLog.Debug(Feature, $"Registration complete — uid={uid}, steamId={steamId}");
        }

        internal static void AbandonIncomplete(long playerUid, ulong steamIdHint = 0)
        {
            if (!ModConfig.EnableJoinAnytime.Value || playerUid == 0)
            {
                return;
            }

            _ = FullyReadyUids.Remove(playerUid);

            bool hadPriorStats = HadStoredStatsBeforeConnect.TryGetValue(playerUid, out bool hadStored)
                                 && hadStored;
            _ = HadStoredStatsBeforeConnect.Remove(playerUid);

            ulong steamId = steamIdHint;
            if (steamId == 0 && SessionContextAccess.TryGetPlayerByUid(playerUid, out VPlayer? player))
            {
                steamId = GameSessionAccess.ResolveSteamId(playerUid, player!.IsHost);
            }

            if (steamId == 0)
            {
                return;
            }

            _ = DeferredSteamIds.Remove(steamId);

            if (!hadPriorStats)
            {
                StatisticsTracker.AbandonIncompleteConnection(steamId);
            }

            ModLog.Info(Feature, $"Abandoned incomplete join — uid={playerUid}, steamId={steamId}");
        }

        private static string? TryResolveVoiceId(ulong steamId)
        {
            if (SpeechEventPoolManager.TryResolveVoiceIdForSteam(steamId, out string? voiceId))
            {
                return voiceId;
            }

            return SaveSlotDocumentStore.TryGetVoiceId(SaveSlotDocumentStore.LoadedSlotId, steamId, out voiceId)
                ? voiceId
                : null;
        }
    }
}
