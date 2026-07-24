using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/UiPrefab_RoomCard.cs:L110-187
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
