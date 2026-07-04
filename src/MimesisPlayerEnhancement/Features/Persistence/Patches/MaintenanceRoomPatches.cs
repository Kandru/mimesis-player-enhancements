using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
    public static class MaintenanceRoomPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPostfix]
        public static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                return;
            }

            if (__result != MsgErrorCode.Success)
            {
                return;
            }

            if (!MimesisSaveManager.IsHost())
            {
                return;
            }

            ModLog.Info(Feature, $"Game save triggered — persisting mimic voices for slot {saveSlotID} (auto={isAutoSave}).");
            MimesisSaveManager.SaveMimesisData(saveSlotID);
        }
    }
}
