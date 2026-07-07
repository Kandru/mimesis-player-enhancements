using System.IO;

namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    [HarmonyPatch(typeof(PlatformMgr), nameof(PlatformMgr.Delete))]
    public static class PlatformMgrPatches
    {
        private const string Feature = "Persistence";

        [HarmonyPostfix]
        public static void Postfix(string fileName)
        {
            if (!ModConfig.EnablePersistence.Value)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("MMGameData", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string slotStr = Path.GetFileNameWithoutExtension(fileName).Replace("MMGameData", "");
                if (int.TryParse(slotStr, out int slotId) && MMSaveGameData.CheckSaveSlotID(slotId, true))
                {
                    MimesisSaveManager.DeleteMimesisData(slotId);
                    ModLog.Info(Feature, $"Deleted persisted voice data for slot {slotId}.");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PlatformMgr.Delete: {ex.Message}");
            }
        }
    }
}
