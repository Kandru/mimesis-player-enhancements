using System;
using MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone.Patches;
using MimesisPlayerEnhancement.Features.DeadPlayerFeatures.MimicPossession;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures
{
    internal static class DeadPlayerFeaturesPatches
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            MimicPossessionPatches.Apply(harmony);
            DeadPlayerPhonePatches.Apply(harmony);

            HarmonyPatchHelper.PatchApplyResult lifecycleResult = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(DeadPlayerFeaturesPatches)));

            HarmonyPatchHelper.LogPatchSummary(Feature, lifecycleResult);
        }

        [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
        internal static class CameraManagerOnEnterDungeonPostfix
        {
            [HarmonyPostfix]
            internal static void Postfix()
            {
                try
                {
                    DeadPlayerFeaturesRuntime.OnDungeonEnter();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnEnterDungeon postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEndDungeon))]
        internal static class CameraManagerOnEndDungeonPrefix
        {
            [HarmonyPrefix]
            internal static void Prefix()
            {
                try
                {
                    DeadPlayerFeaturesRuntime.OnDungeonEnd();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnEndDungeon prefix failed — {ex.Message}");
                }
            }
        }
    }
}
