namespace MimesisPlayerEnhancement.Features.SavegamePreparation.Patches
{
    // game@0.3.1 Assembly-CSharp/MainMenu.cs:L87-104
    [HarmonyPatch(typeof(MainMenu), "CreateNewGameInSlot", typeof(UIPrefab_NewTram), typeof(int))]
    internal static class MainMenuCreateNewGameInSlotNewTramPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SavegamePreparationApplier.OnCreateNewGameInSlot();
        }
    }

    [HarmonyPatch(typeof(MainMenu), "CreateNewGameInSlot", typeof(UIPrefab_LoadTram), typeof(int))]
    internal static class MainMenuCreateNewGameInSlotLoadTramPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SavegamePreparationApplier.OnCreateNewGameInSlot();
        }
    }
}
