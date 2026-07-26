using Mimic.InputSystem;

namespace MimesisPlayerEnhancement.Features.UserInterface.InventoryNumberKeys.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.InputSystem/InputManager.cs:L526-535
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.PressKey))]
    internal static class InputManagerPressKeyPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(InputAction action)
        {
            try
            {
                InventoryNumberKeyHotkeys.MarkSyntheticPress(action);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Inventory number key synthetic latch failed — {ex.Message}");
            }
        }
    }
}
