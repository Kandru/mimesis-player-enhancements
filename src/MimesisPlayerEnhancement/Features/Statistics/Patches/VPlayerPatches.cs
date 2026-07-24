using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.1 Assembly-CSharp/VPlayer.cs:L369-395
    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandlePutIntoToilet))]
    internal static class VPlayerHandlePutIntoToiletPatches
    {
        [HarmonyPrefix]
        public static void Prefix(VPlayer __instance)
        {
            StatisticsPatchGuard.Run(nameof(VPlayer.HandlePutIntoToilet), () =>
            {
                TrainDepositTracker.BeginDeposit(__instance);
            });
        }

        [HarmonyPostfix]
        public static void Postfix(MsgErrorCode __result)
        {
            StatisticsPatchGuard.Run(nameof(VPlayer.HandlePutIntoToilet), () =>
            {
                if (__result != MsgErrorCode.Success)
                {
                    return;
                }

                TrainDepositTracker.CompleteDeposit();
            });
        }
    }

    // game@0.3.1 Assembly-CSharp/VPlayer.cs:L586-602
    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.Revive))]
    public static class VPlayerRevivePatches
    {
        [HarmonyPostfix]
        public static void Postfix(VPlayer __instance, bool __result)
        {
            StatisticsPatchGuard.Run(nameof(VPlayer.Revive), () =>
            {
                if (!__result)
                {
                    return;
                }

                StatisticsTracker.OnPlayerRevived(__instance.SteamID);
            });
        }
    }
}
