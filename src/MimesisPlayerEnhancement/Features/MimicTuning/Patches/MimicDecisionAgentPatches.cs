using System.Reflection;
using DLAgent;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicEmoteProps;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicSocial;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicTrust;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicDecisionAgentPatchSupport
    {
        internal const string Feature = "MimicTuning";

        internal static readonly FieldInfo? TrustScoreInitialField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreInitial");

        internal static readonly FieldInfo? TrustScoreBehaviorTrustThresholdField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreBehaviorTrustThreshold");

        internal static readonly FieldInfo? OutdoorTrustScoreMultiplierField =
            AccessTools.Field(typeof(DLDecisionAgent), "_outdoorTrustScoreMultiplier");

        internal static readonly FieldInfo? TrustScoreLookingDeltaField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreLookingDelta");

        internal static readonly FieldInfo? TrustScoreNotLookingDeltaField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreNotLookingDelta");

        internal static readonly FieldInfo? TrustScoreApproachDeltaField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreApproachDelta");

        internal static readonly FieldInfo? TrustScoreMaintainDeltaField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreMaintainDelta");

        internal static readonly FieldInfo? TrustScoreWalkAwayDeltaField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreWalkAwayDelta");

        internal static readonly FieldInfo? TrustScoreSprintAwayDeltaField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreSprintAwayDelta");

        internal static readonly FieldInfo? TrustScoreHitDamageMultiplierField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreHitDamageMultiplier");

        internal static readonly FieldInfo? TrustScoreFriendlyThresholdField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreFriendlyThreshold");

        internal static readonly FieldInfo? TrustScoreDistrustThresholdField =
            AccessTools.Field(typeof(DLDecisionAgent), "_trustScoreDistrustThreshold");

        internal static readonly FieldInfo? ChaseModeActivationDistanceField =
            AccessTools.Field(typeof(DLDecisionAgent), "_chaseModeActivationDistance");

        internal static readonly FieldInfo? ChaseForceRunDistanceField =
            AccessTools.Field(typeof(DLDecisionAgent), "_chaseForceRunDistance");

        internal static readonly FieldInfo? MimicRunawayProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_mimicRunawayProb");

        internal static readonly FieldInfo? JumpRespondProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_jumpRespondProb");

        internal static readonly FieldInfo? CopyTargetSlotChangeProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_copyTargetSlotChangeProb");

        internal static readonly FieldInfo? EmoteRespondProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_emoteRespondProb");

        internal static readonly FieldInfo? EmoteSuggestProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_emoteSuggestProb");

        internal static readonly FieldInfo? ReactToSprinklerProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_reactToSprinklerProb");

        internal static readonly FieldInfo? UseTrapSwitchProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_useTrapSwitchProb");

        internal static readonly FieldInfo? UseChargerProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_useChargerProb");

        internal static readonly FieldInfo? UseTransmitterProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_useTransmitterProb");

        internal static readonly FieldInfo? UseShutterSwitchProbField =
            AccessTools.Field(typeof(DLDecisionAgent), "_useShutterSwitchProb");

        internal static void ApplyTuning(DLDecisionAgent agent)
        {
            if (MimicTrustResolver.ShouldApplyCustom)
            {
                ApplyTrustTuning(agent);
            }

            if (MimicSocialResolver.ShouldApplyCustom)
            {
                ApplySocialTuning(agent);
            }

            if (MimicEmotePropsResolver.ShouldApplyCustom)
            {
                ApplyEmotePropsTuning(agent);
            }
        }

        internal static void ApplyTuningToAllActiveAgents()
        {
            if (!MimicTrustResolver.ShouldApplyCustom
                && !MimicSocialResolver.ShouldApplyCustom
                && !MimicEmotePropsResolver.ShouldApplyCustom)
            {
                return;
            }

            foreach (DLDecisionAgent agent in UnityEngine.Object.FindObjectsByType<DLDecisionAgent>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (agent != null)
                {
                    ApplyTuning(agent);
                }
            }
        }

        private static void ApplyTrustTuning(DLDecisionAgent agent)
        {
            SetFloat(OutdoorTrustScoreMultiplierField, agent, MimicTrustResolver.OutdoorMultiplier);
            SetFloat(TrustScoreLookingDeltaField, agent, MimicTrustResolver.LookingDelta);
            SetFloat(TrustScoreNotLookingDeltaField, agent, MimicTrustResolver.NotLookingDelta);
            SetFloat(TrustScoreApproachDeltaField, agent, MimicTrustResolver.ApproachDelta);
            SetFloat(TrustScoreMaintainDeltaField, agent, MimicTrustResolver.MaintainDelta);
            SetFloat(TrustScoreWalkAwayDeltaField, agent, MimicTrustResolver.WalkAwayDelta);
            SetFloat(TrustScoreSprintAwayDeltaField, agent, MimicTrustResolver.SprintAwayDelta);
            SetFloat(TrustScoreHitDamageMultiplierField, agent, MimicTrustResolver.HitDamageMultiplier);
            SetFloat(TrustScoreFriendlyThresholdField, agent, MimicTrustResolver.FriendlyThreshold);
            SetFloat(TrustScoreDistrustThresholdField, agent, MimicTrustResolver.DistrustThreshold);
            SetFloat(ChaseModeActivationDistanceField, agent, MimicTrustResolver.ChaseActivationDistance);
            SetFloat(ChaseForceRunDistanceField, agent, MimicTrustResolver.ChaseForceRunDistance);

            if (MimicTrustResolver.IsCustomScoreMode)
            {
                float initial = MimicTrustResolver.ResolveInitialTrust(
                    ReadFloat(TrustScoreInitialField, agent, MimicTrustResolver.VanillaInitialTrust));
                SetFloat(TrustScoreInitialField, agent, initial);
                float behavior = MimicTrustResolver.ResolveBehaviorTrust(
                    ReadFloat(
                        TrustScoreBehaviorTrustThresholdField,
                        agent,
                        MimicTrustResolver.VanillaBehaviorTrust));
                SetFloat(TrustScoreBehaviorTrustThresholdField, agent, behavior);
            }
        }

        private static void ApplySocialTuning(DLDecisionAgent agent)
        {
            SetFloat(MimicRunawayProbField, agent, MimicSocialResolver.RunawayChance);
            SetFloat(JumpRespondProbField, agent, MimicSocialResolver.JumpCopyChance);
            SetFloat(CopyTargetSlotChangeProbField, agent, MimicSocialResolver.SlotFollowChangeChance);
        }

        private static void ApplyEmotePropsTuning(DLDecisionAgent agent)
        {
            SetFloat(EmoteRespondProbField, agent, MimicEmotePropsResolver.EmoteRespondChance);
            SetFloat(EmoteSuggestProbField, agent, MimicEmotePropsResolver.EmoteSuggestChance);
            SetFloat(ReactToSprinklerProbField, agent, MimicEmotePropsResolver.ReactToSprinklerChance);
            SetFloat(UseTrapSwitchProbField, agent, MimicEmotePropsResolver.UseTrapSwitchChance);
            SetFloat(UseChargerProbField, agent, MimicEmotePropsResolver.UseChargerChance);
            SetFloat(UseTransmitterProbField, agent, MimicEmotePropsResolver.UseTransmitterChance);
            SetFloat(UseShutterSwitchProbField, agent, MimicEmotePropsResolver.UseShutterSwitchChance);
        }

        private static float ReadFloat(FieldInfo? field, DLDecisionAgent agent, float fallback)
        {
            if (field == null)
            {
                return fallback;
            }

            return (float)field.GetValue(agent)!;
        }

        private static void SetFloat(FieldInfo? field, DLDecisionAgent agent, float value)
        {
            field?.SetValue(agent, value);
        }
    }

    [HarmonyPatch(typeof(DLDecisionAgent), "OnEnable")]
    internal static class DLDecisionAgentOnEnablePostfix
    {
        [HarmonyPostfix]
        internal static void Postfix(DLDecisionAgent __instance)
        {
            try
            {
                MimicDecisionAgentPatchSupport.ApplyTuning(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicDecisionAgentPatchSupport.Feature, $"DLDecisionAgent OnEnable postfix failed — {ex.Message}");
            }
        }
    }
}
