namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_PublicRoomList.cs:L252-321
    [HarmonyPatch(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.SetRoomList))]
    internal static class PublicRoomListSetRoomListTranspiler
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }
}
