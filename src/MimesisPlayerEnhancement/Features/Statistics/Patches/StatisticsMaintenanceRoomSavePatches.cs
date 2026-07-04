using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
    public static class StatisticsMaintenanceRoomSavePatches
    {
        [HarmonyPostfix]
        public static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
        {
            StatisticsPatchGuard.Run(nameof(MaintenanceRoom.SaveGameData), () =>
            {
                if (__result != MsgErrorCode.Success || !MimesisSaveManager.IsHost())
                {
                    return;
                }

                SaveSlotSidecarPersistence.OnGameSaved(saveSlotID);
            });
        }
    }
}
