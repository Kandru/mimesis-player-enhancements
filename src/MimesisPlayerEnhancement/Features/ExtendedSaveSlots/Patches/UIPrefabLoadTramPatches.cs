namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_LoadTram.cs:L305-313
    [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.GetLoadedSaveData))]
    internal static class GetLoadedSaveDataPrefix
    {
        [HarmonyPrefix]
        private static bool Prefix(int slotID, ref MMSaveGameData __result)
        {
            if (!TramSavePickerController.IsActive)
            {
                return true;
            }

            if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
            {
                __result = cached;
                return false;
            }

            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_LoadTram.cs:L295-303
    [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.IsSlotVersionCompatible))]
    internal static class IsSlotVersionCompatiblePrefix
    {
        [HarmonyPrefix]
        private static bool Prefix(int slotID, ref bool __result)
        {
            if (!TramSavePickerController.IsActive)
            {
                return true;
            }

            if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
            {
                __result = cached.Version >= 1;
                return false;
            }

            return true;
        }
    }
}
