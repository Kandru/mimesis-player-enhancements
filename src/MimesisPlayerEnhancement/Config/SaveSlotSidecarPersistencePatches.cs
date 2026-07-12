using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement
{
    internal static class SaveSlotSidecarPersistencePatches
    {
        private const string Feature = "SaveSlotSidecar";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(SaveSlotSidecarPersistencePatches)));

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("ApplyLoadedGameData/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))),
                ("SaveGameData/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))),
            ]);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
        private static class GameSessionInfoLoadPatch
        {
            [HarmonyPostfix]
            private static void Postfix(MMSaveGameData saveGameData)
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

                SaveSlotSidecarPersistence.OnSaveSlotLoaded(slotId);
            }
        }

        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
        private static class MaintenanceRoomSavePatch
        {
            [HarmonyPostfix]
            private static void Postfix(int saveSlotID, List<string> playerNames, bool isAutoSave, MsgErrorCode __result)
            {
                if (__result != MsgErrorCode.Success || !MimesisSaveManager.IsHost())
                {
                    return;
                }

                SaveSlotSidecarPersistence.OnGameSaved(saveSlotID);
            }
        }
    }
}
