using HarmonyLib;

namespace MimesisSeedScanner.Mod
{
    internal static class SeedScannerInput
    {
        private static bool _initialized;
        private static Func<bool>? _wasF10Pressed;
        private static bool _warnedUnavailable;

        internal static bool WasScanTriggerPressedThisFrame()
        {
            EnsureInitialized();
            return _wasF10Pressed?.Invoke() == true;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _wasF10Pressed = TryCreateKeyProbe("f10Key");
            if (_wasF10Pressed == null && !_warnedUnavailable)
            {
                _warnedUnavailable = true;
                MelonLogger.Warning("Seed scanner keyboard trigger unavailable — Unity Input System keyboard not found.");
            }
        }

        private static Func<bool>? TryCreateKeyProbe(string keyPropertyName)
        {
            Type? keyboardType = AccessTools.TypeByName("UnityEngine.InputSystem.Keyboard");
            if (keyboardType == null)
            {
                return null;
            }

            PropertyInfo? currentProperty = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            PropertyInfo? keyProperty = keyboardType.GetProperty(keyPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (currentProperty == null || keyProperty == null)
            {
                return null;
            }

            PropertyInfo? wasPressedProperty = null;
            return () =>
            {
                try
                {
                    if (currentProperty.GetValue(null) is not { } keyboard
                        || keyProperty.GetValue(keyboard) is not { } keyControl)
                    {
                        return false;
                    }

                    wasPressedProperty ??= keyControl.GetType().GetProperty(
                        "wasPressedThisFrame",
                        BindingFlags.Public | BindingFlags.Instance);
                    return wasPressedProperty?.GetValue(keyControl) is true;
                }
                catch
                {
                    return false;
                }
            };
        }
    }
}
