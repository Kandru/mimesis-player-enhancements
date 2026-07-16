using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Config.Patches
{
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
    internal static class MaintenanceRoomSavePatches
    {
        private const string Feature = "SaveSlotSidecar";

        [HarmonyPostfix]
        private static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
        {
            if (__result != MsgErrorCode.Success || !MimesisSaveManager.IsHost())
            {
                return;
            }

            SaveSlotSidecarPersistence.OnGameSaved(saveSlotID, playerNames, isAutoSave);
        }
    }
}
