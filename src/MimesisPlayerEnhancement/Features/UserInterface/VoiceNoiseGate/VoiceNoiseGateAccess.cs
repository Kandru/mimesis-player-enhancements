using System.Linq;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.VoiceNoiseGate
{
    internal static class VoiceNoiseGateAccess
    {
        private const string Feature = "Ui";
        private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        private static bool _warnedUnavailable;
        private static PropertyInfo? _instanceProperty;
        private static PropertyInfo? _vadProperty;
        private static PropertyInfo? _denoiseProperty;

        internal static bool TryEnsureInitialized()
        {
            if (_instanceProperty != null && _vadProperty != null && _denoiseProperty != null)
            {
                return true;
            }

            Assembly? dissonance = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(
                    assembly.GetName().Name,
                    "DissonanceVoip",
                    StringComparison.Ordinal));
            if (dissonance == null)
            {
                WarnOnce("DissonanceVoip assembly not loaded — voice noise gate unavailable.");
                return false;
            }

            Type? voiceSettingsType = dissonance.GetType("Dissonance.Config.VoiceSettings");
            if (voiceSettingsType == null)
            {
                WarnOnce("Dissonance.Config.VoiceSettings missing — voice noise gate unavailable.");
                return false;
            }

            _instanceProperty = voiceSettingsType.GetProperty("Instance", StaticFlags);
            _vadProperty = voiceSettingsType.GetProperty("VadSensitivity", InstanceFlags)
                ?? voiceSettingsType.GetProperty("VadSensitivityLevel", InstanceFlags);
            _denoiseProperty = voiceSettingsType.GetProperty("DenoiseAmount", InstanceFlags)
                ?? voiceSettingsType.GetProperty("NoiseSuppressionLevel", InstanceFlags);

            if (_instanceProperty == null || _vadProperty == null || _denoiseProperty == null)
            {
                WarnOnce("VoiceSettings voice tuning properties missing — voice noise gate unavailable.");
                _instanceProperty = null;
                _vadProperty = null;
                _denoiseProperty = null;
                return false;
            }

            return true;
        }

        internal static bool TryGetCurrent(out object? vad, out object? denoise)
        {
            vad = null;
            denoise = null;
            if (!TryEnsureInitialized()
                || _instanceProperty?.GetValue(null) is not object instance)
            {
                return false;
            }

            try
            {
                vad = _vadProperty!.GetValue(instance);
                denoise = _denoiseProperty!.GetValue(instance);
                return vad != null && denoise != null;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice noise gate read failed — {ex.Message}");
                return false;
            }
        }

        internal static bool TryApply(string vadLevelName, string denoiseLevelName)
        {
            if (!TryEnsureInitialized()
                || _instanceProperty?.GetValue(null) is not object instance)
            {
                return false;
            }

            try
            {
                object? vadValue = ParseEnumValue(_vadProperty!.PropertyType, vadLevelName);
                object? denoiseValue = ParseEnumValue(_denoiseProperty!.PropertyType, denoiseLevelName);
                if (vadValue == null || denoiseValue == null)
                {
                    return false;
                }

                _vadProperty.SetValue(instance, vadValue);
                _denoiseProperty.SetValue(instance, denoiseValue);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice noise gate apply failed — {ex.Message}");
                return false;
            }
        }

        internal static bool TryRestore(object? vad, object? denoise)
        {
            if (!TryEnsureInitialized()
                || _instanceProperty?.GetValue(null) is not object instance
                || vad == null
                || denoise == null)
            {
                return false;
            }

            try
            {
                _vadProperty!.SetValue(instance, vad);
                _denoiseProperty!.SetValue(instance, denoise);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice noise gate restore failed — {ex.Message}");
                return false;
            }
        }

        private static object? ParseEnumValue(Type enumType, string name)
        {
            if (!enumType.IsEnum)
            {
                return null;
            }

            try
            {
                return Enum.Parse(enumType, name, ignoreCase: false);
            }
            catch (ArgumentException)
            {
                ModLog.Warn(Feature, $"Voice noise gate enum value missing — {enumType.Name}.{name}");
                return null;
            }
        }

        private static void WarnOnce(string message)
        {
            if (_warnedUnavailable)
            {
                return;
            }

            _warnedUnavailable = true;
            ModLog.Warn(Feature, message);
        }
    }
}
