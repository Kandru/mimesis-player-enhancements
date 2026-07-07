using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicVoiceTuningPatchSupport
    {
        internal static readonly FieldInfo? AsServerField =
            AccessTools.Field(typeof(VoiceManager), "_asServer");

        internal static readonly FieldInfo? LastMimicVoiceTimeField =
            AccessTools.Field(typeof(VoiceManager), "_lastMimicVoiceTime");

        internal static readonly MethodInfo? GetActorByPlayerUidMethod =
            AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.GetActorByPlayerUID));

        internal static readonly MethodInfo? StartCoroutineMethod =
            AccessTools.Method(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), [typeof(IEnumerator)]);

        internal static readonly MethodInfo? SpawnMimicVoiceWithDelayMethod =
            AccessTools.Method(typeof(VoiceManager), "SpawnMimicVoiceWithDelay");

        internal static bool TryGetLastMimicVoiceTime(VoiceManager instance, out float lastTime)
        {
            lastTime = -1f;
            if (LastMimicVoiceTimeField == null)
            {
                return false;
            }

            lastTime = (float)LastMimicVoiceTimeField.GetValue(instance)!;
            return true;
        }

        internal static void SetLastMimicVoiceTime(VoiceManager instance, float value)
        {
            LastMimicVoiceTimeField?.SetValue(instance, value);
        }

        internal static bool IsServer(VoiceManager instance)
        {
            return AsServerField != null && (bool)AsServerField.GetValue(instance)!;
        }
    }
}
