using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots.Patches
{
    // game@0.3.1 Assembly-CSharp/UIManager.cs:L998-1163
    [HarmonyPatch(typeof(UIManager), "Update")]
    internal static class UiManagerUpdateEscapePrefix
    {
        // game@0.3.1 Mimic.InputSystem.InputAction Escape + inputman.wasPressedThisFrame
        private static Type? _inputActionType;
        private static object? _escapeActionValue;
        private static MethodInfo? _wasPressedThisFrameMethod;
        private static Type? _cachedInputManagerType;

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!TramSavePickerController.IsActive
                || !TramSavePickerController.IsSavePickerOpen
                || TramSavePickerController.Panel == null)
            {
                return true;
            }

            object? inputman = SaveSlotGameAccess.TryGetInputManager();
            if (inputman == null || !WasEscapePressed(inputman))
            {
                return true;
            }

            TramSavePickerController.Panel.Close();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            return false;
        }

        private static bool WasEscapePressed(object inputman)
        {
            EnsureInputReflectionCached(inputman);
            return _wasPressedThisFrameMethod != null
                && _escapeActionValue != null
                && _wasPressedThisFrameMethod.Invoke(inputman, [_escapeActionValue]) is true;
        }

        private static void EnsureInputReflectionCached(object inputman)
        {
            Type inputManagerType = inputman.GetType();
            if (_cachedInputManagerType == inputManagerType
                && _wasPressedThisFrameMethod != null
                && _escapeActionValue != null)
            {
                return;
            }

            _cachedInputManagerType = inputManagerType;
            _inputActionType ??= AccessTools.TypeByName("Mimic.InputSystem.InputAction");
            if (_inputActionType == null)
            {
                _wasPressedThisFrameMethod = null;
                _escapeActionValue = null;
                return;
            }

            _escapeActionValue ??= Enum.Parse(_inputActionType, "Escape");
            _wasPressedThisFrameMethod = AccessTools.Method(
                inputManagerType,
                "wasPressedThisFrame",
                [_inputActionType]);
        }
    }
}
