namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    // game@0.3.2 Assembly-CSharp/UIPrefab_MainMenu.cs:L453-456
    [HarmonyPatch(typeof(UIPrefab_MainMenu), "OnEnable")]
    internal static class MainMenuOnEnablePostfix
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_MainMenu __instance)
        {
            TramSavePickerController.OnMainMenuShown(__instance);
        }
    }

    // game@0.3.2 Assembly-CSharp/UIPrefab_MainMenu.cs:L181-192
    [HarmonyPatch(typeof(UIPrefab_MainMenu), nameof(UIPrefab_MainMenu.OnHostButton), MethodType.Setter)]
    internal static class MainMenuHostButtonSetterPrefix
    {
        [HarmonyPrefix]
        private static void Prefix(ref Action<string> value)
        {
            if (!TramSavePickerController.IsActive)
            {
                return;
            }

            value = static _ => { };
        }
    }
}
