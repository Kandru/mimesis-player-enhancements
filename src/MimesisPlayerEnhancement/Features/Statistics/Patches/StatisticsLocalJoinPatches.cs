using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    internal static class StatisticsLocalJoinPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SpeechEventArchive __instance)
        {
            bool isLocal;
            try
            {
                isLocal = __instance.IsLocal;
            }
            catch
            {
                return;
            }

            if (!isLocal)
            {
                return;
            }

            StatisticsMessages.OnLocalPlayerArchiveStarted();
        }
    }

    [HarmonyPatch(typeof(UIPrefab_PlayerEnterInfo), nameof(UIPrefab_PlayerEnterInfo.AddPlayerInfo))]
    internal static class StatisticsGamePlayerInfoPatch
    {
        [HarmonyPostfix]
        private static void Postfix(string userName, bool isEntering)
        {
            StatisticsMessages.OnGamePlayerInfoShown(userName, isEntering);
        }
    }
}
