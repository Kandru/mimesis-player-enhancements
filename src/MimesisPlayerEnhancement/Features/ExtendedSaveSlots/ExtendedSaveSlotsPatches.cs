namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class ExtendedSaveSlotsPatches
    {
        private const string Feature = "ExtendedSaveSlots";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ExtendedSaveSlotsPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("CheckSaveSlotID/MMSaveGameData", AccessTools.Method(typeof(MMSaveGameData), nameof(MMSaveGameData.CheckSaveSlotID))),
                ("GetLoadedSaveData/UIPrefab_LoadTram", AccessTools.Method(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.GetLoadedSaveData))),
                ("IsSlotVersionCompatible/UIPrefab_LoadTram", AccessTools.Method(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.IsSlotVersionCompatible))),
                ("Start/MainMenu", AccessTools.Method(typeof(MainMenu), "Start")),
                ("OnEnable/UIPrefab_MainMenu", AccessTools.Method(typeof(UIPrefab_MainMenu), "OnEnable")),
                ("OnButtonClick/UIPrefabScript", AccessTools.Method(typeof(UIPrefabScript), "OnButtonClick", [typeof(string)])),
                ("OnHostButton/UIPrefab_MainMenu (setter)", AccessTools.Method(typeof(UIPrefab_MainMenu), nameof(UIPrefab_MainMenu.OnHostButton), [typeof(Action<string>)])),
                ("Update/UIManager", AccessTools.Method(typeof(UIManager), "Update")),
            ]);
        }
    }
}
