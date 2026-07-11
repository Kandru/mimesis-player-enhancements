using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPlayerStateResolver
    {
        internal static void ApplyActivityState(WebDashboardPlayerDto dto, SessionContext? context)
        {
            dto.ActivityState = ResolveStateCode(dto, context);
            dto.ActivityDetail = ResolveDetail(dto, context);
        }

        private static string ResolveStateCode(WebDashboardPlayerDto dto, SessionContext? context)
        {
            if (dto.PlayerUid == 0)
            {
                return "connecting";
            }

            VPlayer? vPlayer = ResolveVPlayer(dto, context);
            if (vPlayer == null)
            {
                return "connecting";
            }

            if (!vPlayer.LevelLoadCompleted)
            {
                return "loading";
            }

            if (!string.IsNullOrWhiteSpace(dto.LateJoinLabel))
            {
                return "late_join";
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            GameMainBase? main = pdata?.main;

            return vPlayer.VRoom switch
            {
                MaintenanceRoom => main is MaintenanceScene ? "maintenance_room" : "maintenance",
                VWaitingRoom => "tram",
                _ when main is GamePlayScene => "dungeon",
                _ when main is InTramWaitingScene => "tram",
                _ when main is MaintenanceScene => "maintenance_bay",
                _ when main is DeathMatchScene => "death_match",
                _ => "online",
            };
        }

        private static string ResolveDetail(WebDashboardPlayerDto dto, SessionContext? context)
        {
            if (dto.PlayerUid == 0)
            {
                return string.Empty;
            }

            VPlayer? vPlayer = ResolveVPlayer(dto, context);
            if (vPlayer == null)
            {
                return string.Empty;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();

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

            if (pdata?.main is not GamePlayScene gps)
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
    }
}
