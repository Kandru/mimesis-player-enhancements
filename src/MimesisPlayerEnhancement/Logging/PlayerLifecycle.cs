using System.Text;
using MimesisPlayerEnhancement.Features.MoreVoices;
using Mimic.Voice.SpeechSystem;

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
            PlayerLifecycleContribution? moreVoices = MoreVoicesPatches.TryDescribeArchiveStarted(archive);
            if (moreVoices.HasValue)
            {
                state.Contributions[moreVoices.Value.Feature] = moreVoices.Value;
            }

            if (state.SteamId != 0 && _pendingStatisticsConnect.Remove(state.SteamId, out PlayerLifecycleContribution stats))
            {
                state.Contributions[stats.Feature] = stats;
            }

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

            if (_connectBySteam.TryGetValue(steamId, out ConnectState? state))
            {
                state.Contributions[contribution.Value.Feature] = contribution.Value;
                if (!state.ConnectedLogged)
                {
                    TryFlushConnect(state);
                }

                return;
            }

            _pendingStatisticsConnect[steamId] = contribution.Value;
        }

        internal static void OnArchiveDisconnecting(SpeechEventArchive archive, PlayerLifecycleContribution? contribution)
        {
            if (archive == null)
            {
                return;
            }

            _ = VoiceEventStats.TryGetConnectionInfo(archive, out PlayerConnectionInfo info);
            if (!_disconnectBySteam.TryGetValue(info.SteamId, out DisconnectState? state))
            {
                state = new DisconnectState { Archive = archive };
                _disconnectBySteam[info.SteamId] = state;
            }

            state.Archive ??= archive;
            if (contribution.HasValue)
            {
                state.Contributions[contribution.Value.Feature] = contribution.Value;
            }

            LogLifecycle("disconnecting", archive, OrderedContributions(state.Contributions));
        }

        internal static void NotifyStatisticsDisconnect(ulong steamId, PlayerLifecycleContribution? contribution)
        {
            if (steamId == 0)
            {
                return;
            }

            if (!_disconnectBySteam.TryGetValue(steamId, out DisconnectState? state))
            {
                state = new DisconnectState();
                _disconnectBySteam[steamId] = state;
            }

            if (contribution.HasValue)
            {
                state.Contributions[contribution.Value.Feature] = contribution.Value;
            }

            LogLifecycle("disconnected", state.Archive, OrderedContributions(state.Contributions), steamId);
            _ = _disconnectBySteam.Remove(steamId);
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
                state.SteamId = info.SteamId;
                _connectBySteam[state.SteamId] = state;
            }
        }

        private static void TryFlushConnect(ConnectState state)
        {
            if (state.PersistencePhase == PersistenceConnectPhase.Connecting)
            {
                if (!state.ConnectingLogged)
                {
                    state.ConnectingLogged = true;
                    LogLifecycle("connecting", state.Archive, OrderedContributions(state.Contributions));
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

            state.ConnectedLogged = true;
            LogLifecycle("connected", state.Archive, OrderedContributions(state.Contributions));
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
            ulong steamIdFallback = 0)
        {
            string identity = archive != null
                ? VoiceEventStats.DescribePlayer(archive)
                : VoiceEventStats.DescribeSteamPlayer(steamIdFallback);
            string suffix = FormatContributions(contributions);
            ModLog.Info(LogFeature, string.IsNullOrEmpty(suffix)
                ? $"Player {verb} — {identity}"
                : $"Player {verb} — {identity} — {suffix}");
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
