using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameState
    {
        internal static bool IsConnected()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata != null && pdata.SessionJoined;
        }

        internal static bool IsIngame()
        {
            if (!IsConnected())
            {
                return false;
            }

            return !string.IsNullOrEmpty(
                WebDashboardSessionScene.Resolve(GameSessionAccess.TryGetPdata()?.main));
        }

        internal static bool IsLocalPlayerAlive()
        {
            if (!IsIngame())
            {
                return false;
            }

            return WebDashboardSessionAccess.TryGetLocalVPlayer(out VPlayer? player)
                && player != null
                && player.IsAliveStatus();
        }

        internal static bool CanUseUiDebugOverlays() => IsIngame() && IsLocalPlayerAlive();

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

            return GameSessionAccess.TryGetPdata()?.ClientMode == NetworkClientMode.Host
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
            if (!IsConnected() || !IsHost())
            {
                return -1;
            }

            // Sidecars load from ApplyLoadedGameData(saveGameData.SlotID) — authoritative for this session.
            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId >= 0 && GameSessionAccess.IsValidSaveSlotId(activeSlotId))
            {
                int vworldSlotId = GameSessionAccess.GetSaveSlotId();
                if (vworldSlotId >= 0
                    && GameSessionAccess.IsValidSaveSlotId(vworldSlotId)
                    && vworldSlotId != activeSlotId)
                {
                    ModLog.Debug(
                        "WebDashboard",
                        $"Save slot mismatch — sidecar={activeSlotId}, vworld={vworldSlotId}; using sidecar.");
                }

                return activeSlotId;
            }

            if (MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId))
            {
                return slotId;
            }

            return -1;
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
