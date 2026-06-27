using System.Collections.Generic;
using HarmonyLib;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
public static class StatisticsMaintenanceRoomSavePatches
{
    [HarmonyPostfix]
    public static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;
        if (__result != MsgErrorCode.Success)
            return;

        int slotId = isAutoSave ? 0 : saveSlotID;
        StatisticsTracker.OnGameSaved(slotId);
    }
}
