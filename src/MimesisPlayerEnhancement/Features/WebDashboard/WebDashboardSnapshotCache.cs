using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSnapshotCache
    {
        private const int FullRefreshIntervalMs = 1000;
        private const int LargeRosterRefreshIntervalMs = 2000;
        private const int LargeRosterPlayerThreshold = 64;
        private const int MinDirtyRefreshMs = 500;
        private const int MinimapRefreshIntervalMs = 100;
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
        private static List<WebDashboardPlayerDto> _cachedMergedPlayers = [];
        private static int _cachedOfflineRevision = -1;
        private static HashSet<ulong> _previousLiveSteamIds = [];
        private static string _lastPublishFingerprint = "";
        private static string _cachedOfflinePublishFingerprint = "";
        private static int _cachedOfflinePublishRevision = -1;
        private static Dictionary<ulong, int> _mergedIndexBySteam = [];
        private static bool _requireFullPublish;

        internal static void RequestFullPublish()
        {
            _requireFullPublish = true;
        }

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
                    _requireFullPublish = true;
                }

                long nowMs = tickNowMs;
                bool hasMinimapAudience = WebDashboardSseHub.HasMinimapClients;

                if (hasMinimapAudience && nowMs - _lastMinimapRefreshMs >= MinimapRefreshIntervalMs)
                {
                    RefreshMinimapLive();
                    _lastMinimapRefreshMs = nowMs;
                }

                if (!ShouldRunRefresh(nowMs))
                {
                    return;
                }

                Refresh(listenUrl, nowMs);
                _dirty = false;
                _lastFullRefreshMs = nowMs;
                _lastSeenStatisticsRevision = StatisticsTracker.Revision;

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

            if (!ShouldRunRefresh(idleNowMs))
            {
                return;
            }

            Refresh(listenUrl, idleNowMs);
            _dirty = false;
            _lastFullRefreshMs = idleNowMs;
            _minimapFingerprint = "";
        }

        private static bool ShouldRunRefresh(long nowMs)
        {
            if (_dirty)
            {
                return nowMs - _lastFullRefreshMs >= MinDirtyRefreshMs;
            }

            if (!WebDashboardSseHub.HasSnapshotClients)
            {
                return false;
            }

            return nowMs - _lastFullRefreshMs >= ResolveRefreshIntervalMs();
        }

        private static int ResolveRefreshIntervalMs()
        {
            return _cachedMergedPlayers.Count >= LargeRosterPlayerThreshold
                ? LargeRosterRefreshIntervalMs
                : FullRefreshIntervalMs;
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
                Players = previous.Players,
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
            int offlineRevision = WebDashboardOfflinePlayerCache.CachedRevision;

            WebDashboardSnapshot next = new()
            {
                Status = new WebDashboardStatusDto
                {
                    IsConnected = connected,
                    IsHost = isHost,
                    SaveSlotId = saveSlotId,
                    LobbyName = WebDashboardGameState.GetLobbyName(),
                    ModVersion = VersionInfo.ModuleVersion,
                    LastSeenModVersion = ModConfig.LastSeenModVersion.Value ?? string.Empty,
                    ListenUrl = listenUrl,
                    SnapshotVersion = Version,
                    ConfigVersion = ModConfig.Version,
                    JoinAnytimeRoutingCount = ModConfig.EnableJoinAnytime.Value
                        ? LateJoinRouteTracker.GetActiveRoutingCount()
                        : 0,
                    Locale = GameLocaleAccess.GetCurrentLanguage(),
                    SessionScene = WebDashboardSessionScene.Resolve(JoinAnytimeHub.GetPdata()?.main),
                    BlindModeEnabled = WebDashboardMinimapBlindMode.Enabled,
                },
            };

            if (!connected)
            {
                _lastLivePlayers = [];
                _lastLeaderboardJson = null;
                _lastConnectedSteamIds = [];
                _minimapFingerprint = "";
                _cachedMergedPlayers = [];
                _cachedOfflineRevision = -1;
                _previousLiveSteamIds = [];
                _lastPublishFingerprint = "";
                _cachedOfflinePublishFingerprint = "";
                _cachedOfflinePublishRevision = -1;
                _mergedIndexBySteam = [];
                _requireFullPublish = false;
                WebDashboardAvatarService.Clear();
                WebDashboardLeaderboardCache.Clear();
                WebDashboardOfflinePlayerCache.Clear();
                WebDashboardSnapshotEventCache.Clear();
                WebDashboardHostCheatsRuntime.DisableAll("disconnected");
                WebDashboardCatalogCache.Invalidate();
            }
            else
            {
                WebDashboardHostCheatsRuntime.SyncFromSession();
                WebDashboardCatalogCache.RefreshCatalogsIfNeeded(connected: true);
                WebDashboardCatalogCache.RefreshHostCheats();

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

                IReadOnlyList<WebDashboardPlayerDto> offlinePlayers = WebDashboardOfflinePlayerCache.GetCached();
                offlineRevision = WebDashboardOfflinePlayerCache.CachedRevision;
                bool needsFullMerge = _requireFullPublish
                    || _cachedMergedPlayers.Count == 0
                    || offlineRevision != _cachedOfflineRevision;
                if (needsFullMerge)
                {
                    _cachedMergedPlayers = WebDashboardPlayerListMerger.MergePlayerLists(livePlayers, offlinePlayers);
                    _mergedIndexBySteam = WebDashboardPlayerListMerger.BuildMergedIndex(_cachedMergedPlayers);
                }
                else
                {
                    _ = WebDashboardPlayerListMerger.ApplyLiveToMerged(
                        _cachedMergedPlayers,
                        _mergedIndexBySteam,
                        livePlayers,
                        offlinePlayers,
                        _previousLiveSteamIds);
                }

                _cachedOfflineRevision = offlineRevision;
                _previousLiveSteamIds = CollectLiveSteamIds(livePlayers);
                next.Players = _cachedMergedPlayers;

                HashSet<ulong> avatarSteamIds = [];
                foreach (WebDashboardPlayerDto player in livePlayers)
                {
                    if (player.SteamId != 0)
                    {
                        _ = avatarSteamIds.Add(player.SteamId);
                    }
                }

                if (isHost && saveSlotId >= 0 && (_requireFullPublish || _dirty))
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
                else if (isHost && saveSlotId >= 0)
                {
                    next.LeaderboardJson = _lastLeaderboardJson ?? _snapshot.LeaderboardJson;
                }

                if (WebDashboardSseHub.HasSnapshotClients)
                {
                    WebDashboardAvatarService.PrewarmForPlayers([.. avatarSteamIds]);
                }

                if (WebDashboardSseHub.HasMinimapClients)
                {
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
                else
                {
                    next.MinimapLayout = _snapshot.MinimapLayout;
                    next.MinimapMarkers = CloneMarkers(_snapshot.MinimapMarkers);
                    next.MinimapTrain = _snapshot.MinimapTrain;
                }
            }

            bool publishFull = _requireFullPublish;
            List<WebDashboardPlayerDto> liveForPublish = _lastLivePlayers.Count == 0 ? [] : [.. _lastLivePlayers];
            string publishFingerprint = publishFull
                ? BuildPublishFingerprint(next, _previousLiveSteamIds, offlineRevision)
                : BuildLivePublishFingerprint(next, liveForPublish);
            if (WebDashboardSseHub.HasSnapshotClients
                && publishFingerprint != _lastPublishFingerprint)
            {
                _lastPublishFingerprint = publishFingerprint;
                int newVersion = Interlocked.Increment(ref _version);
                next.Status.SnapshotVersion = newVersion;
                bool liveOnly = !publishFull;
                WebDashboardSnapshotEventCache.ScheduleBuild(
                    next,
                    newVersion,
                    liveOnly,
                    liveOnly ? liveForPublish : null);
                WebDashboardSseHub.NotifySnapshotChanged();
                if (publishFull)
                {
                    _requireFullPublish = false;
                }
            }

            _ = Interlocked.Exchange(ref _snapshot, next);
        }

        private static string BuildLivePublishFingerprint(
            WebDashboardSnapshot snapshot,
            IReadOnlyList<WebDashboardPlayerDto> livePlayers)
        {
            System.Text.StringBuilder sb = new();
            WebDashboardStatusDto status = snapshot.Status;
            _ = sb.Append(status.IsConnected ? '1' : '0')
                .Append(status.IsHost ? '1' : '0')
                .Append('|')
                .Append(status.SaveSlotId)
                .Append('|')
                .Append(status.ConfigVersion)
                .Append('|')
                .Append(status.JoinAnytimeRoutingCount)
                .Append('|')
                .Append(status.SessionScene)
                .Append('|')
                .Append(status.BlindModeEnabled ? '1' : '0')
                .Append('|')
                .Append(livePlayers.Count)
                .Append('|');

            foreach (WebDashboardPlayerDto player in livePlayers)
            {
                AppendPlayerFingerprint(sb, player, includePlaytimeBucket: true);
            }

            return sb.ToString();
        }

        private static HashSet<ulong> CollectLiveSteamIds(IReadOnlyList<WebDashboardPlayerDto> livePlayers)
        {
            HashSet<ulong> liveIds = new(livePlayers.Count);
            foreach (WebDashboardPlayerDto player in livePlayers)
            {
                if (player.SteamId != 0)
                {
                    _ = liveIds.Add(player.SteamId);
                }
            }

            return liveIds;
        }

        private static string BuildPublishFingerprint(
            WebDashboardSnapshot snapshot,
            IReadOnlyCollection<ulong> liveSteamIds,
            int offlineRevision)
        {
            System.Text.StringBuilder sb = new();
            WebDashboardStatusDto status = snapshot.Status;
            _ = sb.Append(status.IsConnected ? '1' : '0')
                .Append(status.IsHost ? '1' : '0')
                .Append('|')
                .Append(status.SaveSlotId)
                .Append('|')
                .Append(status.LobbyName)
                .Append('|')
                .Append(status.SessionScene)
                .Append('|')
                .Append(status.BlindModeEnabled ? '1' : '0')
                .Append('|')
                .Append(status.ConfigVersion)
                .Append('|')
                .Append(snapshot.LeaderboardJson?.Length ?? 0)
                .Append('|')
                .Append(snapshot.Players.Count)
                .Append('|');

            if (offlineRevision != _cachedOfflinePublishRevision)
            {
                System.Text.StringBuilder offline = new();
                HashSet<ulong> liveIds = liveSteamIds is HashSet<ulong> set ? set : [.. liveSteamIds];
                foreach (WebDashboardPlayerDto player in snapshot.Players)
                {
                    if (player.SteamId != 0 && liveIds.Contains(player.SteamId))
                    {
                        continue;
                    }

                    AppendPlayerFingerprint(offline, player, includePlaytimeBucket: false);
                }

                _cachedOfflinePublishFingerprint = offline.ToString();
                _cachedOfflinePublishRevision = offlineRevision;
            }

            _ = sb.Append(_cachedOfflinePublishFingerprint).Append('|');

            HashSet<ulong> liveLookup = liveSteamIds is HashSet<ulong> liveSet ? liveSet : [.. liveSteamIds];
            foreach (WebDashboardPlayerDto player in snapshot.Players)
            {
                if (player.SteamId == 0 || !liveLookup.Contains(player.SteamId))
                {
                    continue;
                }

                AppendPlayerFingerprint(sb, player, includePlaytimeBucket: true);
            }

            return sb.ToString();
        }

        private static void AppendPlayerFingerprint(
            System.Text.StringBuilder sb,
            WebDashboardPlayerDto player,
            bool includePlaytimeBucket)
        {
            _ = sb.Append(player.SteamId)
                .Append(':')
                .Append(player.PlayerUid)
                .Append(':')
                .Append(player.DisplayName)
                .Append(':')
                .Append(player.IsHost ? '1' : '0')
                .Append(':')
                .Append(player.IsLocal ? '1' : '0')
                .Append(':')
                .Append(player.IsBanned ? '1' : '0')
                .Append(':')
                .Append(player.IsAlive ? '1' : '0')
                .Append(':')
                .Append(player.NetworkGrade)
                .Append(':')
                .Append(player.ConnectionRole)
                .Append(':')
                .Append(player.ConnectionAddress)
                .Append(':')
                .Append(player.VoiceLineCount)
                .Append(':')
                .Append(player.Health ?? -1)
                .Append(':')
                .Append(player.MaxHealth ?? -1)
                .Append(':')
                .Append(player.ToxicPercent?.ToString("F0") ?? "-")
                .Append(':')
                .Append(player.LateJoinPhase)
                .Append(':')
                .Append(player.LateJoinLabel)
                .Append(':')
                .Append(player.LateJoinStuckSeconds?.ToString("F1") ?? "-")
                .Append(':')
                .Append(player.LateJoinAttemptCount)
                .Append(':')
                .Append(player.ActivityState)
                .Append(':')
                .Append(player.ActivityDetail)
                .Append(':')
                .Append(player.GodMode ? '1' : '0')
                .Append(':')
                .Append(player.NoClip ? '1' : '0');

            WebDashboardSessionStatsDto? session = player.CurrentSession;
            if (session != null)
            {
                _ = sb.Append(':')
                    .Append(includePlaytimeBucket ? session.TotalConnectedSeconds / 10 : 0)
                    .Append(':')
                    .Append(session.CurrencyEarned)
                    .Append(':')
                    .Append(session.SurvivalDeaths)
                    .Append(':')
                    .Append(session.SurvivalWins)
                    .Append(':')
                    .Append(session.Revives)
                    .Append(':')
                    .Append(session.MimicEncounterCount)
                    .Append(':')
                    .Append(session.ItemCarryCount)
                    .Append(':')
                    .Append(session.DamageToFriend)
                    .Append(':')
                    .Append(session.FriendsKilled);
            }

            _ = sb.Append(';');
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
