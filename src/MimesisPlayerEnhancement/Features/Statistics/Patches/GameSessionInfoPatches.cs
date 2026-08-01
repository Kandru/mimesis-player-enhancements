namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/GameSessionInfo.cs:L245-255
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.IncreaseStageCount))]
    internal static class GameSessionInfoIncreaseStageCountPatches
    {
        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance, bool reset)
        {
            StatisticsPatchGuard.Run(nameof(GameSessionInfo.IncreaseStageCount), () =>
            {
                StatisticsRunTracker.OnStageChanged(__instance.StageCount, reset);
            });
        }
    }
}
