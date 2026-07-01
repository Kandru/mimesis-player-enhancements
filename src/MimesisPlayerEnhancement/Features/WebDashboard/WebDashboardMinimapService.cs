using System;
using System.Collections.Generic;
using System.Reflection;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;
using Mimic.Actors;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapService
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? BgRootField =
            typeof(GameMainBase).GetField("BGRoot", InstanceFlags);

        private static readonly FieldInfo? TramConsoleField =
            typeof(GameMainBase).GetField("tramConsole", InstanceFlags);

        internal static List<WebDashboardMinimapMarkerDto> CollectRawMarkers()
        {
            List<WebDashboardMinimapMarkerDto> markers = [];

            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                if (main == null)
                {
                    return markers;
                }

                Dictionary<int, ProtoActor>? map = main.GetProtoActorMap();
                if (map == null)
                {
                    return markers;
                }

                DungeonRoom? dungeonRoom = JoinAnytimeRoomTools.GetActiveDungeonRoom() as DungeonRoom;

                List<ProtoActor?> actors = [.. map.Values];
                foreach (ProtoActor? actor in actors)
                {
                    if (actor == null || actor.ActorType != ActorType.Player)
                    {
                        continue;
                    }

                    ulong steamId = StatisticsTracker.TryResolveSteamId(actor);
                    if (steamId == 0)
                    {
                        continue;
                    }

                    Transform transform = actor.transform;
                    Vector3 position = transform.position;
                    float x = position.x;
                    float z = position.z;
                    float yaw = ResolveHorizontalYaw(transform);

                    string areaId = WebDashboardMinimapAreaResolver.HubAreaId;
                    string tileId = string.Empty;
                    string roomName = string.Empty;

                    if (main is GamePlayScene gps && dungeonRoom != null)
                    {
                        areaId = WebDashboardMinimapAreaResolver.ResolvePlayerAreaId(gps, dungeonRoom, position) ?? string.Empty;
                        roomName = SpawnScalingRoomLookup.TryGetRoomName(dungeonRoom, position);
                        tileId = TryResolveTileId(dungeonRoom, position, areaId);
                    }
                    else if (WebDashboardMinimapTramSpace.IsWaitingRoom(main))
                    {
                        WebDashboardMinimapTramSpace.WorldToLocal(main, transform, out x, out z, out yaw);
                        areaId = string.Empty;
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
                        IsAlive = !actor.dead,
                        IsHost = WebDashboardGameState.IsHost() && LocalPlayerHelper.IsLocalSteamId(steamId),
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
            out WebDashboardMinimapTrainDto? train)
        {
            List<WebDashboardMinimapMarkerDto> raw = CollectRawMarkers();
            WebDashboardMinimapLayoutDto layout = WebDashboardMinimapLayoutBuilder.Current;
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            WebDashboardMinimapTrainDto? rawTrain = TryCollectTrain(pdata?.main, layout);
            List<WebDashboardMinimapMarkerDto> normalized = [];

            foreach (WebDashboardMinimapMarkerDto marker in raw)
            {
                EnrichFromPlayers(marker, players);
                WebDashboardMinimapBoundsDto bounds = ResolveMarkerBounds(layout, marker);
                normalized.Add(NormalizeMarker(marker, bounds));
            }

            WebDashboardMinimapAreaDto? outdoorArea =
                TryGetArea(layout, WebDashboardMinimapAreaResolver.OutdoorAreaId);
            train = NormalizeTrainForLayout(rawTrain, layout, outdoorArea);
            return normalized;
        }

        private static WebDashboardMinimapTrainDto? NormalizeTrainForLayout(
            WebDashboardMinimapTrainDto? rawTrain,
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapAreaDto? outdoorArea)
        {
            if (rawTrain == null)
            {
                return null;
            }

            if (outdoorArea != null)
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
            ulong focusSteamId,
            bool showAll,
            bool isHost)
        {
            if (showAll)
            {
                if (!isHost)
                {
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

            if (focusSteamId == 0)
            {
                foreach (WebDashboardMinimapMarkerDto marker in markers)
                {
                    if (marker.IsLocal)
                    {
                        return [marker];
                    }
                }

                return markers.Count > 0 ? [markers[0]] : [];
            }

            foreach (WebDashboardMinimapMarkerDto marker in markers)
            {
                if (marker.SteamId == focusSteamId)
                {
                    return [marker];
                }
            }

            return [];
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

            return new WebDashboardMinimapTrainDto
            {
                X = NormalizeCoord(train.X, bounds.MinX, bounds.MaxX),
                Z = NormalizeCoord(train.Z, bounds.MinZ, bounds.MaxZ),
                Yaw = train.Yaw,
                AreaId = areaId,
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
            if (main is InTramWaitingScene or DeathMatchScene or MaintenanceScene)
            {
                return null;
            }

            if (main is GamePlayScene)
            {
                WebDashboardMinimapAreaDto? outdoorArea =
                    TryGetArea(layout, WebDashboardMinimapAreaResolver.OutdoorAreaId);
                return outdoorArea != null ? TryFindSceneTrainMarker(main) : null;
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

        private static WebDashboardMinimapTrainDto? TryFindSceneTrainMarker(GameMainBase main)
        {
            try
            {
                Transform? root = BgRootField?.GetValue(main) as Transform;
                if (root == null && TramConsoleField?.GetValue(main) is Component console)
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

                Vector3 position = root.position;
                return new WebDashboardMinimapTrainDto
                {
                    X = position.x,
                    Z = position.z,
                    Yaw = ResolveHorizontalYaw(root),
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
