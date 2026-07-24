using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    // game@0.3.1 Assembly-CSharp/IVroom.cs:L958-972
    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L332-410 (EnterMaintenenceRoom / EnterWaitingRoom closures)
    [HarmonyPatch]
    internal static class MaxPlayerCountFieldTranspiler
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(IVroom), "CanEnterChannel");

            foreach (MethodBase lambda in MorePlayersPatchHelpers.FindEnterRoomLambdaMethods())
            {
                yield return lambda;
            }
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return MaxPlayerCountIl.ReplaceConstMaxPlayerCount(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }
}
