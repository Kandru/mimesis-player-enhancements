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

            FieldInfo? field = typeof(Hub).GetField("audioman", InstanceFlags)
                ?? typeof(Hub).GetField("<audioman>k__BackingField", InstanceFlags);
            PropertyInfo? property = typeof(Hub).GetProperty("audioman", InstanceFlags);
            object? audioManager = field?.GetValue(Hub.s) ?? property?.GetValue(Hub.s);
            if (audioManager == null)
            {
                return;
            }

            MethodInfo? playSfx = audioManager.GetType().GetMethod(
                "PlaySfx",
                InstanceFlags,
                binder: null,
                [typeof(string)],
                modifiers: null);
            playSfx?.Invoke(audioManager, [sfxId]);
        }
    }
}
