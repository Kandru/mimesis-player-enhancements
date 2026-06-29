using System.Collections.Generic;
using ReluNetwork.ConstEnum;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Server-only late join: route joiners through vanilla maintenance -> tram using stock packets.
    /// Joiners wait in the waiting room until active players return from the dungeon.
    /// </summary>
    internal static class LateJoinManager
    {
        private const string Feature = "JoinAnytime";

        private static readonly HashSet<long> SentPreGameStateUids = [];

        internal static bool IsEnabled => ModConfig.EnableJoinAnytime.Value;

        internal static void OnServerLogin(SessionContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Host)
            {
                return;
            }

            if (pdata.main is not GamePlayScene && pdata.main is not InTramWaitingScene)
            {
                return;
            }

            ModLog.Debug(Feature, $"Login while session active — uid={context.GetPlayerUID()} main={pdata.main.GetType().Name}");
            JoinAnytimeNetworkTools.SendPreGameTramStateToClient(context);
        }

        internal static void OnServerPlayerCreated(VPlayer player)
        {
            if (!IsEnabled)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Host || player.IsHost)
            {
                return;
            }

            if (player.VRoom is not MaintenanceRoom)
            {
                return;
            }

            ModLog.Info(
                Feature,
                $"Late joiner in maintenance — uid={player.UID} hostScene={pdata.main?.GetType().Name ?? "null"}");

            JoinAnytimeNetworkTools.SendPreGameTramStateToClient(player);
        }

        internal static void OnServerEnterWaitingRoom(SessionContext context)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            if (context.GetVRoomType() != VRoomType.Maintenance)
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.main is not InTramWaitingScene and not GamePlayScene)
            {
                return;
            }

            ModLog.Debug(Feature, "Moving player snapshot Maintenance -> Waiting");
            JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
        }

        internal static void OnServerEnterMaintenance(SessionContext context)
        {
            if (!IsEnabled || context == null || !context.ExistPlayer())
            {
                return;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (context.GetVRoomType() == VRoomType.Game
                && pdata?.main is MaintenanceScene)
            {
                ModLog.Debug(Feature, "Moving player snapshot Dungeon -> Maintenance");
                JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
            }
        }

        internal static bool HasPreGameStateBeenSent(long uid) => SentPreGameStateUids.Contains(uid);

        internal static void MarkPreGameStateSent(long uid)
        {
            _ = SentPreGameStateUids.Add(uid);
        }

        internal static void RefreshLobbyVisibilityAfterSteamUpdate()
        {
            JoinAnytimeLobbyController.RefreshAfterSteamLobbyDataUpdate();
        }
    }
}
