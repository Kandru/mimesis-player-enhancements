namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
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

    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.Reset))]
    internal static class GameSessionInfoResetPatches
    {
        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance)
        {
            StatisticsPatchGuard.Run(nameof(GameSessionInfo.Reset), () =>
            {
                if (__instance.StageCount <= 1)
                {
                    StatisticsRunTracker.OnRunRestart();
                }
            });
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.TerminateSession))]
    internal static class VRoomManagerTerminateSessionPatches
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            StatisticsPatchGuard.Run(nameof(VRoomManager.TerminateSession), () =>
            {
                GameSessionInfo? session = GameSessionAccess.TryGetGameSessionInfo();
                if (session != null && session.StageCount <= 1)
                {
                    StatisticsRunTracker.OnRunRestart();
                }
            });
        }
    }
}
