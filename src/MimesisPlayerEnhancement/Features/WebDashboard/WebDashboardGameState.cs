using ReluNetwork.ConstEnum;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameState
    {
        internal static bool IsConnected()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata != null && pdata.SessionJoined;
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

        internal static bool CanViewGlobalSettings()
        {
            return true;
        }

        internal static bool CanViewCatalogs()
        {
            // Read-only item/dungeon labels for settings UI (including offline global settings).
            return CanViewGlobalSettings();
        }

        internal static bool CanEditGlobalSettings()
        {
            return !IsConnected() || IsHost();
        }

        internal static bool CanEditGlobalSetting(string sectionId, string key)
        {
            if (!IsConnected())
            {
                return true;
            }

            if (IsHost())
            {
                return true;
            }

            return ModConfigEntryLocalEffect.HasLocalEffect(sectionId, key);
        }

        internal static bool CanEditSaveSettings()
        {
            return IsConnected() && IsHost() && GetSaveSlotId() >= 0;
        }

        internal static int GetSaveSlotId()
        {
            if (MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId))
            {
                return slotId;
            }

            if (!IsHost())
            {
                return -1;
            }

            if (SaveSlotConfigStore.ActiveSlotId >= 0)
            {
                return SaveSlotConfigStore.ActiveSlotId;
            }

            slotId = GameSessionAccess.GetSaveSlotId();
            if (slotId >= 0 && GameSessionAccess.IsValidSaveSlotId(slotId))
            {
                return slotId;
            }

            return StatisticsTracker.TryGetLoadedSlotId(out slotId) ? slotId : -1;
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
    }
}
