using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluProtocol.C2S;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeNetworkTools
    {
        private const string Feature = "JoinAnytime";

        internal static bool SendPreGameTramStateToClient(VPlayer player, bool allowResend = false)
        {
            if (player.VRoom is VWaitingRoom)
            {
                LateJoinRouteTracker.MarkInWaitingRoom(player.UID);
                return true;
            }

            return SendPreGameTramState(player.UID, msg => player.SendToMe(msg), allowResend)
                && TryReleaseLateJoiner(player);
        }

        internal static bool ResendPreGameTramStateToSession(SessionContext context, bool allowResend)
        {
            long uid = context.GetPlayerUID();
            if (uid == 0)
            {
                return false;
            }

            if (WebDashboardSessionAccess.GetVPlayer(context) is VPlayer livePlayer)
            {
                return SendPreGameTramStateToClient(livePlayer, allowResend);
            }

            if (!SendPreGameTramState(uid, msg => { _ = context.Send(msg); }, allowResend))
            {
                return false;
            }

            return TrySendMaintenanceLeaveRoomSig(context, uid, resend: true);
        }

        private static bool TryReleaseLateJoiner(VPlayer player)
        {
            if (player.VRoom is not MaintenanceRoom)
            {
                LateJoinRouteTracker.MarkAwaitingClient(player.UID);
                return true;
            }

            if (!JoinAnytimeRoomTools.ReleaseLateJoinerFromMaintenance(player))
            {
                return false;
            }

            return true;
        }

        private static bool TrySendMaintenanceLeaveRoomSig(SessionContext context, long uid, bool resend)
        {
            if (!LateJoinRouteTracker.TryGetMaintenanceActorId(uid, out int actorId))
            {
                return true;
            }

            if (resend)
            {
                ModLog.Debug(Feature, $"Resending maintenance LeaveRoomSig for uid={uid} — actorId={actorId}");
            }

            context.Send(new LeaveRoomSig
            {
                actorID = actorId,
            });
            return true;
        }

        private static bool SendPreGameTramState(long uid, Action<IMsg> send, bool allowResend)
        {
            if (LateJoinRouteTracker.HasCompletedServerRoute(uid))
            {
                if (!allowResend)
                {
                    ModLog.Debug(Feature, $"Skipping duplicate pre-game tram state send for uid={uid}");
                    return LateJoinRouteTracker.GetPhase(uid) != LateJoinRoutePhase.InMaintenance;
                }

                ModLog.Debug(Feature, $"Resending pre-game tram state for uid={uid} — phase={LateJoinRouteTracker.GetPhase(uid)}");
            }

            if (!JoinAnytimeRoomTools.TryEnsureWaitingRoom(out IVroom? waitingRoom))
            {
                ModLog.Warn(Feature, $"SendPreGameTramState failed — waiting room unavailable for uid={uid}");
                return false;
            }

            ModLog.Info(Feature, $"Sending pre-game tram state to uid={uid} — waitingRoomUID={waitingRoom!.RoomID}");

            send(new MakeRoomCompleteSig
            {
                nextRoomInfo = new RoomInfo
                {
                    roomType = VRoomType.Waiting,
                    roomUID = waitingRoom.RoomID,
                },
            });

            send(new MoveToWaitingRoomSig());

            return true;
        }
    }
}
