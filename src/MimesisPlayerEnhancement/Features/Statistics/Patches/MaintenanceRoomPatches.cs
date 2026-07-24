namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L1225-1235
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
