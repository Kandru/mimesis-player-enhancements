using System;
using System.Reflection;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeLobbyDisplay
    {
        private const int BrowseCap = 3;
        private const int VanillaBrowseDenominator = 4;

        internal static int GetBrowsePlayerCount(int realCount)
        {
            if (realCount < 1)
            {
                return 0;
            }

            return realCount < BrowseCap ? realCount : BrowseCap;
        }

        internal static string FormatBrowsePlayerCount(int realCount) =>
            $"{GetBrowsePlayerCount(realCount)}/{VanillaBrowseDenominator}";

        internal static bool ShouldIncludeInPublicList(int playerCount, CSteamID lobbyId)
        {
            if (playerCount < VanillaBrowseDenominator)
            {
                return true;
            }

            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return false;
            }

            string phase = SteamMatchmaking.GetLobbyData(lobbyId, JoinAnytimeLobbyMetadata.JoinPhaseKey);
            return !string.IsNullOrEmpty(phase);
        }

        internal static PublicRoomListData CreatePublicRoomListData(CSteamID lobbyId, int playerCount)
        {
            PublicRoomListData data = new()
            {
                lobbyID = lobbyId,
                PlayerCount = playerCount,
                locale = SteamMatchmaking.GetLobbyData(lobbyId, "Locale"),
                lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "LobbyName"),
                password = SteamMatchmaking.GetLobbyData(lobbyId, "HasPassword"),
            };

            _ = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId, "Cycle"), out data.cycle);
            int repairStatus = 0;
            _ = int.TryParse(SteamMatchmaking.GetLobbyData(lobbyId, "RepairStatus"), out repairStatus);
            data.repairStatus = data.cycle == 1 ? 0 : repairStatus + 1;
            return data;
        }

        internal static void ApplyBrowsePlayerCountToRoomCard(PublicRoomListData data, object roomCard)
        {
            if (!ModConfig.EnableJoinAnytime.Value || data == null || roomCard == null)
            {
                return;
            }

            try
            {
                PropertyInfo? playerCountProp = roomCard.GetType().GetProperty(
                    "UE_PlayerCount",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object? playerCountUi = playerCountProp?.GetValue(roomCard);
                if (playerCountUi == null)
                {
                    return;
                }

                MethodInfo? getComponent = playerCountUi.GetType().GetMethod(
                    "GetComponent",
                    [typeof(Type)]);
                if (getComponent == null)
                {
                    return;
                }

                Type? tmpTextType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
                if (tmpTextType == null)
                {
                    return;
                }

                object? text = getComponent.Invoke(playerCountUi, [tmpTextType]);
                PropertyInfo? textProp = tmpTextType.GetProperty("text");
                textProp?.SetValue(text, FormatBrowsePlayerCount(data.PlayerCount));
            }
            catch (Exception ex)
            {
                ModLog.Debug("JoinAnytime", $"Browse player count UI patch failed — {ex.Message}");
            }
        }
    }
}
