namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    [HarmonyPatch(typeof(MMSaveGameData), nameof(MMSaveGameData.CheckSaveSlotID))]
    internal static class CheckSaveSlotIdPrefix
    {
        private const string Feature = "ExtendedSaveSlots";

        [HarmonyPrefix]
        private static bool Prefix(int slotID, bool includeAutoSlot, ref bool __result)
        {
            if (!ModConfig.EnableExtendedSaveSlots.Value)
            {
                return true;
            }

            if (slotID == -1)
            {
                __result = false;
                return false;
            }

            if (includeAutoSlot && slotID == SaveSlotLimits.AutosaveSlotId)
            {
                __result = true;
                return false;
            }

            int maxManual = SaveSlotDiscovery.GetMaxManualSlots();
            __result = slotID >= SaveSlotLimits.MinManualSlotId && slotID <= maxManual;
            return false;
        }
    }
}
