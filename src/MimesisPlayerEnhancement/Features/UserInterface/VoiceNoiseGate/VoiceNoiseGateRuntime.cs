using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.VoiceNoiseGate
{
    internal static class VoiceNoiseGateRuntime
    {
        private const string Feature = "Ui";

        private static bool _enabled;
        private static float _strength = VoiceNoiseGateMapper.DefaultStrength;
        private static string? _lastTalkModeName;
        private static bool _applied;
        private static bool _hasCachedBaseline;
        private static object? _cachedVad;
        private static object? _cachedDenoise;
        private static bool _loggedApply;

        internal static void RefreshFromConfig()
        {
            _enabled = ModConfig.EnableVoiceNoiseGate.Value;
            _strength = Mathf.Clamp01(ModConfig.VoiceNoiseGateStrength.Value);
            Reconcile();
        }

        internal static void OnTalkModeChanged(object? mode)
        {
            _lastTalkModeName = mode?.ToString();
            Reconcile();
        }

        internal static void OnSessionEnded()
        {
            RestoreBaseline();
            _lastTalkModeName = null;
            _hasCachedBaseline = false;
            _cachedVad = null;
            _cachedDenoise = null;
            _loggedApply = false;
        }

        private static void Reconcile()
        {
            if (!_enabled || !VoiceNoiseGateMapper.IsVoiceActivationTalkMode(_lastTalkModeName))
            {
                RestoreBaseline();
                return;
            }

            if (!VoiceNoiseGateAccess.TryEnsureInitialized())
            {
                return;
            }

            if (!_hasCachedBaseline && VoiceNoiseGateAccess.TryGetCurrent(out object? vad, out object? denoise))
            {
                _cachedVad = vad;
                _cachedDenoise = denoise;
                _hasCachedBaseline = true;
            }

            VoiceNoiseGateTargets targets = VoiceNoiseGateMapper.MapStrength(_strength);
            if (!VoiceNoiseGateAccess.TryApply(targets.VadSensitivityLevelName, targets.DenoiseLevelName))
            {
                return;
            }

            if (!_applied && !_loggedApply)
            {
                _loggedApply = true;
                ModLog.Info(
                    Feature,
                    $"Voice noise gate active — VAD {targets.VadSensitivityLevelName}, denoise {targets.DenoiseLevelName}");
            }

            _applied = true;
        }

        private static void RestoreBaseline()
        {
            if (!_applied)
            {
                return;
            }

            if (_hasCachedBaseline)
            {
                VoiceNoiseGateAccess.TryRestore(_cachedVad, _cachedDenoise);
            }

            _applied = false;
        }
    }
}
