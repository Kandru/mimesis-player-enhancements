using System;
using System.Collections.Generic;
using System.Threading;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSnapshotCache
    {
        private const int FullRefreshIntervalMs = 1000;
        private const int MinimapRefreshIntervalMs = 100;

        private static WebDashboardSnapshot _snapshot = new();
        private static int _version;
        private static volatile bool _dirty = true;
        private static bool _lastConnected;
        private static long _lastFullRefreshMs;
        private static long _lastMinimapRefreshMs;
        private static string _minimapFingerprint = "";
        private static List<WebDashboardPlayerDto> _lastPlayers = [];
        private static string? _lastLeaderboardJson;
        private static List<ulong> _lastConnectedSteamIds = [];

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
            bool connected = WebDashboardGameState.IsConnected();
            if (connected != _lastConnected)
            {
                _dirty = true;
                _lastConnected = connected;
                _minimapFingerprint = "";
            }

            if (connected)
            {
                long nowMs = UtcNowMs();
                if (nowMs - _lastMinimapRefreshMs >= MinimapRefreshIntervalMs)
                {
                    RefreshMinimapLive();
                    _lastMinimapRefreshMs = nowMs;
                }

                if (_dirty || nowMs - _lastFullRefreshMs >= FullRefreshIntervalMs)
                {
                    Refresh(listenUrl);
                    _dirty = false;
                    _lastFullRefreshMs = nowMs;
                }

                return;
            }

            if (!_dirty)
            {
                return;
            }

            Refresh(listenUrl);
            _dirty = false;
            _lastFullRefreshMs = UtcNowMs();
            _minimapFingerprint = "";
        }

        internal static void RefreshMinimapLive()
        {
            if (!WebDashboardGameState.IsConnected())
            {
                return;
            }

            List<WebDashboardPlayerDto> players = _lastPlayers;
            if (players.Count == 0)
            {
                players = WebDashboardPlayerService.CollectPlayers();
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
                Players = previous.Players,
                LeaderboardJson = previous.LeaderboardJson,
                ConnectedSteamIds = previous.ConnectedSteamIds,
                PlayerStatsJson = previous.PlayerStatsJson,
                MinimapLayout = layout,
                MinimapMarkers = markers,
                MinimapTrain = train,
            };

            _ = Interlocked.Exchange(ref _snapshot, next);
            WebDashboardSseHub.NotifyMinimapChanged();
        }

        internal static void Refresh(string listenUrl)
        {
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
                    ModVersion = VersionInfo.ModuleVersion,
                    ListenUrl = listenUrl,
                    SnapshotVersion = Version,
                    ConfigVersion = ModConfig.Version,
                },
            };

            if (!connected)
            {
                _lastPlayers = [];
                _lastLeaderboardJson = null;
                _lastConnectedSteamIds = [];
                _minimapFingerprint = "";
                WebDashboardAvatarService.Clear();
            }
            else
            {
                List<WebDashboardPlayerDto> players = WebDashboardPlayerService.CollectPlayers();
                if (players.Count > 0)
                {
                    _lastPlayers = players;
                }
                else if (_lastPlayers.Count > 0)
                {
                    players = _lastPlayers;
                }

                next.Players = players;

                HashSet<ulong> avatarSteamIds = [];
                foreach (WebDashboardPlayerDto player in players)
                {
                    if (player.SteamId != 0)
                    {
                        _ = avatarSteamIds.Add(player.SteamId);
                    }
                }

                if (isHost && saveSlotId >= 0)
                {
                    List<ulong> connectedSteamIds = WebDashboardStatisticsBridge.GetConnectedSteamIds();
                    if (connectedSteamIds.Count > 0)
                    {
                        _lastConnectedSteamIds = connectedSteamIds;
                    }
                    else if (_lastConnectedSteamIds.Count > 0)
                    {
                        connectedSteamIds = _lastConnectedSteamIds;
                    }

                    next.ConnectedSteamIds = connectedSteamIds;
                    LeaderboardDocument? leaderboard = WebDashboardStatisticsBridge.GetLeaderboardDocument(saveSlotId);
                    string? leaderboardJson = leaderboard == null
                        ? null
                        : WebDashboardJson.SerializeLeaderboardResponse(leaderboard, connectedSteamIds);
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

                    if (leaderboard?.Entries != null)
                    {
                        foreach (LeaderboardEntry entry in leaderboard.Entries)
                        {
                            if (entry.SteamId == 0)
                            {
                                continue;
                            }

                            _ = avatarSteamIds.Add(entry.SteamId);

                            string? statsJson = WebDashboardStatisticsBridge.BuildPlayerStatsJson(
                                saveSlotId,
                                entry.SteamId,
                                leaderboard);
                            if (!string.IsNullOrEmpty(statsJson))
                            {
                                next.PlayerStatsJson[entry.SteamId] = statsJson;
                            }
                        }
                    }
                }

                WebDashboardAvatarService.PrewarmForPlayers([.. avatarSteamIds]);

                WebDashboardMinimapLayoutBuilder.EnsureLayout();
                next.MinimapLayout = WebDashboardMinimapLayoutBuilder.Current;
                next.MinimapMarkers = WebDashboardMinimapService.CollectMarkers(players, out WebDashboardMinimapTrainDto? train);
                next.MinimapTrain = train;
                _minimapFingerprint = BuildMinimapFingerprint(next.MinimapMarkers, train);
            }

            _ = Interlocked.Exchange(ref _snapshot, next);
            _ = Interlocked.Increment(ref _version);
            WebDashboardSseHub.NotifySnapshotChanged();
        }

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
