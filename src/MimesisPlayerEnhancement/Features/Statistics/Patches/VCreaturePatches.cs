namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/VCreature.cs:L93-111
    [HarmonyPatch(typeof(VCreature), nameof(VCreature.OnDying))]
    public static class VCreatureDyingPatches
    {
        [HarmonyPostfix]
        public static void Postfix(VCreature __instance, ActorDyingSig sig)
        {
            StatisticsPatchGuard.Run(nameof(VCreature.OnDying), () =>
            {
                if (__instance is not VPlayer player || __instance.VRoom == null)
                {
                    return;
                }

                StatisticsDeathHandler.OnPlayerDying(player, sig, __instance.VRoom);
            });
        }
    }
}
