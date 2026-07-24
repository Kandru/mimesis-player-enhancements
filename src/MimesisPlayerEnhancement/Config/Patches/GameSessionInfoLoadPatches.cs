namespace MimesisPlayerEnhancement.Config.Patches
{
    // game@0.3.1 Assembly-CSharp/GameSessionInfo.cs:L225-243
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
    internal static class GameSessionInfoLoadPatches
    {
        private const string Feature = "SaveSlotSidecar";

        [HarmonyPostfix]
        private static void Postfix(MMSaveGameData saveGameData)
        {
            try
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
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ApplyLoadedGameData sidecar load failed — {ex.Message}");
            }
        }
    }
}
