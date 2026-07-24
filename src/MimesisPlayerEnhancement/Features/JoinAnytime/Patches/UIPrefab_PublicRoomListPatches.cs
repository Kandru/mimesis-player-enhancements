using System.Linq;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_PublicRoomList.cs:L252-320
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
}
