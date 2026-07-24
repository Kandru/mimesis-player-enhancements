using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/VPlayer.cs:L41-73
    [HarmonyPatch]
    internal static class VPlayerCtorPatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Constructor(
                typeof(VPlayer),
                [
                    typeof(SessionContext),
                    typeof(int),
                    typeof(int),
                    typeof(bool),
                    typeof(string),
                    typeof(string),
                    typeof(PosWithRot),
                    typeof(bool),
                    typeof(IVroom),
                    typeof(ReasonOfSpawn),
                ]);

        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnServerPlayerCreated(__instance);
        }
    }

    // game@0.3.1 Assembly-CSharp/VPlayer.cs:L117-154
    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))]
    internal static class VPlayerHandleLevelLoadCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(VPlayer __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnLevelLoadCompleted(__instance);
            JoinAnytimeLobbyController.OnSessionRosterChanged();
        }
    }
}
