using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
    public static class GameSessionInfoLoadPatches
    {
        [HarmonyPostfix]
        public static void Postfix(MMSaveGameData saveGameData)
        {
            StatisticsPatchGuard.Run(nameof(GameSessionInfo.ApplyLoadedGameData), () =>
            {
                if (!MimesisSaveManager.IsHost())
                {
                    return;
                }

                int slotId = saveGameData?.SlotID ?? MimesisSaveManager.GetCurrentSaveSlotId();
                if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
                {
                    return;
                }

                StatisticsTracker.LoadForSlot(slotId);
                SaveSlotConfigLifecycle.OnSaveSlotLoaded(slotId);
            });
        }
    }
}
