using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    // game@0.3.1 Assembly-CSharp/UiPrefab_RoomCard.cs:L110-188
    [HarmonyPatch(typeof(UiPrefab_RoomCard), "SetRoomData")]
    internal static class RoomCardSetRoomDataTranspiler
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string literal && literal == "/4")
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, MorePlayersPatchHelpers.GetLobbyPlayerCountSuffixMethod);
                }
            }

            return codes;
        }
    }
}
