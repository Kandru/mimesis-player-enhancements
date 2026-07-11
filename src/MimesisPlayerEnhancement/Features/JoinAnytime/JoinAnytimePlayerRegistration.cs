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
        private static readonly Dictionary<long, bool> HadStoredStatsBeforeConnect = [];

        internal static void Reset()
        {
            FullyReadyUids.Clear();
            HadStoredStatsBeforeConnect.Clear();
        }

        internal static void NoteDeferredConnect(long playerUid, ulong steamId)
        {
            if (playerUid == 0 || !ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            HadStoredStatsBeforeConnect[playerUid] = steamId != 0
                && StatisticsTracker.TryGetPlayerDocument(steamId) != null;
        }

        internal static bool ShouldDeferRegistration(long playerUid)
        {
            if (!ModConfig.EnableJoinAnytime.Value || playerUid == 0)
            {
                return false;
            }

            if (WebDashboardSessionAccess.TryGetHostPlayerUid(out long hostUid) && playerUid == hostUid)
            {
                return false;
            }

            if (WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player) && player!.IsHost)
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
                && !StatisticsTracker.TryGetLoadedSlotId(out slotId))
            {
                ModLog.Debug(Feature, $"Registration complete deferred — uid={uid}, no save slot");
                return;
            }

            StatisticsTracker.OnPlayerRegistered(steamId, slotId);
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
            if (steamId == 0 && WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player))
            {
                steamId = GameSessionAccess.ResolveSteamId(playerUid, player!.IsHost);
            }

            if (steamId == 0)
            {
                return;
            }

            if (!hadPriorStats)
            {
                StatisticsTracker.RemovePlayerIfNeverConnected(steamId);
            }

            ModLog.Info(Feature, $"Abandoned incomplete join — uid={playerUid}, steamId={steamId}");
        }
    }
}
