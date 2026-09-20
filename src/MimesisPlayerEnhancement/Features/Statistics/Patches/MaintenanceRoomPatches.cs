namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.2 Assembly-CSharp/MaintenanceRoom.cs:L1235-1245
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnActorEvent))]
    public static class MaintenanceRoomActorDeathPatches
    {
        [HarmonyPostfix]
        public static void Postfix(MaintenanceRoom __instance, VActorEventArgs args)
        {
            StatisticsPatchGuard.Run(nameof(MaintenanceRoom.OnActorEvent), () =>
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
