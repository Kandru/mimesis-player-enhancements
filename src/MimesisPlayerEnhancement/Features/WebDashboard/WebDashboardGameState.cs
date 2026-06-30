using MimesisPlayerEnhancement.Features.JoinAnytime;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Util;
using ReluNetwork.ConstEnum;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameState
    {
        private static bool _sessionActive;

        internal static bool IsConnected()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata == null)
            {
                _sessionActive = false;
                return false;
            }

            if (pdata.SessionJoined)
            {
                _sessionActive = true;
                return true;
            }

            // Scene transition grace: SessionJoined may drop while main is still in-game.
            if (_sessionActive && IsInGameScene(pdata.main))
            {
                return true;
            }

            _sessionActive = false;
            return false;
        }

        internal static bool IsHost()
        {
            if (!IsConnected())
            {
                return false;
            }

            if (HostApplyGate.IsParticipantClient())
            {
                return false;
            }

            return JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Host
                || MimesisSaveManager.IsHost();
        }

        internal static bool CanEditGlobalSettings()
        {
            return !IsConnected() || IsHost();
        }

        internal static bool CanEditSaveSettings()
        {
            return IsConnected() && IsHost() && GetSaveSlotId() >= 0;
        }

        internal static int GetSaveSlotId()
        {
            return MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId) ? slotId : -1;
        }

        internal static string GetLobbyName()
        {
            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return string.Empty;
            }

            CSteamID lobbyId = dispatcher.joinedLobbyID;
            if (lobbyId == CSteamID.Nil)
            {
                return string.Empty;
            }

            string fromSteam = SteamMatchmaking.GetLobbyData(lobbyId, SteamInviteDispatcher.LOBBY_NAME_KEY);
            if (!string.IsNullOrWhiteSpace(fromSteam))
            {
                return fromSteam.Trim();
            }

            return string.IsNullOrWhiteSpace(dispatcher.lobbyName)
                ? string.Empty
                : dispatcher.lobbyName.Trim();
        }

        private static bool IsInGameScene(GameMainBase? main)
        {
            return main is InTramWaitingScene or MaintenanceScene or GamePlayScene or DeathMatchScene;
        }
    }
}
