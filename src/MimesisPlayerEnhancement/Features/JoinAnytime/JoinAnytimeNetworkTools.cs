namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeNetworkTools
    {
        private const string Feature = "JoinAnytime";

        internal static bool RouteToTram(VPlayer player)
        {
            if (player == null)
            {
                return false;
            }

            if (player.VRoom is VWaitingRoom)
            {
                LateJoinRouteTracker.MarkInWaitingRoom(player.UID);
                return true;
            }

            long uid = player.UID;
            LateJoinRoutePhase phase = LateJoinRouteTracker.GetPhase(uid);
            if (phase is LateJoinRoutePhase.AwaitingClient or LateJoinRoutePhase.InWaitingRoom)
            {
                // Client owns the transition after server release — never resend route packets.
                return true;
            }

            if (!JoinAnytimeRoomTools.TryEnsureWaitingRoom(out IVroom? waitingRoom))
            {
                ModLog.Warn(Feature, $"RouteToTram failed — waiting room unavailable for uid={uid}");
                return false;
            }

            ModLog.Info(Feature, $"Route to tram uid={uid} — waitingRoomUID={waitingRoom!.RoomID}");

            player.SendToMe(new MakeRoomCompleteSig
            {
                nextRoomInfo = new RoomInfo
                {
                    roomType = VRoomType.Waiting,
                    roomUID = waitingRoom.RoomID,
                },
            });
            player.SendToMe(new MoveToWaitingRoomSig());

            if (player.VRoom is MaintenanceRoom)
            {
                player.SendToMe(new LeaveRoomSig { actorID = player.ObjectID });
                JoinAnytimeRoomTools.ReleaseLateJoinerFromMaintenance(player);
            }

            LateJoinRouteTracker.MarkAwaitingClient(uid);
            return true;
        }
    }
}
