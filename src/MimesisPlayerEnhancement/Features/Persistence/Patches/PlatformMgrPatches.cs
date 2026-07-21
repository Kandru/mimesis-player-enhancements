namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(PlatformMgr), nameof(PlatformMgr.Delete))]
    internal static class PlatformMgrPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPostfix]
        public static void Postfix(string fileName)
        {
            try
            {
                if (!PlatformMgrSlotParser.TryParseSlotIdFromGameDataFile(fileName, out int slotId))
                {
                    return;
                }

                if (MMSaveGameData.CheckSaveSlotID(slotId, true))
                {
                    SaveSidecarPaths.DeleteAllFilesForSlot(slotId, Feature);
                    SpeechEventArchivePatches.InvalidatePoolLoaded();
                    ModLog.Info(Feature, $"Deleted all mod files for slot {slotId}.");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PlatformMgr.Delete: {ex.Message}");
            }
        }
    }
}
