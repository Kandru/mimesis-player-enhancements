namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    // game@0.3.2 Assembly-CSharp/SteamInviteDispatcher.cs:L788-800
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), typeof(bool), typeof(bool))]
    internal static class SteamLobbyCreationTranspiler
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Sole ldc.i4.4 is SteamMatchmaking.CreateLobby(..., 4) — keep timeout/bookkeeping intact.
            return MaxPlayerCountIl.ReplaceAllPlayerCapLiteralFour(
                instructions,
                MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }

    // game@0.3.2 Assembly-CSharp/SteamInviteDispatcher.cs:L1017-1043
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdatePlayerGroupSize))]
    internal static class UpdatePlayerGroupSizeTranspiler
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }
}
