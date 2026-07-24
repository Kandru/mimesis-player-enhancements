using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/NewTramLeverLevelObject.cs:L131-158
    [HarmonyPatch]
    internal static class NewTramLeverLevelObjectIsTriggerablePatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(NewTramLeverLevelObject), "IsTriggerable", [typeof(ProtoActor), typeof(int)]);

        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (!__result || !ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (GameSessionAccess.TryGetPdata()?.main is not InTramWaitingScene)
            {
                return;
            }

            if (!JoinAnytimeRoomTools.ShouldBlockWaitingRoomStartGame())
            {
                return;
            }

            __result = false;
        }
    }

    // game@0.3.1 Assembly-CSharp/NewTramLeverLevelObject.cs:L196-233
    [HarmonyPatch(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.OnChangeLevelObjectStateSig))]
    internal static class NewTramLeverLevelObjectOnChangeLevelObjectStateSigPatch
    {
        [HarmonyPostfix]
        private static void Postfix(int actorId, int prevState, int currentState)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (currentState != (int)NewTramLeverState.Open)
            {
                return;
            }

            JoinAnytimeUserMessages.OnLocalTramLeverOpened(actorId);
        }
    }
}
