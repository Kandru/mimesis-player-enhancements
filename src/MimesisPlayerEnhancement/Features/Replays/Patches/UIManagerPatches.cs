using System.Reflection;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(UIManager), "Update")]
    internal static class UiManagerReplayUpdatePrefix
    {
        private const string Feature = "Replays";

        private static Type? _inputActionType;
        private static object? _escapeActionValue;
        private static MethodInfo? _wasPressedThisFrameMethod;
        private static Type? _cachedInputManagerType;

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!ReplaysRuntime.IsEnabled
                && !ReplayPickerController.IsPickerOpen
                && !ReplayPlaybackEngine.IsActive)
            {
                return true;
            }

            try
            {
                if (ReplayPickerController.IsPickerOpen && ReplayPickerController.Panel != null)
                {
                    object? inputman = ReplayGameAccess.TryGetInputManager();
                    if (inputman != null && WasEscapePressed(inputman))
                    {
                        ReplayPickerController.Panel.Close();
                        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                        return false;
                    }
                }

                if (ReplaySharedData.IsReplayPlayMode && ReplayPlaybackEngine.IsActive)
                {
                    object? inputman = ReplayGameAccess.TryGetInputManager();
                    if (inputman != null && WasEscapePressed(inputman))
                    {
                        ReplayPlaybackEngine.ExitToMainMenu();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"UIManager.Update patch failed — {ex.Message}");
            }

            return true;
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
