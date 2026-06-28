using System;
using ReluProtocol;
using ReluProtocol.C2S;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeNetworkTools
    {
        internal static void SendPreGameTramStateToClient(VPlayer player)
        {
            SendPreGameTramState(player.UID, msg => player.SendToMe(msg));
        }

        internal static void SendPreGameTramStateToClient(SessionContext context)
        {
            SendPreGameTramState(context.GetPlayerUID(), msg => context.Send(msg));
        }

        private static void SendPreGameTramState(long uid, Action<IMsg> send)
        {
            if (LateJoinManager.HasPreGameStateBeenSent(uid))
            {
                ModLog.Debug("JoinAnytime", $"Skipping duplicate pre-game tram state send for uid={uid}");
                return;
            }

            if (!JoinAnytimeRoomTools.TryEnsureWaitingRoom(out IVroom? waitingRoom))
            {
                ModLog.Warn("JoinAnytime", $"SendPreGameTramState failed — waiting room unavailable for uid={uid}");
                return;
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
        }
    }
}
