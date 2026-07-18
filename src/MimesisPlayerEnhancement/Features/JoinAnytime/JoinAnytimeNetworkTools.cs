namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeNetworkTools
    {
        private const string Feature = "JoinAnytime";

        internal static bool RouteToTram(VPlayer player, bool allowResend = false)
        {
            if (player.VRoom is VWaitingRoom)
            {
                LateJoinRouteTracker.MarkInWaitingRoom(player.UID);
                return true;
            }

            return RouteToTram(player.UID, msg => player.SendToMe(msg), player, allowResend);
        }

        internal static bool RouteToTram(SessionContext context, bool allowResend = false)
        {
            long uid = context.GetPlayerUID();
            if (uid == 0)
            {
                return false;
            }

            if (SessionContextAccess.GetVPlayer(context) is VPlayer livePlayer)
            {
                return RouteToTram(livePlayer, allowResend);
            }

            return RouteToTram(uid, msg => { _ = context.Send(msg); }, player: null, allowResend);
        }

        private static bool RouteToTram(
            long uid,
            Action<IMsg> send,
            VPlayer? player,
            bool allowResend)
        {
            if (LateJoinRouteTracker.HasCompletedServerRoute(uid))
            {
                if (!allowResend)
                {
                    return LateJoinRouteTracker.GetPhase(uid) != LateJoinRoutePhase.InMaintenance;
                }

                ModLog.Debug(
                    Feature,
                    $"Resending route to tram for uid={uid} — phase={LateJoinRouteTracker.GetPhase(uid)}");
            }

            if (!JoinAnytimeRoomTools.TryEnsureWaitingRoom(out IVroom? waitingRoom))
            {
                ModLog.Warn(Feature, $"RouteToTram failed — waiting room unavailable for uid={uid}");
                LateJoinRouteTracker.SetRoutePending(uid, pending: true);
                return false;
            }

            LateJoinRouteTracker.SetRoutePending(uid, pending: false);

            ModLog.Info(Feature, $"Route to tram uid={uid} — waitingRoomUID={waitingRoom!.RoomID}");

            send(new MakeRoomCompleteSig
            {
                nextRoomInfo = new RoomInfo
                {
                    roomType = VRoomType.Waiting,
                    roomUID = waitingRoom.RoomID,
                },
            });

            send(new MoveToWaitingRoomSig());

            if (player?.VRoom is MaintenanceRoom)
            {
                int actorId = player.ObjectID;
                LateJoinRouteTracker.RecordMaintenanceActorId(uid, actorId);
                send(new LeaveRoomSig { actorID = actorId });
                JoinAnytimeRoomTools.ReleaseLateJoinerFromMaintenance(player);
            }
            else if (LateJoinRouteTracker.TryGetMaintenanceActorId(uid, out int actorId))
            {
                ModLog.Debug(Feature, $"Resending maintenance LeaveRoomSig for uid={uid} — actorId={actorId}");
                send(new LeaveRoomSig { actorID = actorId });
            }

            LateJoinRouteTracker.MarkAwaitingClient(uid);
            return true;
        }
    }
}
