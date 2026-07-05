using System.Reflection;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures
{
    /// <summary>
    /// Reflection-based Hub access for internal game managers (cameraman, voiceman, main).
    /// </summary>
    internal static class DeadPlayerFeaturesGameAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _cameramanField;
        private static PropertyInfo? _cameramanProperty;
        private static FieldInfo? _voicemanField;
        private static PropertyInfo? _voicemanProperty;

        internal static CameraManager? TryGetCameraManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            _cameramanProperty ??= typeof(Hub).GetProperty("cameraman", InstanceFlags);
            if (_cameramanProperty?.GetValue(Hub.s) is CameraManager propertyManager)
            {
                return propertyManager;
            }

            _cameramanField ??= typeof(Hub).GetField("cameraman", InstanceFlags)
                ?? typeof(Hub).GetField("<cameraman>k__BackingField", InstanceFlags);
            return _cameramanField?.GetValue(Hub.s) as CameraManager;
        }

        internal static VoiceManager? TryGetVoiceManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            _voicemanProperty ??= typeof(Hub).GetProperty("voiceman", InstanceFlags);
            if (_voicemanProperty?.GetValue(Hub.s) is VoiceManager propertyManager)
            {
                return propertyManager;
            }

            _voicemanField ??= typeof(Hub).GetField("voiceman", InstanceFlags)
                ?? typeof(Hub).GetField("<voiceman>k__BackingField", InstanceFlags);
            return _voicemanField?.GetValue(Hub.s) as VoiceManager;
        }

        internal static GameMainBase? TryGetMain()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata?.main ?? Hub.Main;
        }

        internal static CancellationToken GetDestroyToken(GameMainBase main) =>
            main.destroyCancellationToken;
    }
}
