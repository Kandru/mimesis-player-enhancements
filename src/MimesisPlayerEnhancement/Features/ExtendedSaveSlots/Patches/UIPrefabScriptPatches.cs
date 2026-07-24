namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefabScript.cs:L154-179
    [HarmonyPatch(typeof(UIPrefabScript), "OnButtonClick", typeof(string))]
    internal static class MainMenuHostButtonClickPrefix
    {
        [HarmonyPrefix]
        private static bool Prefix(UIPrefabScript __instance, string _id)
        {
            if (__instance is not UIPrefab_MainMenu mainMenuUi)
            {
                return true;
            }

            if (!MainMenuButtonWiring.IsHostButtonElement(__instance, _id, mainMenuUi.UE_HostButton))
            {
                return true;
            }

            if (!TramSavePickerController.IsActive)
            {
                return true;
            }

            TramSavePickerController.TryHandleHostButtonClick(mainMenuUi);
            return false;
        }
    }
}
