namespace MimesisPlayerEnhancement.Config.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
    internal static class GameSessionInfoLoadPatches
    {
        private const string Feature = "SaveSlotSidecar";

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
}
