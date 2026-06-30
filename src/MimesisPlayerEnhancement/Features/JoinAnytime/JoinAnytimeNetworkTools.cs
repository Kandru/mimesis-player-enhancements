using System;
using ReluProtocol;
using ReluProtocol.C2S;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeNetworkTools
    {
        internal static bool SendPreGameTramStateToClient(VPlayer player, bool allowResend = false)
        {
            if (player.VRoom is VWaitingRoom)
            {
                LateJoinManager.MarkPreGameStateSent(player.UID);
                return true;
            }

            if (!SendPreGameTramState(player, msg => player.SendToMe(msg), allowResend))
            {
                return false;
            }

            JoinAnytimeRoomTools.ReleaseLateJoinerFromMaintenance(player);
            return true;
        }

        private static bool SendPreGameTramState(VPlayer player, Action<IMsg> send, bool allowResend)
        {
            long uid = player.UID;

            if (LateJoinManager.HasPreGameStateBeenSent(uid))
            {
                if (!allowResend)
                {
                    ModLog.Debug("JoinAnytime", $"Skipping duplicate pre-game tram state send for uid={uid}");
                    return player.VRoom is not MaintenanceRoom;
                }

                ModLog.Debug("JoinAnytime", $"Resending pre-game tram state for uid={uid} — still in maintenance");
            }

            if (!JoinAnytimeRoomTools.TryEnsureWaitingRoom(out IVroom? waitingRoom))
            {
                ModLog.Warn("JoinAnytime", $"SendPreGameTramState failed — waiting room unavailable for uid={uid}");
                return false;
            }

            ModLog.Info(
                "JoinAnytime",
                $"Sending pre-game tram state to uid={uid} — waitingRoomUID={waitingRoom!.RoomID}");

            send(new MakeRoomCompleteSig
            {
                nextRoomInfo = new RoomInfo
                {
                    roomType = VRoomType.Waiting,
                    roomUID = waitingRoom.RoomID,
                },
            });

            send(new MoveToWaitingRoomSig());

            LateJoinManager.MarkPreGameStateSent(uid);
            return true;
        }
    }
}
