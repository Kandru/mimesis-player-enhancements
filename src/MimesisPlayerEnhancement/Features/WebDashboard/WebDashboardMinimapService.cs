using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapService
    {
        internal static List<WebDashboardMinimapMarkerDto> CollectRawMarkers(WebDashboardLiveRoster? roster = null)
        {
            List<WebDashboardMinimapMarkerDto> markers = [];

            try
            {
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                GameMainBase? main = pdata?.main;
                if (main == null)
                {
                    return markers;
                }

                WebDashboardLiveRoster resolvedRoster = roster ?? WebDashboardLiveRoster.Capture();
                DungeonRoom? dungeonRoom = JoinAnytimeRoomTools.GetActiveDungeonRoom() as DungeonRoom;

                foreach (WebDashboardLivePlayer entry in resolvedRoster.Enumerate())
                {
                    ProtoActor actor = entry.Actor;
                    ulong steamId = entry.SteamId;
                    Transform transform = actor.transform;
                    float worldY = transform.position.y;
                    float x;
                    float z;
                    float yaw;

                    string areaId = WebDashboardMinimapAreaResolver.HubAreaId;
                    string tileId = string.Empty;
                    string roomName = string.Empty;
                    int floorIndex = 0;

                    if (main is GamePlayScene gps && dungeonRoom != null)
                    {
                        Vector3 position = transform.position;
                        x = position.x;
                        z = position.z;
                        yaw = ResolveHorizontalYaw(transform);
                        areaId = WebDashboardMinimapAreaResolver.ResolvePlayerAreaId(gps, dungeonRoom, position) ?? string.Empty;
                        roomName = SpawnScalingRoomLookup.TryGetRoomName(dungeonRoom, position);
                        tileId = TryResolveTileId(dungeonRoom, position, areaId);
                        floorIndex = WebDashboardMinimapFloorRegistry.ResolveFloorIndex(worldY, areaId);
                        if (WebDashboardMinimapAreaResolver.IsIndoorAreaId(areaId))
                        {
                            string? floorAreaId = WebDashboardMinimapFloorRegistry.TryGetAreaIdForFloor(floorIndex);
                            if (!string.IsNullOrWhiteSpace(floorAreaId))
                            {
                                areaId = floorAreaId;
                            }
                        }
                    }
                    else if (ShouldUseTramLocalCoords(main))
                    {
                        WebDashboardMinimapTramSpace.WorldToLocal(main, transform, out x, out z, out yaw);
                    }
                    else
                    {
                        Vector3 position = transform.position;
                        x = position.x;
                        z = position.z;
                        yaw = ResolveHorizontalYaw(transform);
                    }

                    markers.Add(new WebDashboardMinimapMarkerDto
                    {
                        SteamId = steamId,
                        DisplayName = string.IsNullOrWhiteSpace(actor.nickName) ? steamId.ToString() : actor.nickName,
                        X = x,
                        Z = z,
                        Yaw = yaw,
                        RoomName = roomName,
                        AreaId = areaId,
                        TileId = tileId,
                        FloorIndex = floorIndex,
                        IsAlive = !actor.dead,
                        IsHost = actor.IsHost,
                        IsLocal = LocalPlayerHelper.IsLocalSteamId(steamId),
                    });
                }
            }
            catch
            {
                /* scene may be transitioning */
            }

            return markers;
        }

        internal static List<WebDashboardMinimapMarkerDto> CollectMarkers(
            IReadOnlyList<WebDashboardPlayerDto> players,
            out WebDashboardMinimapTrainDto? train,
            WebDashboardLiveRoster? roster = null)
        {
            List<WebDashboardMinimapMarkerDto> raw = CollectRawMarkers(roster);
            WebDashboardMinimapLayoutDto layout = WebDashboardMinimapLayoutBuilder.Current;
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            WebDashboardMinimapTrainDto? rawTrain = TryCollectTrain(pdata?.main, layout);
            List<WebDashboardMinimapMarkerDto> normalized = [];

            foreach (WebDashboardMinimapMarkerDto marker in raw)
            {
                EnrichFromPlayers(marker, players);
                WebDashboardMinimapBoundsDto bounds = ResolveMarkerBounds(layout, marker);
                normalized.Add(NormalizeMarker(marker, bounds));
            }

            train = NormalizeTrainForLayout(rawTrain, layout);
            return normalized;
        }

        private static WebDashboardMinimapTrainDto? NormalizeTrainForLayout(
            WebDashboardMinimapTrainDto? rawTrain,
            WebDashboardMinimapLayoutDto layout)
        {
            if (rawTrain == null)
            {
                return null;
            }

            WebDashboardMinimapAreaDto? outdoorArea =
                TryGetArea(layout, WebDashboardMinimapAreaResolver.OutdoorAreaId);
            if (outdoorArea != null && rawTrain.AreaId == WebDashboardMinimapAreaResolver.OutdoorAreaId)
            {
                return NormalizeTrain(
                    rawTrain,
                    outdoorArea.Bounds,
                    WebDashboardMinimapAreaResolver.OutdoorAreaId);
            }

            WebDashboardMinimapAreaDto? hubArea =
                TryGetArea(layout, WebDashboardMinimapAreaResolver.HubAreaId);
            if (hubArea != null)
            {
                return NormalizeTrain(rawTrain, hubArea.Bounds, WebDashboardMinimapAreaResolver.HubAreaId);
            }

            return null;
        }

        internal static List<WebDashboardMinimapMarkerDto> FilterMarkers(
            IReadOnlyList<WebDashboardMinimapMarkerDto> markers,
            ulong focusSteamId = 0)
        {
            if (WebDashboardMinimapBlindMode.ShouldHideOtherPlayers())
            {
                List<WebDashboardMinimapMarkerDto> localOnly = [];
                foreach (WebDashboardMinimapMarkerDto marker in markers)
                {
                    if (marker.IsLocal && marker.IsAlive)
                    {
                        localOnly.Add(marker);
                    }
                }

                return localOnly;
            }

            if (focusSteamId != 0)
            {
                foreach (WebDashboardMinimapMarkerDto marker in markers)
                {
                    if (marker.SteamId == focusSteamId && marker.IsAlive)
                    {
                        return [marker];
                    }
                }

                return [];
            }

            List<WebDashboardMinimapMarkerDto> alive = [];
            foreach (WebDashboardMinimapMarkerDto marker in markers)
            {
                if (marker.IsAlive)
                {
                    alive.Add(marker);
                }
            }

            return alive;
        }

        internal static List<WebDashboardMinimapMarkerDto> FilterMarkersForClient(
            IReadOnlyList<WebDashboardMinimapMarkerDto> markers)
        {
            return FilterMarkers(markers);
        }

        internal static WebDashboardMinimapAreaDto? TryGetArea(
            WebDashboardMinimapLayoutDto layout,
            string areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
            {
                return null;
            }

            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                if (area.Id == areaId)
                {
                    return area;
                }
            }

            return null;
        }

        internal static WebDashboardMinimapTrainDto? NormalizeTrain(
            WebDashboardMinimapTrainDto? train,
            WebDashboardMinimapBoundsDto bounds,
            string areaId)
        {
            if (train == null || string.IsNullOrWhiteSpace(areaId) || IsPlaceholderBounds(bounds))
            {
                return null;
            }

            float spanX = Mathf.Max(bounds.MaxX - bounds.MinX, 1f);
            float spanZ = Mathf.Max(bounds.MaxZ - bounds.MinZ, 1f);

            return new WebDashboardMinimapTrainDto
            {
                X = NormalizeCoord(train.X, bounds.MinX, bounds.MaxX),
                Z = NormalizeCoord(train.Z, bounds.MinZ, bounds.MaxZ),
                Yaw = train.Yaw,
                AreaId = areaId,
                SpanX = train.SpanX > 0f ? train.SpanX / spanX : 0f,
                SpanZ = train.SpanZ > 0f ? train.SpanZ / spanZ : 0f,
            };
        }

        internal static WebDashboardMinimapBoundsDto ResolveEffectiveBounds(
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapTrainDto? rawTrain)
        {
            if (!IsPlaceholderBounds(layout.Bounds))
            {
                return layout.Bounds;
            }

            return BuildFallbackBounds(rawTrain);
        }

        internal static WebDashboardMinimapTrainDto? TryCollectTrain(
            GameMainBase? main,
            WebDashboardMinimapLayoutDto layout)
        {
            if (main is DeathMatchScene)
            {
                return null;
            }

            if (main is GamePlayScene)
            {
                WebDashboardMinimapAreaDto? outdoorArea =
                    TryGetArea(layout, WebDashboardMinimapAreaResolver.OutdoorAreaId);
                return outdoorArea != null ? TryFindSceneTrainMarker(main, WebDashboardMinimapAreaResolver.OutdoorAreaId) : null;
            }

            if (main != null
                && (main is InTramWaitingScene or MaintenanceScene
                    || layout.DisplayMode == "open"))
            {
                return TryFindSceneTrainMarker(main, WebDashboardMinimapAreaResolver.HubAreaId);
            }

            return null;
        }

        private static WebDashboardMinimapBoundsDto ResolveMarkerBounds(
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapMarkerDto marker)
        {
            WebDashboardMinimapAreaDto? area = TryGetArea(layout, marker.AreaId);
            if (area != null && !IsPlaceholderBounds(area.Bounds))
            {
                return area.Bounds;
            }

            if (!string.IsNullOrWhiteSpace(marker.AreaId))
            {
                return PlaceholderBounds();
            }

            return ResolveEffectiveBounds(layout, null);
        }

        private static WebDashboardMinimapBoundsDto PlaceholderBounds()
        {
            return new WebDashboardMinimapBoundsDto
            {
                MinX = 0f,
                MinZ = 0f,
                MaxX = 1f,
                MaxZ = 1f,
            };
        }

        private static string TryResolveTileId(DungeonRoom room, Vector3 position, string areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId)
                || areaId == WebDashboardMinimapAreaResolver.OutdoorAreaId
                || !WebDashboardMinimapAreaResolver.IsIndoorAreaId(areaId))
            {
                return string.Empty;
            }

            if (WebDashboardMinimapAreaResolver.TryGetIndoorTileGroup(room) is not ISpaceGroup spaceGroup)
            {
                return string.Empty;
            }

            try
            {
                IVSpace? space = spaceGroup.GetSpace(position);
                if (space?.Coordinate is TileCoordinate tileCoordinate)
                {
                    return $"tile-{tileCoordinate.TileID}";
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool ShouldUseTramLocalCoords(GameMainBase? main)
        {
            return WebDashboardMinimapTramSpace.IsWaitingRoom(main)
                || main is MaintenanceScene
                || WebDashboardMinimapLayoutBuilder.Current.DisplayMode == "open";
        }

        private static float ResolveHorizontalYaw(Transform transform)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return transform.eulerAngles.y;
            }

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private static WebDashboardMinimapTrainDto? TryFindSceneTrainMarker(GameMainBase main, string areaId)
        {
            try
            {
                if (WebDashboardMinimapHubBounds.TryGetTramLocalBounds(
                        main,
                        out float centerX,
                        out float centerZ,
                        out float spanX,
                        out float spanZ,
                        out float yaw))
                {
                    return new WebDashboardMinimapTrainDto
                    {
                        X = centerX,
                        Z = centerZ,
                        Yaw = yaw,
                        AreaId = areaId,
                        SpanX = spanX,
                        SpanZ = spanZ,
                    };
                }

                Transform? root = WebDashboardSceneRoots.TryGetBgRoot(main);
                if (root == null && WebDashboardSceneRoots.TryGetTramConsole(main) is Component console)
                {
                    root = console.transform;
                    while (root.parent != null && root.parent != root.root)
                    {
                        root = root.parent;
                    }
                }

                if (root == null)
                {
                    return null;
                }

                return new WebDashboardMinimapTrainDto
                {
                    X = 0f,
                    Z = 0f,
                    Yaw = 0f,
                    AreaId = areaId,
                };
            }
            catch
            {
                return null;
            }
        }

        private static WebDashboardMinimapBoundsDto BuildFallbackBounds(WebDashboardMinimapTrainDto? rawTrain)
        {
            const float halfSpan = 75f;
            const float padding = 0.05f;
            float centerX = rawTrain?.X ?? 0f;
            float centerZ = rawTrain?.Z ?? 0f;
            float pad = halfSpan * padding;

            return new WebDashboardMinimapBoundsDto
            {
                MinX = centerX - halfSpan - pad,
                MinZ = centerZ - halfSpan - pad,
                MaxX = centerX + halfSpan + pad,
                MaxZ = centerZ + halfSpan + pad,
            };
        }

        private static bool IsPlaceholderBounds(WebDashboardMinimapBoundsDto bounds)
        {
            return bounds.MinX == 0f
                && bounds.MinZ == 0f
                && bounds.MaxX == 1f
                && bounds.MaxZ == 1f;
        }

        private static void EnrichFromPlayers(WebDashboardMinimapMarkerDto marker, IReadOnlyList<WebDashboardPlayerDto> players)
        {
            foreach (WebDashboardPlayerDto player in players)
            {
                if (player.SteamId != marker.SteamId)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(player.DisplayName))
                {
                    marker.DisplayName = player.DisplayName;
                }

                marker.IsHost = player.IsHost;
                marker.IsLocal = player.IsLocal;
                return;
            }
        }

        private static WebDashboardMinimapMarkerDto NormalizeMarker(
            WebDashboardMinimapMarkerDto marker,
            WebDashboardMinimapBoundsDto bounds)
        {
            return new WebDashboardMinimapMarkerDto
            {
                SteamId = marker.SteamId,
                DisplayName = marker.DisplayName,
                X = NormalizeCoord(marker.X, bounds.MinX, bounds.MaxX),
                Z = NormalizeCoord(marker.Z, bounds.MinZ, bounds.MaxZ),
                Yaw = marker.Yaw,
                RoomName = marker.RoomName,
                AreaId = marker.AreaId,
                TileId = marker.TileId,
                FloorIndex = marker.FloorIndex,
                IsAlive = marker.IsAlive,
                IsHost = marker.IsHost,
                IsLocal = marker.IsLocal,
            };
        }

        private static float NormalizeCoord(float value, float min, float max)
        {
            float span = max - min;
            return span <= 0f ? 0.5f : Mathf.Clamp01((value - min) / span);
        }
    }
}
