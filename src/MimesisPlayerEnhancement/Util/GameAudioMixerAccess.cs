using System.Reflection;
using UnityEngine.Audio;

namespace MimesisPlayerEnhancement.Util
{
    internal static class GameAudioMixerAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] SfxGroupCandidates = ["SFX", "Sfx", "Sound", "Master"];
        private static FieldInfo? _audiomanField;
        private static PropertyInfo? _audiomanProperty;
        private static PropertyInfo? _audioMixerProperty;
        private static AudioMixerGroup? _cachedGroup;
        private static bool _resolved;

        internal static AudioMixerGroup? TryResolveSfxGroup()
        {
            if (_resolved)
            {
                return _cachedGroup;
            }

            _resolved = true;
            try
            {
                object? audioManager = TryGetAudioManager();
                if (audioManager == null)
                {
                    return null;
                }

                AudioMixer? mixer = TryGetAudioMixer(audioManager);
                if (mixer == null)
                {
                    return null;
                }

                for (int i = 0; i < SfxGroupCandidates.Length; i++)
                {
                    AudioMixerGroup[] groups = mixer.FindMatchingGroups(SfxGroupCandidates[i]);
                    if (groups.Length > 0 && groups[0] != null)
                    {
                        _cachedGroup = groups[0];
                        ModLog.Debug("Ui", $"SFX mixer group resolved — {SfxGroupCandidates[i]}");
                        return _cachedGroup;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Debug("Ui", $"SFX mixer group lookup failed — {ex.Message}");
            }

            return null;
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

        private static AudioMixer? TryGetAudioMixer(object audioManager)
        {
            _audioMixerProperty ??= audioManager.GetType().GetProperty("AudioMixer", InstanceFlags);
            return _audioMixerProperty?.GetValue(audioManager) as AudioMixer;
        }
    }
}
