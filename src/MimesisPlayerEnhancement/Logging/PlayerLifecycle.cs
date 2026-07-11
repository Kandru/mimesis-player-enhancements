using System.Linq;
using System.Text;

namespace MimesisPlayerEnhancement
{
    public readonly struct PlayerLifecycleContribution(string feature, string detail)
    {
        public string Feature { get; } = feature;
        public string Detail { get; } = detail;
    }

    internal enum PersistenceConnectPhase
    {
        Skipped,
        Connecting,
        Connected,
    }

    internal readonly struct PersistenceConnectOutcome(PersistenceConnectPhase phase, string? detail)
    {
        internal PersistenceConnectPhase Phase { get; } = phase;
        internal string? Detail { get; } = detail;
    }

    internal static class PlayerLifecycleCoordinator
    {
        private const string LogFeature = "Session";
        private static readonly string[] ContributionOrder = ["MoreVoices", "Persistence", "Statistics"];

        private sealed class ConnectState
        {
            internal SpeechEventArchive Archive = null!;
            internal ulong SteamId;
            internal bool ConnectingLogged;
            internal bool ConnectedLogged;
            internal PersistenceConnectPhase PersistencePhase = PersistenceConnectPhase.Skipped;
            internal readonly Dictionary<string, PlayerLifecycleContribution> Contributions = [];
        }

        private sealed class DisconnectState
        {
            internal SpeechEventArchive? Archive;
            internal ulong SteamId;
            internal long PlayerUid;
            internal bool DisconnectedLogged;
            internal readonly Dictionary<string, PlayerLifecycleContribution> Contributions = [];
        }

        private static readonly Dictionary<SpeechEventArchive, ConnectState> _connectByArchive = [];
        private static readonly Dictionary<ulong, ConnectState> _connectBySteam = [];
        private static readonly Dictionary<ulong, DisconnectState> _disconnectBySteam = [];
        private static readonly Dictionary<ulong, PlayerLifecycleContribution> _pendingStatisticsConnect = [];

        internal static void FinishArchiveConnect(SpeechEventArchive archive)
        {
            if (archive == null)
            {
                return;
            }

            ConnectState state = GetOrCreateConnectState(archive);
            PlayerLifecycleContribution? moreVoices = MoreVoicesPatchHelpers.TryDescribeArchiveStarted(archive);
            if (moreVoices.HasValue)
            {
                state.Contributions[moreVoices.Value.Feature] = moreVoices.Value;
            }

            ApplyPendingStatisticsConnect(state);
            TryFlushConnect(state);
        }

        internal static void SetPersistenceOutcome(SpeechEventArchive archive, PersistenceConnectOutcome outcome, bool flush = false)
        {
            if (archive == null)
            {
                return;
            }

            ConnectState state = GetOrCreateConnectState(archive);
            state.PersistencePhase = outcome.Phase;
            if (string.IsNullOrEmpty(outcome.Detail))
            {
                _ = state.Contributions.Remove("Persistence");
            }
            else
            {
                state.Contributions["Persistence"] = new PlayerLifecycleContribution("Persistence", outcome.Detail);
            }

            if (flush)
            {
                TryFlushConnect(state);
            }
        }

        internal static void TryFlushConnect(SpeechEventArchive archive)
        {
            if (archive != null && _connectByArchive.TryGetValue(archive, out ConnectState? state))
            {
                TryFlushConnect(state);
            }
        }

        internal static void NotifyStatisticsConnect(ulong steamId, PlayerLifecycleContribution? contribution)
        {
            if (steamId == 0 || !contribution.HasValue)
            {
                return;
            }

            ConnectState? state = FindConnectStateForSteam(steamId);
            if (state == null)
            {
                _pendingStatisticsConnect[steamId] = contribution.Value;
                return;
            }

            BindConnectSteam(state, steamId);
            state.Contributions[contribution.Value.Feature] = contribution.Value;
            if (!state.ConnectedLogged)
            {
                TryFlushConnect(state);
            }
        }

        internal static void OnArchiveDisconnecting(
            SpeechEventArchive archive,
            PlayerLifecycleContribution? contribution,
            ulong steamIdHint = 0,
            long playerUidHint = 0)
        {
            if (archive == null)
            {
                return;
            }

            ulong steamId = steamIdHint;
            long playerUid = playerUidHint;
            if (steamId == 0 && playerUid == 0)
            {
                _ = VoiceEventStats.TryCaptureArchiveIdentity(archive, out playerUid, out _, out steamId);
            }

            DisconnectState state = GetOrCreateDisconnectState(steamId, archive, playerUid);
            if (contribution.HasValue)
            {
                state.Contributions[contribution.Value.Feature] = contribution.Value;
            }

            TryFlushDisconnect(state);
        }

        internal static void NotifyStatisticsDisconnect(ulong steamId, PlayerLifecycleContribution? contribution)
        {
            if (steamId == 0)
            {
                return;
            }

            DisconnectState state = GetOrCreateDisconnectState(steamId, archive: null, playerUid: 0);
            state.SteamId = steamId;
            if (contribution.HasValue)
            {
                state.Contributions[contribution.Value.Feature] = contribution.Value;
            }

            if (!ShouldDeferDisconnectForPersistence(steamId))
            {
                TryFlushDisconnect(state);
            }
        }

        internal static void ClearConnectState(SpeechEventArchive archive)
        {
            if (archive == null)
            {
                return;
            }

            if (_connectByArchive.Remove(archive, out ConnectState? state) && state.SteamId != 0)
            {
                _ = _connectBySteam.Remove(state.SteamId);
            }
        }

        private static ConnectState GetOrCreateConnectState(SpeechEventArchive archive)
        {
            if (_connectByArchive.TryGetValue(archive, out ConnectState? existing))
            {
                EnsureSteamId(existing, archive);
                return existing;
            }

            ConnectState state = new() { Archive = archive };
            EnsureSteamId(state, archive);
            _connectByArchive[archive] = state;
            return state;
        }

        private static void EnsureSteamId(ConnectState state, SpeechEventArchive archive)
        {
            if (state.SteamId != 0)
            {
                return;
            }

            if (VoiceEventStats.TryGetConnectionInfo(archive, out PlayerConnectionInfo info) && info.SteamId != 0)
            {
                BindConnectSteam(state, info.SteamId);
            }
        }

        private static void BindConnectSteam(ConnectState state, ulong steamId)
        {
            if (steamId == 0)
            {
                return;
            }

            state.SteamId = steamId;
            _connectBySteam[steamId] = state;
            ApplyPendingStatisticsConnect(state);
        }

        private static void ApplyPendingStatisticsConnect(ConnectState state)
        {
            if (state.SteamId == 0)
            {
                return;
            }

            if (_pendingStatisticsConnect.Remove(state.SteamId, out PlayerLifecycleContribution stats))
            {
                state.Contributions[stats.Feature] = stats;
            }
        }

        private static ConnectState? FindConnectStateForSteam(ulong steamId)
        {
            if (_connectBySteam.TryGetValue(steamId, out ConnectState? bySteam))
            {
                return bySteam;
            }

            ConnectState? matched = null;
            ConnectState? unassigned = null;
            int unassignedCount = 0;

            foreach (ConnectState state in _connectByArchive.Values)
            {
                if (state.SteamId != 0)
                {
                    continue;
                }

                unassignedCount++;
                unassigned = state;

                if (TryArchiveMatchesSteam(state.Archive, steamId))
                {
                    matched = state;
                    break;
                }
            }

            if (matched != null)
            {
                BindConnectSteam(matched, steamId);
                return matched;
            }

            if (unassignedCount == 1 && unassigned != null)
            {
                BindConnectSteam(unassigned, steamId);
                return unassigned;
            }

            return null;
        }

        private static bool TryArchiveMatchesSteam(SpeechEventArchive archive, ulong steamId)
        {
            if (archive == null || steamId == 0)
            {
                return false;
            }

            if (VoiceEventStats.TryCaptureArchiveIdentity(archive, out long playerUid, out bool isLocal, out ulong archiveSteam))
            {
                if (archiveSteam == steamId)
                {
                    return true;
                }

                if (playerUid != 0 && GameSessionAccess.ResolveSteamId(playerUid, isLocal) == steamId)
                {
                    return true;
                }
            }

            return VoiceEventStats.TryGetConnectionInfo(archive, out PlayerConnectionInfo info) && info.SteamId == steamId;
        }

        private static bool ShouldDeferDisconnectForPersistence(ulong steamId)
        {
            return ModConfig.EnablePersistence.Value
                   && MimesisSaveManager.IsHost()
                   && steamId != GameSessionAccess.GetLocalSteamId();
        }

        private static DisconnectState GetOrCreateDisconnectState(ulong steamId, SpeechEventArchive? archive, long playerUid)
        {
            if (steamId != 0 && _disconnectBySteam.TryGetValue(steamId, out DisconnectState? bySteam))
            {
                if (archive != null)
                {
                    bySteam.Archive ??= archive;
                }

                if (playerUid != 0)
                {
                    bySteam.PlayerUid = playerUid;
                }

                bySteam.SteamId = steamId;
                return bySteam;
            }

            if (steamId == 0 && _disconnectBySteam.Count == 1)
            {
                KeyValuePair<ulong, DisconnectState> pending = _disconnectBySteam.First();
                pending.Value.Archive ??= archive;
                if (playerUid != 0)
                {
                    pending.Value.PlayerUid = playerUid;
                }

                if (steamId != 0)
                {
                    pending.Value.SteamId = steamId;
                }

                return pending.Value;
            }

            DisconnectState state = new()
            {
                Archive = archive,
                SteamId = steamId,
                PlayerUid = playerUid,
            };

            if (steamId != 0)
            {
                _disconnectBySteam[steamId] = state;
            }

            return state;
        }

        private static void TryFlushDisconnect(DisconnectState state)
        {
            if (state.DisconnectedLogged)
            {
                return;
            }

            state.DisconnectedLogged = true;
            LogLifecycle(
                "disconnected",
                state.Archive,
                OrderedContributions(state.Contributions),
                state.SteamId,
                state.PlayerUid);

            if (state.SteamId != 0)
            {
                _ = _disconnectBySteam.Remove(state.SteamId);
            }
        }

        private static bool HasMinimumIdentity(ConnectState state)
        {
            if (state.SteamId != 0)
            {
                return true;
            }

            return state.Archive != null
                   && VoiceEventStats.TryGetConnectionInfo(state.Archive, out PlayerConnectionInfo info)
                   && (info.SteamId != 0 || info.PlayerUid != 0);
        }

        private static void TryFlushConnect(ConnectState state)
        {
            if (state.PersistencePhase == PersistenceConnectPhase.Connecting)
            {
                if (!state.ConnectingLogged && HasMinimumIdentity(state))
                {
                    state.ConnectingLogged = true;
                    LogLifecycle("connecting", state.Archive, OrderedContributions(state.Contributions), state.SteamId);
                }

                return;
            }

            if (state.ConnectedLogged || SpeechEventPoolManager.AwaitingVoiceUuid(state.Archive))
            {
                return;
            }

            if (ModConfig.EnablePersistence.Value
                && state.PersistencePhase == PersistenceConnectPhase.Connected
                && !state.Contributions.ContainsKey("Persistence"))
            {
                return;
            }

            if (!HasMinimumIdentity(state))
            {
                return;
            }

            state.ConnectedLogged = true;
            LogLifecycle("connected", state.Archive, OrderedContributions(state.Contributions), state.SteamId);
        }

        private static List<PlayerLifecycleContribution> OrderedContributions(
            Dictionary<string, PlayerLifecycleContribution> contributions)
        {
            List<PlayerLifecycleContribution> list = [];
            foreach (string key in ContributionOrder)
            {
                if (contributions.TryGetValue(key, out PlayerLifecycleContribution contribution))
                {
                    list.Add(contribution);
                }
            }

            return list;
        }

        private static void LogLifecycle(
            string verb,
            SpeechEventArchive? archive,
            IReadOnlyList<PlayerLifecycleContribution> contributions,
            ulong steamIdFallback = 0,
            long playerUidFallback = 0)
        {
            string identity = ResolveLifecycleIdentity(archive, steamIdFallback, playerUidFallback);
            string suffix = FormatContributions(contributions);
            ModLog.Info(LogFeature, string.IsNullOrEmpty(suffix)
                ? $"Player {verb} — {identity}"
                : $"Player {verb} — {identity} — {suffix}");
        }

        private static string ResolveLifecycleIdentity(
            SpeechEventArchive? archive,
            ulong steamIdFallback,
            long playerUidFallback = 0)
        {
            if (steamIdFallback != 0)
            {
                return VoiceEventStats.DescribeSteamPlayer(steamIdFallback, playerUidFallback);
            }

            if (archive != null
                && VoiceEventStats.TryGetConnectionInfo(archive, out PlayerConnectionInfo info)
                && (info.SteamId != 0 || info.PlayerUid != 0))
            {
                return VoiceEventStats.DescribePlayer(archive);
            }

            return archive != null
                ? VoiceEventStats.DescribePlayer(archive)
                : VoiceEventStats.DescribeSteamPlayer(steamIdFallback, playerUidFallback);
        }

        private static string FormatContributions(IReadOnlyList<PlayerLifecycleContribution> contributions)
        {
            if (contributions.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            foreach (PlayerLifecycleContribution c in contributions)
            {
                if (string.IsNullOrEmpty(c.Detail))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    _ = sb.Append("; ");
                }

                _ = sb.Append(c.Feature).Append(": ").Append(c.Detail);
            }

            return sb.ToString();
        }
    }
}
