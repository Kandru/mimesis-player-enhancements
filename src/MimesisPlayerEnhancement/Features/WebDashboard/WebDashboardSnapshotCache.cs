using System;
using System.Collections.Generic;
using System.Threading;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSnapshotCache
    {
        private const int FullRefreshIntervalMs = 1000;
        private const int MinDirtyRefreshMs = 500;
        private const int MinimapRefreshIntervalMs = 250;
        private const int TickIntervalMs = 100;

        private static WebDashboardSnapshot _snapshot = new();
        private static int _version;
        private static volatile bool _dirty = true;
        private static bool _lastConnected;
        private static long _lastTickMs;
        private static long _lastFullRefreshMs;
        private static long _lastMinimapRefreshMs;
        private static string _minimapFingerprint = "";
        private static List<WebDashboardPlayerDto> _lastLivePlayers = [];
        private static string? _lastLeaderboardJson;
        private static List<ulong> _lastConnectedSteamIds = [];
        private static int _lastSeenStatisticsRevision = -1;

        internal static int Version => Volatile.Read(ref _version);

        internal static WebDashboardSnapshot Get()
        {
            return _snapshot;
        }

        internal static void MarkDirty()
        {
            _dirty = true;
        }

        internal static void Tick(string listenUrl)
        {
            long tickNowMs = UtcNowMs();
            if (tickNowMs - _lastTickMs < TickIntervalMs)
            {
                return;
            }

            _lastTickMs = tickNowMs;

            bool connected = WebDashboardGameState.IsConnected();
            if (connected != _lastConnected)
            {
                _dirty = true;
                _lastConnected = connected;
                _minimapFingerprint = "";
            }

            if (connected)
            {
                if (ModConfig.EnableStatistics.Value
                    && WebDashboardGameState.IsHost()
                    && StatisticsTracker.Revision != _lastSeenStatisticsRevision)
                {
                    _dirty = true;
                }

                long nowMs = tickNowMs;
                if (nowMs - _lastMinimapRefreshMs >= MinimapRefreshIntervalMs)
                {
                    RefreshMinimapLive();
                    _lastMinimapRefreshMs = nowMs;
                }

                if (ShouldRunFullRefresh(nowMs))
                {
                    Refresh(listenUrl, nowMs);
                    _dirty = false;
                    _lastFullRefreshMs = nowMs;
                    _lastSeenStatisticsRevision = StatisticsTracker.Revision;
                }

                return;
            }

            long idleNowMs = UtcNowMs();
            string lobbyName = WebDashboardGameState.GetLobbyName();
            if (!string.IsNullOrEmpty(lobbyName))
            {
                if (lobbyName != _snapshot.Status.LobbyName)
                {
                    _dirty = true;
                }
                else if (idleNowMs - _lastFullRefreshMs >= FullRefreshIntervalMs)
                {
                    _dirty = true;
                }
            }

            if (!ShouldRunFullRefresh(idleNowMs))
            {
                return;
            }

            Refresh(listenUrl, idleNowMs);
            _dirty = false;
            _lastFullRefreshMs = idleNowMs;
            _minimapFingerprint = "";
        }

        private static bool ShouldRunFullRefresh(long nowMs)
        {
            if (!_dirty && nowMs - _lastFullRefreshMs < FullRefreshIntervalMs)
            {
                return false;
            }

            if (nowMs - _lastFullRefreshMs >= FullRefreshIntervalMs)
            {
                return true;
            }

            return _dirty && nowMs - _lastFullRefreshMs >= MinDirtyRefreshMs;
        }

        internal static void RefreshMinimapLive()
        {
            if (!WebDashboardGameState.IsConnected())
            {
                return;
            }

            List<WebDashboardPlayerDto> players = _lastLivePlayers;
            if (players.Count == 0)
            {
                players = WebDashboardPlayerService.CollectLivePlayers();
            }

            WebDashboardMinimapLayoutBuilder.EnsureLayout();
            List<WebDashboardMinimapMarkerDto> markers =
                WebDashboardMinimapService.CollectMarkers(players, out WebDashboardMinimapTrainDto? train);
            string fingerprint = BuildMinimapFingerprint(markers, train);
            if (fingerprint == _minimapFingerprint
                && WebDashboardMinimapLayoutBuilder.LayoutVersion == _snapshot.MinimapLayout.LayoutVersion)
            {
                return;
            }

            _minimapFingerprint = fingerprint;
            WebDashboardMinimapLayoutDto layout = WebDashboardMinimapLayoutBuilder.Current;
            WebDashboardSnapshot previous = _snapshot;
            WebDashboardSnapshot next = new()
            {
                Status = previous.Status,
                Players = ClonePlayers(previous.Players),
                LeaderboardJson = previous.LeaderboardJson,
                MinimapLayout = layout,
                MinimapMarkers = CloneMarkers(markers),
                MinimapTrain = train,
            };

            _ = Interlocked.Exchange(ref _snapshot, next);
            WebDashboardSseHub.NotifyMinimapChanged();
        }

        internal static void Refresh(string listenUrl, long nowMs = 0)
        {
            if (nowMs <= 0)
            {
                nowMs = UtcNowMs();
            }

            bool connected = WebDashboardGameState.IsConnected();
            bool isHost = WebDashboardGameState.IsHost();
            int saveSlotId = WebDashboardGameState.GetSaveSlotId();
            _lastConnected = connected;

            WebDashboardSnapshot next = new()
            {
                Status = new WebDashboardStatusDto
                {
                    IsConnected = connected,
                    IsHost = isHost,
                    SaveSlotId = saveSlotId,
                    LobbyName = WebDashboardGameState.GetLobbyName(),
                    ModVersion = VersionInfo.ModuleVersion,
                    ListenUrl = listenUrl,
                    SnapshotVersion = Version,
                    ConfigVersion = ModConfig.Version,
                },
            };

            if (!connected)
            {
                _lastLivePlayers = [];
                _lastLeaderboardJson = null;
                _lastConnectedSteamIds = [];
                _minimapFingerprint = "";
                WebDashboardAvatarService.Clear();
                WebDashboardLeaderboardCache.Clear();
                WebDashboardOfflinePlayerCache.Clear();
                WebDashboardSnapshotEventCache.Clear();
                WebDashboardPlayerNameStore.Clear();
            }
            else
            {
                if (isHost && saveSlotId >= 0 && ModConfig.EnableStatistics.Value)
                {
                    WebDashboardOfflinePlayerCache.EnsureFresh(StatisticsTracker.Revision);
                }

                List<WebDashboardPlayerDto> livePlayers = WebDashboardPlayerService.CollectLivePlayers();
                if (livePlayers.Count > 0)
                {
                    _lastLivePlayers = livePlayers;
                }
                else if (_lastLivePlayers.Count > 0)
                {
                    livePlayers = _lastLivePlayers;
                }

                List<WebDashboardPlayerDto> players = WebDashboardPlayerService.MergePlayerLists(
                    livePlayers,
                    WebDashboardOfflinePlayerCache.GetCached());
                next.Players = players;

                HashSet<ulong> avatarSteamIds = [];
                foreach (WebDashboardPlayerDto player in livePlayers)
                {
                    if (player.SteamId != 0)
                    {
                        _ = avatarSteamIds.Add(player.SteamId);
                    }
                }

                if (isHost && saveSlotId >= 0)
                {
                    List<ulong> connectedSteamIds = CollectConnectedSteamIds(livePlayers);
                    if (connectedSteamIds.Count > 0)
                    {
                        _lastConnectedSteamIds = connectedSteamIds;
                    }
                    else if (_lastConnectedSteamIds.Count > 0)
                    {
                        connectedSteamIds = _lastConnectedSteamIds;
                    }

                    string? leaderboardJson = WebDashboardLeaderboardCache.UpdateAndGetCached(
                        saveSlotId,
                        connectedSteamIds);
                    if (!string.IsNullOrEmpty(leaderboardJson))
                    {
                        _lastLeaderboardJson = leaderboardJson;
                    }
                    else if (!string.IsNullOrEmpty(_lastLeaderboardJson))
                    {
                        leaderboardJson = _lastLeaderboardJson;
                    }

                    next.LeaderboardJson = leaderboardJson;

                    foreach (ulong steamId in connectedSteamIds)
                    {
                        if (steamId != 0)
                        {
                            _ = avatarSteamIds.Add(steamId);
                        }
                    }

                    foreach (ulong steamId in WebDashboardLeaderboardCache.GetCachedLeaderboardSteamIds())
                    {
                        if (steamId != 0)
                        {
                            _ = avatarSteamIds.Add(steamId);
                        }
                    }
                }

                WebDashboardAvatarService.PrewarmForPlayers([.. avatarSteamIds]);

                if (nowMs - _lastMinimapRefreshMs < MinimapRefreshIntervalMs
                    && _snapshot.MinimapMarkers.Count > 0)
                {
                    next.MinimapLayout = _snapshot.MinimapLayout;
                    next.MinimapMarkers = CloneMarkers(_snapshot.MinimapMarkers);
                    next.MinimapTrain = _snapshot.MinimapTrain;
                }
                else
                {
                    WebDashboardMinimapLayoutBuilder.EnsureLayout();
                    next.MinimapLayout = WebDashboardMinimapLayoutBuilder.Current;
                    List<WebDashboardMinimapMarkerDto> markers =
                        WebDashboardMinimapService.CollectMarkers(livePlayers, out WebDashboardMinimapTrainDto? train);
                    next.MinimapMarkers = CloneMarkers(markers);
                    next.MinimapTrain = train;
                    _minimapFingerprint = BuildMinimapFingerprint(markers, train);
                    _lastMinimapRefreshMs = nowMs;
                }
            }

            _ = Interlocked.Exchange(ref _snapshot, next);
            int newVersion = Interlocked.Increment(ref _version);
            WebDashboardSnapshotEventCache.ScheduleBuild(next, newVersion);
            WebDashboardSseHub.NotifySnapshotChanged();
        }

        private static List<ulong> CollectConnectedSteamIds(IReadOnlyList<WebDashboardPlayerDto> livePlayers)
        {
            if (!ModConfig.EnableStatistics.Value)
            {
                List<ulong> ids = [];
                foreach (WebDashboardPlayerDto player in livePlayers)
                {
                    if (player.SteamId != 0)
                    {
                        ids.Add(player.SteamId);
                    }
                }

                return ids;
            }

            return [.. StatisticsTracker.GetConnectedSteamIds()];
        }

        private static List<WebDashboardPlayerDto> ClonePlayers(IReadOnlyList<WebDashboardPlayerDto> players) =>
            players.Count == 0 ? [] : [.. players];

        private static List<ulong> CloneSteamIds(IReadOnlyList<ulong> steamIds) =>
            steamIds.Count == 0 ? [] : [.. steamIds];

        private static List<WebDashboardMinimapMarkerDto> CloneMarkers(
            IReadOnlyList<WebDashboardMinimapMarkerDto> markers) =>
            markers.Count == 0 ? [] : [.. markers];

        private static string BuildMinimapFingerprint(
            IReadOnlyList<WebDashboardMinimapMarkerDto> markers,
            WebDashboardMinimapTrainDto? train)
        {
            System.Text.StringBuilder sb = new();
            _ = sb.Append(WebDashboardMinimapLayoutBuilder.LayoutVersion).Append('|');
            if (train != null)
            {
                _ = sb.Append(train.AreaId)
                    .Append('|')
                    .Append(train.X.ToString("F3"))
                    .Append(',')
                    .Append(train.Z.ToString("F3"))
                    .Append(',')
                    .Append(train.Yaw.ToString("F1"))
                    .Append('|');
            }

            foreach (WebDashboardMinimapMarkerDto marker in markers)
            {
                _ = sb.Append(marker.SteamId)
                    .Append(':')
                    .Append(marker.X.ToString("F3"))
                    .Append(',')
                    .Append(marker.Z.ToString("F3"))
                    .Append(',')
                    .Append(marker.Yaw.ToString("F1"))
                    .Append(',')
                    .Append(marker.AreaId)
                    .Append(',')
                    .Append(marker.IsAlive ? '1' : '0')
                    .Append(';');
            }

            return sb.ToString();
        }

        private static long UtcNowMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
