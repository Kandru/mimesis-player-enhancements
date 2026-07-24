using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal enum WebDashboardActivityRoomKind
    {
        Unknown = 0,
        MaintenanceRoom = 1,
        WaitingRoom = 2,
    }

    internal enum WebDashboardActivitySceneKind
    {
        Unknown = 0,
        Maintenance = 1,
        GamePlay = 2,
        TramWaiting = 3,
        DeathMatch = 4,
    }

    internal static class WebDashboardPlayerStateResolver
    {
        internal static void ApplyActivityState(WebDashboardPlayerDto dto, SessionContext? context)
        {
            if (dto.PlayerUid == 0)
            {
                dto.ActivityState = "connecting";
                dto.ActivityDetail = string.Empty;
                return;
            }

            VPlayer? vPlayer = ResolveVPlayer(dto, context);
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            GameMainBase? main = pdata?.main;

            dto.ActivityState = ResolveActivityState(
                dto.PlayerUid,
                hasPlayer: vPlayer != null,
                levelLoadCompleted: vPlayer?.LevelLoadCompleted == true,
                hasLateJoinLabel: !string.IsNullOrWhiteSpace(dto.LateJoinLabel),
                roomKind: ClassifyRoom(vPlayer?.VRoom),
                sceneKind: ClassifyScene(main));
            dto.ActivityDetail = ResolveDetail(dto, vPlayer, pdata);
        }

        internal static string ResolveActivityState(
            long playerUid,
            bool hasPlayer,
            bool levelLoadCompleted,
            bool hasLateJoinLabel,
            WebDashboardActivityRoomKind roomKind,
            WebDashboardActivitySceneKind sceneKind)
        {
            if (playerUid == 0 || !hasPlayer)
            {
                return "connecting";
            }

            if (!levelLoadCompleted)
            {
                return "loading";
            }

            if (hasLateJoinLabel)
            {
                return "late_join";
            }

            return (roomKind, sceneKind) switch
            {
                (WebDashboardActivityRoomKind.MaintenanceRoom, WebDashboardActivitySceneKind.Maintenance)
                    => "maintenance_room",
                (WebDashboardActivityRoomKind.MaintenanceRoom, _) => "maintenance",
                (WebDashboardActivityRoomKind.WaitingRoom, _) => "tram",
                (_, WebDashboardActivitySceneKind.GamePlay) => "dungeon",
                (_, WebDashboardActivitySceneKind.TramWaiting) => "tram",
                (_, WebDashboardActivitySceneKind.Maintenance) => "maintenance_bay",
                (_, WebDashboardActivitySceneKind.DeathMatch) => "death_match",
                _ => "online",
            };
        }

        internal static string ResolveLoadingSceneKeyFromTypeName(string? typeName)
        {
            return typeName switch
            {
                nameof(InTramWaitingScene) => "tram",
                nameof(MaintenanceScene) => "maintenance",
                nameof(GamePlayScene) => "dungeon",
                nameof(DeathMatchScene) => "death_match",
                null or "" => "session",
                _ => typeName,
            };
        }

        private static string ResolveLoadingSceneKey(GameMainBase? main)
        {
            return main switch
            {
                InTramWaitingScene => "tram",
                MaintenanceScene => "maintenance",
                GamePlayScene => "dungeon",
                DeathMatchScene => "death_match",
                null => "session",
                _ => main.GetType().Name,
            };
        }

        private static string ResolveDetail(
            WebDashboardPlayerDto dto,
            VPlayer? vPlayer,
            Hub.PersistentData? pdata)
        {
            if (dto.PlayerUid == 0 || vPlayer == null)
            {
                return string.Empty;
            }

            if (!vPlayer.LevelLoadCompleted)
            {
                if (!string.IsNullOrWhiteSpace(dto.LateJoinLabel))
                {
                    return dto.LateJoinLabel;
                }

                if (vPlayer.VRoom is MaintenanceRoom
                    && pdata?.main is InTramWaitingScene or GamePlayScene)
                {
                    return "tram";
                }

                return ResolveLoadingSceneKey(pdata?.main);
            }

            if (!string.IsNullOrWhiteSpace(dto.LateJoinLabel))
            {
                return dto.LateJoinLabel;
            }

            if (vPlayer.VRoom is MaintenanceRoom or VWaitingRoom)
            {
                return string.Empty;
            }

            if (pdata?.main is not GamePlayScene)
            {
                return string.Empty;
            }

            DungeonRoom? dungeonRoom = JoinAnytimeRoomTools.GetActiveDungeonRoom() as DungeonRoom;
            if (dungeonRoom == null)
            {
                return string.Empty;
            }

            string roomName = SpawnScalingRoomLookup.TryGetRoomName(dungeonRoom, vPlayer.PositionVector);
            return string.IsNullOrWhiteSpace(roomName) ? string.Empty : roomName;
        }

        private static VPlayer? ResolveVPlayer(WebDashboardPlayerDto dto, SessionContext? context)
        {
            VPlayer? vPlayer = context != null ? WebDashboardSessionAccess.GetVPlayer(context) : null;
            if (vPlayer == null && dto.PlayerUid != 0)
            {
                WebDashboardSessionAccess.TryGetPlayerByUid(dto.PlayerUid, out vPlayer);
            }

            return vPlayer;
        }

        private static WebDashboardActivityRoomKind ClassifyRoom(IVroom? room)
        {
            return room switch
            {
                MaintenanceRoom => WebDashboardActivityRoomKind.MaintenanceRoom,
                VWaitingRoom => WebDashboardActivityRoomKind.WaitingRoom,
                _ => WebDashboardActivityRoomKind.Unknown,
            };
        }

        private static WebDashboardActivitySceneKind ClassifyScene(GameMainBase? main)
        {
            return main switch
            {
                MaintenanceScene => WebDashboardActivitySceneKind.Maintenance,
                GamePlayScene => WebDashboardActivitySceneKind.GamePlay,
                InTramWaitingScene => WebDashboardActivitySceneKind.TramWaiting,
                DeathMatchScene => WebDashboardActivitySceneKind.DeathMatch,
                _ => WebDashboardActivitySceneKind.Unknown,
            };
        }
    }
}
