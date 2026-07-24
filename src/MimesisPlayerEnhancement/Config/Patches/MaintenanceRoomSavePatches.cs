using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Config.Patches
{
    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L997-1100
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
    internal static class MaintenanceRoomSavePatches
    {
        private const string Feature = "SaveSlotSidecar";

        [HarmonyPostfix]
        private static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
        {
            try
            {
                if (__result != MsgErrorCode.Success || !MimesisSaveManager.IsHost())
                {
                    return;
                }

                SaveSlotSidecarPersistence.OnGameSaved(saveSlotID, playerNames, isAutoSave);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SaveGameData sidecar flush failed — {ex.Message}");
            }
        }
    }
}
