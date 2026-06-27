using System;
using ReluProtocol;
using ReluProtocol.C2S;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

internal static class JoinAnytimeNetworkTools
{
    internal static void SendOnPlayingStateToClient(VPlayer player) =>
        SendOnPlayingState(player.UID, msg => player.SendToMe(msg));

    internal static void SendOnPlayingStateToClient(SessionContext context) =>
        SendOnPlayingState(context.GetPlayerUID(), msg => context.Send(msg));

    private static void SendOnPlayingState(long uid, Action<IMsg> send)
    {
        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (pdata?.main is not GamePlayScene gps)
            return;

        IVroom? dungeonRoom = JoinAnytimeRoomTools.GetActiveDungeonRoom();
        if (dungeonRoom == null)
        {
            LateJoinManager.Log("SendOnPlayingState failed: no active DungeonRoom.");
            return;
        }

        LateJoinManager.Log(
            $"Sending in-game state to uid={uid}: dungeon={gps.DungeonMasterID}, " +
            $"seed={gps.RandDungeonSeed}, roomUID={dungeonRoom.RoomID}");

        send(new MoveToDungeonSig
        {
            selectedDungeonMasterID = gps.DungeonMasterID,
            randDungeonSeed = gps.RandDungeonSeed,
        });

        send(new MakeRoomCompleteSig
        {
            nextRoomInfo = new RoomInfo
            {
                roomType = VRoomType.Game,
                roomMasterID = gps.DungeonMasterID,
                roomUID = dungeonRoom.RoomID,
            },
        });
    }
}
