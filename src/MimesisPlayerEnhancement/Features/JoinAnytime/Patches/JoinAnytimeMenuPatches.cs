using System.Linq;
using System.Reflection;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
    internal static class UIPrefabInGameMenuStartJoinAnytimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            JoinAnytimeInGameMenuTools.OnMenuStart(__instance);
        }
    }

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class UIPrefabInGameMenuOnEnableJoinAnytimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            JoinAnytimeInGameMenuTools.EnsurePublicRoomControlsAccessible(__instance);
        }
    }

    [HarmonyPatch]
    internal static class UIPrefabInGameMenuSetPublicRoomNamePatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetPublicRoomName");

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeLobbyController.OnPublicRoomNameChanged(__instance, __instance.lobbyName);
        }
    }

    [HarmonyPatch]
    internal static class UIPrefabPublicRoomListSetRoomListPatch
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? RoomListDataField =
            typeof(UIPrefab_PublicRoomList).GetField("roomListData", InstanceFlags);

        private static readonly MethodInfo? SetRoomListUiMethod =
            typeof(UIPrefab_PublicRoomList).GetMethod("SetRoomListUI", InstanceFlags);

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_PublicRoomList), "SetRoomList");

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_PublicRoomList __instance, List<CSteamID> lobbyIDs)
        {
            if (!ModConfig.EnableJoinAnytime.Value || lobbyIDs == null || lobbyIDs.Count == 0)
            {
                return;
            }

            FieldInfo? roomListDataField = RoomListDataField;
            if (roomListDataField?.GetValue(__instance) is not List<PublicRoomListData> roomListData)
            {
                return;
            }

            HashSet<CSteamID> existing = roomListData
                .Select(entry => entry.lobbyID)
                .ToHashSet();

            bool added = false;
            foreach (CSteamID lobbyId in lobbyIDs)
            {
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                if (playerCount < JoinAnytimeLobbyDisplay.VanillaBrowseDenominator || existing.Contains(lobbyId))
                {
                    continue;
                }

                if (!JoinAnytimeLobbyDisplay.ShouldIncludeInPublicList(playerCount, lobbyId))
                {
                    continue;
                }

                roomListData.Add(JoinAnytimeLobbyDisplay.CreatePublicRoomListData(lobbyId, playerCount));
                _ = existing.Add(lobbyId);
                added = true;
            }

            if (!added)
            {
                return;
            }

            SetRoomListUiMethod?.Invoke(__instance, null);
        }
    }

    [HarmonyPatch]
    internal static class UiPrefabRoomCardSetRoomDataPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UiPrefab_RoomCard), "SetRoomData");

        [HarmonyPostfix]
        private static void Postfix(PublicRoomListData data, UiPrefab_RoomCard __instance)
        {
            JoinAnytimeLobbyDisplay.ApplyBrowsePlayerCountToRoomCard(data, __instance);
        }
    }
}
