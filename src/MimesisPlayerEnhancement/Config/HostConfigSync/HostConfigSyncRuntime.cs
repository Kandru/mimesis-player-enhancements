namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    internal static class HostConfigSyncRuntime
    {
        private const string Feature = "HostConfigSync";
        private const float BroadcastDebounceSeconds = 0.15f;
        private const float HelloRetrySeconds = 2f;
        private const int HelloStatusLogInterval = 8;

        private static readonly HashSet<ulong> ModPeers = [];

        private static int _revision;
        private static bool _broadcastPending;
        private static float _broadcastAfterTime;
        private static bool _clientHelloPending;
        private static float _nextHelloTime;
        private static int _helloSendCount;
        private static bool _subscribedToConfigChanges;

        internal static void OnSessionStarted(SessionRole role, int slotId)
        {
            _revision = 0;
            _broadcastPending = false;
            ModPeers.Clear();
            ResetClientHelloState();

            if (role == SessionRole.Client)
            {
                _clientHelloPending = true;
                _nextHelloTime = UnityEngine.Time.unscaledTime;
                ModLog.Info(Feature, "Client config mirror — requesting host save settings.");
                return;
            }

            if (role == SessionRole.Host)
            {
                SubscribeHostConfigChanges();
                ModLog.Info(Feature, $"Host config sync active — slot={slotId}.");
            }
        }

        internal static void OnSessionEnded()
        {
            UnsubscribeHostConfigChanges();
            ModPeers.Clear();
            _broadcastPending = false;
            _revision = 0;
            ResetClientHelloState();
            HostConfigSyncTransport.ResetSessionState();
            HostConfigSyncCodec.ResetSessionDiagnostics();
            HostConfigMirror.Clear();
            ModLog.Debug(Feature, "Session sync state cleared.");
        }

        internal static void OnUpdate()
        {
            TickClientHello();

            if (_broadcastPending && UnityEngine.Time.unscaledTime >= _broadcastAfterTime)
            {
                _broadcastPending = false;
                BroadcastSnapshot();
            }
        }

        internal static void OnClientHello(VPlayer player, string modVersion)
        {
            if (player == null)
            {
                return;
            }

            if (!HostConfigSyncCodec.IsCompatibleModVersion(modVersion))
            {
                HostConfigSyncCodec.LogProtocolMismatchOnce(modVersion);
                return;
            }

            ulong steamId = player.SteamID;
            if (steamId == 0)
            {
                return;
            }

            bool added = ModPeers.Add(steamId);
            if (added)
            {
                ModLog.Info(Feature, $"Mod peer joined — steamId={steamId}, mod={modVersion}.");
            }
            else
            {
                ModLog.Debug(Feature, $"Mod peer re-hello — steamId={steamId}, resending snapshot.");
            }

            if (player.IsHost)
            {
                return;
            }

            SendSnapshotToPlayer(player);
        }

        internal static void OnPlayerUnregistered(ulong steamId)
        {
            if (steamId == 0)
            {
                return;
            }

            if (ModPeers.Remove(steamId))
            {
                ModLog.Debug(Feature, $"Mod peer left — steamId={steamId}.");
            }
        }

        internal static void OnPlayerRegistered(ulong steamId)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature() || steamId == 0 || !ModPeers.Contains(steamId))
            {
                return;
            }

            VPlayer? player = TryResolveModPeer(steamId);
            if (player != null)
            {
                SendSnapshotToPlayer(player);
            }
        }

        internal static void OnMirrorApplied()
        {
            _clientHelloPending = false;
            _helloSendCount = 0;
        }

        private static void TickClientHello()
        {
            if (!_clientHelloPending || HostConfigMirror.IsActive)
            {
                return;
            }

            if (UnityEngine.Time.unscaledTime < _nextHelloTime)
            {
                return;
            }

            _nextHelloTime = UnityEngine.Time.unscaledTime + HelloRetrySeconds;
            if (!HostConfigSyncTransport.TrySendHello())
            {
                return;
            }

            _helloSendCount++;
            if (_helloSendCount % HelloStatusLogInterval == 0)
            {
                ModLog.Debug(Feature, "Still waiting for host config mirror.");
            }
        }

        private static void SubscribeHostConfigChanges()
        {
            if (_subscribedToConfigChanges)
            {
                return;
            }

            ModConfig.Changed += OnHostConfigChanged;
            _subscribedToConfigChanges = true;
        }

        private static void UnsubscribeHostConfigChanges()
        {
            if (!_subscribedToConfigChanges)
            {
                return;
            }

            ModConfig.Changed -= OnHostConfigChanged;
            _subscribedToConfigChanges = false;
        }

        private static void OnHostConfigChanged(ModConfigChangeInfo change)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature() || ModPeers.Count == 0)
            {
                return;
            }

            if (!AffectsSyncedConfig(change))
            {
                return;
            }

            ScheduleBroadcast();
        }

        private static void ScheduleBroadcast()
        {
            _revision++;
            _broadcastPending = true;
            _broadcastAfterTime = UnityEngine.Time.unscaledTime + BroadcastDebounceSeconds;
            ModLog.Debug(Feature, $"Scheduled config broadcast — rev={_revision}, peers={ModPeers.Count}.");
        }

        private static void BroadcastSnapshot()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature() || ModPeers.Count == 0)
            {
                return;
            }

            if (!IsSnapshotReady())
            {
                ScheduleDeferredBroadcast();
                return;
            }

            EnsureRevision();
            List<ulong> stalePeers = [];
            int sent = 0;

            foreach (ulong steamId in ModPeers)
            {
                VPlayer? player = TryResolveModPeer(steamId);
                if (player == null)
                {
                    stalePeers.Add(steamId);
                    continue;
                }

                HostConfigSyncTransport.SendSnapshot(player, _revision);
                sent++;
            }

            foreach (ulong steamId in stalePeers)
            {
                if (ModPeers.Remove(steamId))
                {
                    ModLog.Debug(Feature, $"Pruned disconnected mod peer — steamId={steamId}.");
                }
            }

            if (sent > 0)
            {
                ModLog.Debug(Feature, $"Broadcast config snapshot — rev={_revision}, peers={sent}.");
            }
        }

        private static void SendSnapshotToPlayer(VPlayer player)
        {
            if (player == null || player.IsHost)
            {
                return;
            }

            if (!IsSnapshotReady())
            {
                ScheduleDeferredBroadcast();
                ModLog.Debug(Feature, "Deferred config snapshot — save slot not loaded yet.");
                return;
            }

            EnsureRevision();
            HostConfigSyncTransport.SendSnapshot(player, _revision);
        }

        private static void ScheduleDeferredBroadcast()
        {
            if (_broadcastPending)
            {
                return;
            }

            _broadcastPending = true;
            _broadcastAfterTime = UnityEngine.Time.unscaledTime + BroadcastDebounceSeconds;
        }

        private static bool IsSnapshotReady()
        {
            return SaveSlotConfigStore.ActiveSlotId >= 0;
        }

        private static void EnsureRevision()
        {
            if (_revision == 0)
            {
                _revision = 1;
            }
        }

        private static VPlayer? TryResolveModPeer(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            SessionManager? sessionManager = SessionContextAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return null;
            }

            foreach (SessionContext context in SessionContextAccess.EnumerateSessionContexts(sessionManager))
            {
                if (context.SteamID != steamId)
                {
                    continue;
                }

                VPlayer? player = SessionContextAccess.GetVPlayer(context);
                if (player != null && !player.IsHost)
                {
                    return player;
                }

                return null;
            }

            return null;
        }

        private static bool AffectsSyncedConfig(ModConfigChangeInfo change)
        {
            if (change.IsFullReload)
            {
                return true;
            }

            foreach (ModConfigKeyChange keyChange in change.ChangedKeys)
            {
                if (HostConfigSyncCodec.ShouldSyncKey(keyChange.SectionId, keyChange.Key))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResetClientHelloState()
        {
            _clientHelloPending = false;
            _helloSendCount = 0;
            _nextHelloTime = 0f;
        }
    }
}
