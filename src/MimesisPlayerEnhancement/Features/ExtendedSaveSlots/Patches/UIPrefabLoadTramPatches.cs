namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.GetLoadedSaveData))]
    internal static class GetLoadedSaveDataPostfix
    {
        [HarmonyPostfix]
        private static void Postfix(int slotID, ref MMSaveGameData __result)
        {
            if (!TramSavePickerController.IsActive)
            {
                return;
            }

            if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
            {
                __result = cached;
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.IsSlotVersionCompatible))]
    internal static class IsSlotVersionCompatiblePostfix
    {
        [HarmonyPostfix]
        private static void Postfix(int slotID, ref bool __result)
        {
            if (!TramSavePickerController.IsActive)
            {
                return;
            }

            if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
            {
                __result = cached.Version >= 1;
            }
        }
    }
}
