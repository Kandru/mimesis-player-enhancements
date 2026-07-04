namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
    public static class PersistenceGameSessionInfoLoadPatches
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                return;
            }

            if (!MimesisSaveManager.IsHost())
            {
                return;
            }

            int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            SpeechEventArchivePatches.EnsurePoolLoaded(slotId);
        }
    }
}
