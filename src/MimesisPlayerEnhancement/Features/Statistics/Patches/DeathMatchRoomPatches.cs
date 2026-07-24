namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/DeathMatchRoom.cs:L252-321
    [HarmonyPatch(typeof(DeathMatchRoom), nameof(DeathMatchRoom.OnActorEvent))]
    public static class DeathMatchRoomActorDeathPatches
    {
        [HarmonyPostfix]
        public static void Postfix(DeathMatchRoom __instance, VActorEventArgs args)
        {
            StatisticsPatchGuard.Run(nameof(DeathMatchRoom.OnActorEvent), () =>
            {
                if (args is not GameActorDeadEventArgs deadArgs)
                {
                    return;
                }

                StatisticsDeathHandler.HandleActorDeath(__instance, deadArgs);
            });
        }
    }
}
