using System.Reflection;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicHorn;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicHornPatchSupport
    {
        internal const string Feature = "MimicTuning";

        internal static readonly FieldInfo? MaxDurationField =
            AccessTools.Field(typeof(MimicHornRecorder), "MAX_DURATION");

        internal static readonly FieldInfo? RecordingIntervalField =
            AccessTools.Field(typeof(MimicHornRecorder), "RECORDING_INTERVAL");

        internal static readonly FieldInfo? MaxRecordsCountField =
            AccessTools.Field(typeof(MimicHornRecorder), "MAX_RECORDS_COUNT");

        internal static void ApplyRecorderTuning(MimicHornRecorder recorder)
        {
            if (MimicHornResolver.ShouldApplyCustom)
            {
                MaxDurationField?.SetValue(recorder, MimicHornResolver.MaxRecordSeconds);
                RecordingIntervalField?.SetValue(recorder, MimicHornResolver.RecordingGapSeconds);
                MaxRecordsCountField?.SetValue(recorder, MimicHornResolver.MaxStoredRecords);
                return;
            }

            MaxDurationField?.SetValue(recorder, MimicHornResolver.VanillaMaxRecordSeconds);
            RecordingIntervalField?.SetValue(recorder, MimicHornResolver.VanillaRecordingGapSeconds);
            MaxRecordsCountField?.SetValue(recorder, MimicHornResolver.VanillaMaxStoredRecords);
        }

        internal static void ApplyRecorderTuningToAllVoiceManagers()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            FieldInfo? recorderField = AccessTools.Field(typeof(VoiceManager), "_mimicHornRecorder");
            if (recorderField == null)
            {
                return;
            }

            foreach (VoiceManager voiceManager in UnityEngine.Object.FindObjectsByType<VoiceManager>(
                         UnityEngine.FindObjectsInactive.Exclude,
                         UnityEngine.FindObjectsSortMode.None))
            {
                if (voiceManager != null
                    && recorderField.GetValue(voiceManager) is MimicHornRecorder recorder)
                {
                    ApplyRecorderTuning(recorder);
                }
            }
        }
    }

    [HarmonyPatch(typeof(VoiceManager), "Awake")]
    internal static class VoiceManagerAwakeHornPostfix
    {
        [HarmonyPostfix]
        internal static void Postfix(VoiceManager __instance)
        {
            try
            {
                FieldInfo? recorderField = AccessTools.Field(typeof(VoiceManager), "_mimicHornRecorder");
                if (recorderField?.GetValue(__instance) is MimicHornRecorder recorder)
                {
                    MimicHornPatchSupport.ApplyRecorderTuning(recorder);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicHornPatchSupport.Feature, $"VoiceManager Awake horn postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(AIController), nameof(AIController.PlayMimicRandomHorn))]
    internal static class PlayMimicRandomHornPrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref float __result)
        {
            try
            {
                if (MimicHornResolver.ShouldApplyCustom && !MimicHornResolver.AllowHornImitation)
                {
                    __result = -1f;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicHornPatchSupport.Feature, $"PlayMimicRandomHorn prefix failed — {ex.Message}");
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
    internal static class CameraManagerOnEnterDungeonPossessionRangePostfix
    {
        [HarmonyPostfix]
        internal static void Postfix(CameraManager __instance)
        {
            try
            {
                if (!MimicPossessionResolver.ShouldOverridePossessionRange())
                {
                    return;
                }

                FieldInfo? field = AccessTools.Field(typeof(CameraManager), "_sqrPossessionDistanceMax");
                field?.SetValue(__instance, MimicPossessionResolver.GetPossessionRangeSqrMeters());
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicHornPatchSupport.Feature, $"CameraManager possession range postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PossessionController), "IsPossessable")]
    internal static class PossessionControllerIsPossessablePrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(PossessionController __instance, ref bool __result)
        {
            try
            {
                if (!MimicPossessionResolver.ShouldBypassBtGate)
                {
                    return true;
                }

                FieldInfo? roleTypeField = AccessTools.Field(typeof(PossessionController), "RoleType");
                FieldInfo? selfField = AccessTools.Field(typeof(PossessionController), "_self");
                if (roleTypeField == null || selfField == null)
                {
                    return true;
                }

                if ((PossessionRoleType)roleTypeField.GetValue(__instance)! != PossessionRoleType.Possessed)
                {
                    __result = false;
                    return false;
                }

                if (selfField.GetValue(__instance) is not VCreature self || !self.IsAliveStatus())
                {
                    __result = false;
                    return false;
                }

                if (self.AbnormalControlUnit != null && self.AbnormalControlUnit.IsSilenced())
                {
                    __result = false;
                    return false;
                }

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicHornPatchSupport.Feature, $"IsPossessable prefix failed — {ex.Message}");
                return true;
            }
        }
    }
}
