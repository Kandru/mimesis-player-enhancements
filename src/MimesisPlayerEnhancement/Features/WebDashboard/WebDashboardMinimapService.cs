using System;
using System.Collections.Generic;
using System.Reflection;
using Mimic.Actors;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;
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

                foreach (ProtoActor? actor in map.Values)
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
                    float yaw = transform.eulerAngles.y;
                    if (WebDashboardMinimapTramSpace.IsWaitingRoom(main))
                    {
                        WebDashboardMinimapTramSpace.WorldToLocal(main, position, yaw, out x, out z, out yaw);
                    }

                    markers.Add(new WebDashboardMinimapMarkerDto
                    {
                        SteamId = steamId,
                        DisplayName = string.IsNullOrWhiteSpace(actor.nickName) ? steamId.ToString() : actor.nickName,
                        X = x,
                        Z = z,
                        Yaw = yaw,
                        RoomName = JoinAnytimeRoomTools.GetActiveDungeonRoom() is DungeonRoom dungeonRoom
                            ? SpawnScalingRoomLookup.TryGetRoomName(dungeonRoom, position)
                            : string.Empty,
                        IsAlive = !actor.dead,
                        IsHost = MimesisSaveManager.IsHost() && LocalPlayerHelper.IsLocalSteamId(steamId),
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
            WebDashboardMinimapTrainDto? rawTrain = TryCollectTrain(pdata?.main);
            WebDashboardMinimapBoundsDto bounds = ResolveEffectiveBounds(layout, rawTrain);
            List<WebDashboardMinimapMarkerDto> normalized = [];

            foreach (WebDashboardMinimapMarkerDto marker in raw)
            {
                EnrichFromPlayers(marker, players);
                normalized.Add(NormalizeMarker(marker, bounds));
            }

            train = NormalizeTrain(rawTrain, bounds);
            return normalized;
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

        internal static WebDashboardMinimapTrainDto? NormalizeTrain(
            WebDashboardMinimapTrainDto? train,
            WebDashboardMinimapBoundsDto bounds)
        {
            return train == null
                ? null
                : new WebDashboardMinimapTrainDto
                {
                    X = NormalizeCoord(train.X, bounds.MinX, bounds.MaxX),
                    Z = NormalizeCoord(train.Z, bounds.MinZ, bounds.MaxZ),
                    Yaw = train.Yaw,
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

        internal static WebDashboardMinimapTrainDto? TryCollectTrain(GameMainBase? main)
        {
            if (main is InTramWaitingScene)
            {
                return WebDashboardMinimapTramSpace.CreateWaitingRoomTrainMarker();
            }

            if (main is not MaintenanceScene)
            {
                return null;
            }

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
                    Yaw = root.eulerAngles.y,
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
