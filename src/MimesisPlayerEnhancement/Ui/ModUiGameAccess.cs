using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Reflection-based access to the game's UI/audio managers used by the shared UI toolkit.
    /// </summary>
    internal static class ModUiGameAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _uimanField;
        private static PropertyInfo? _uimanProperty;
        private static FieldInfo? _audiomanField;
        private static PropertyInfo? _audiomanProperty;
        private static MethodInfo? _playSfxMethod;

        internal static UIManager? TryGetUiManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            _uimanProperty ??= typeof(Hub).GetProperty("uiman", InstanceFlags);
            if (_uimanProperty?.GetValue(Hub.s) is UIManager propertyManager)
            {
                return propertyManager;
            }

            _uimanField ??= typeof(Hub).GetField("uiman", InstanceFlags)
                ?? typeof(Hub).GetField("<uiman>k__BackingField", InstanceFlags);
            return _uimanField?.GetValue(Hub.s) as UIManager;
        }

        internal static void TryPlaySfx(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId) || Hub.s == null || !Application.isFocused)
            {
                return;
            }

            object? audioManager = TryGetAudioManager();
            if (audioManager == null)
            {
                return;
            }

            _playSfxMethod ??= audioManager.GetType().GetMethod(
                "PlaySfx",
                InstanceFlags,
                binder: null,
                [typeof(string)],
                modifiers: null);
            _playSfxMethod?.Invoke(audioManager, [sfxId]);
        }

        private static object? TryGetAudioManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            _audiomanProperty ??= typeof(Hub).GetProperty("audioman", InstanceFlags);
            object? fromProperty = _audiomanProperty?.GetValue(Hub.s);
            if (fromProperty != null)
            {
                return fromProperty;
            }

            _audiomanField ??= typeof(Hub).GetField("audioman", InstanceFlags)
                ?? typeof(Hub).GetField("<audioman>k__BackingField", InstanceFlags);
            return _audiomanField?.GetValue(Hub.s);
        }
    }
}
