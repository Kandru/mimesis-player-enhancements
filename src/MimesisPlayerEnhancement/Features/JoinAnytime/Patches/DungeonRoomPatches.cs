namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L1018-1021
    [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
    internal static class DungeonRoomOnAllMemberEnteredLobbyPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
            JoinAnytimeLobbyController.RefreshLobbyState(force: true);
        }
    }
}
