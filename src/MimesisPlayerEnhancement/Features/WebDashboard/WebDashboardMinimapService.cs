using System.Collections.Generic;
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
                    markers.Add(new WebDashboardMinimapMarkerDto
                    {
                        SteamId = steamId,
                        DisplayName = string.IsNullOrWhiteSpace(actor.nickName) ? steamId.ToString() : actor.nickName,
                        X = position.x,
                        Z = position.z,
                        Yaw = transform.eulerAngles.y,
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
            WebDashboardMinimapBoundsDto bounds = ResolveEffectiveBounds(layout, raw);
            List<WebDashboardMinimapMarkerDto> normalized = [];

            foreach (WebDashboardMinimapMarkerDto marker in raw)
            {
                EnrichFromPlayers(marker, players);
                normalized.Add(NormalizeMarker(marker, bounds));
            }

            train = NormalizeTrain(layout.Train, bounds);
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
            IReadOnlyList<WebDashboardMinimapMarkerDto> rawMarkers)
        {
            if (layout.LayoutKind is not "hub" and not "none")
            {
                return layout.Bounds;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            if (layout.Train != null)
            {
                minX = layout.Train.X;
                maxX = layout.Train.X;
                minZ = layout.Train.Z;
                maxZ = layout.Train.Z;
            }

            foreach (WebDashboardMinimapMarkerDto marker in rawMarkers)
            {
                minX = Mathf.Min(minX, marker.X);
                maxX = Mathf.Max(maxX, marker.X);
                minZ = Mathf.Min(minZ, marker.Z);
                maxZ = Mathf.Max(maxZ, marker.Z);
            }

            if (float.IsPositiveInfinity(minX))
            {
                return layout.Bounds;
            }

            float spanX = Mathf.Max(maxX - minX, 10f);
            float spanZ = Mathf.Max(maxZ - minZ, 10f);
            const float padding = 0.05f;
            float padX = spanX * padding;
            float padZ = spanZ * padding;

            return new WebDashboardMinimapBoundsDto
            {
                MinX = minX - padX,
                MinZ = minZ - padZ,
                MaxX = maxX + padX,
                MaxZ = maxZ + padZ,
            };
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
