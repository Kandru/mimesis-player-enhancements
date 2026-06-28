using System;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.SpectatorTransition;

public static class SpectatorTransitionPatches
{
    private const string Feature = "SpectatorTransition";

    private static readonly Type PlayerActorType =
        AccessTools.Inner(typeof(GameConfig), "PlayerActor")!;

    private static readonly PropertyInfo GameConfigProperty =
        AccessTools.Property(typeof(GameMainBase), "gameConfig")!;

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        _ = GameNetworkApi.GetGameAssembly();

        var result = HarmonyPatchHelper.ApplyPatchTypes(
            harmony,
            Feature,
            HarmonyPatchHelper.GetNestedPatchTypes(typeof(SpectatorTransitionPatches)));

        LogPatchAudit(harmony);
        HarmonyPatchHelper.LogPatchSummary(Feature, result);
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        HarmonyPatchHelper.LogPatchAudit(Feature, harmony, new (string, MethodBase?)[]
        {
            ("get_DyingWaitTime/VCreature", AccessTools.PropertyGetter(typeof(VCreature), nameof(VCreature.DyingWaitTime))),
            ("get_deadCameraTotalDuration/PlayerActor", AccessTools.PropertyGetter(PlayerActorType, "deadCameraTotalDuration")),
            ("CorDying/GameMainBase", AccessTools.Method(typeof(GameMainBase), "CorDying")),
            ("OnPlayerDeathEnterSpectator/GameMainBase", AccessTools.Method(typeof(GameMainBase), "OnPlayerDeathEnterSpectator")),
            ("OnPlayerRevive/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.OnPlayerRevive))),
        });
    }

    [HarmonyPatch(typeof(VCreature), nameof(VCreature.DyingWaitTime), MethodType.Getter)]
    public static class VCreatureDyingWaitTimePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref long __result)
        {
            try
            {
                if (!SpectatorTransitionApplier.IsEnabled || !HostApplyGate.ShouldApplyHostOnlyFeature())
                    return;

                __result = SpectatorTransitionApplier.ScaleDyingWaitTime(__result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"get_DyingWaitTime postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class PlayerActorDeadCameraTotalDurationPatch
    {
        static MethodBase TargetMethod() =>
            AccessTools.PropertyGetter(PlayerActorType, "deadCameraTotalDuration")!;

        [HarmonyPostfix]
        public static void Postfix(ref float __result)
        {
            try
            {
                if (!SpectatorTransitionApplier.IsEnabled || DeadCameraFieldRestore.FieldsAreScaled)
                    return;

                __result = SpectatorTransitionApplier.ScaleDeadCameraDuration(__result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"get_deadCameraTotalDuration postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "CorDying")]
    public static class GameMainBaseCorDyingPatch
    {
        private static readonly FieldInfo BlendTimeField =
            AccessTools.Field(PlayerActorType, "deadCameraBlendTime")!;

        private static readonly FieldInfo DurationField =
            AccessTools.Field(PlayerActorType, "deadCameraDuration")!;

        private static readonly FieldInfo PlayerActorField =
            AccessTools.Field(typeof(GameConfig), "playerActor")!;

        [HarmonyPrefix]
        public static void Prefix(GameMainBase __instance)
        {
            try
            {
                if (!SpectatorTransitionApplier.IsEnabled)
                    return;

                var gameConfig = (GameConfig?)GameConfigProperty.GetValue(__instance);
                if (gameConfig == null)
                    return;

                var playerActor = PlayerActorField.GetValue(gameConfig);
                if (playerActor == null)
                    return;

                float blend = (float)BlendTimeField.GetValue(playerActor)!;
                float duration = (float)DurationField.GetValue(playerActor)!;

                DeadCameraFieldRestore.Save(__instance, blend, duration);

                BlendTimeField.SetValue(playerActor, SpectatorTransitionApplier.ScaleDeadCameraDuration(blend));
                DurationField.SetValue(playerActor, SpectatorTransitionApplier.ScaleDeadCameraDuration(duration));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"CorDying prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "OnPlayerDeathEnterSpectator")]
    public static class GameMainBaseOnPlayerDeathEnterSpectatorPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameMainBase __instance)
        {
            try
            {
                DeadCameraFieldRestore.Restore(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnPlayerDeathEnterSpectator postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerRevive))]
    public static class GameMainBaseOnPlayerRevivePatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameMainBase __instance)
        {
            try
            {
                DeadCameraFieldRestore.Restore(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnPlayerRevive postfix failed — {ex.Message}");
            }
        }
    }

    private static class DeadCameraFieldRestore
    {
        private static readonly FieldInfo BlendTimeField =
            AccessTools.Field(PlayerActorType, "deadCameraBlendTime")!;

        private static readonly FieldInfo DurationField =
            AccessTools.Field(PlayerActorType, "deadCameraDuration")!;

        private static readonly FieldInfo PlayerActorField =
            AccessTools.Field(typeof(GameConfig), "playerActor")!;

        private static GameMainBase? _instance;
        private static float _blendTime;
        private static float _duration;
        private static bool _saved;

        internal static bool FieldsAreScaled => _saved;

        internal static void Save(GameMainBase instance, float blendTime, float duration)
        {
            _instance = instance;
            _blendTime = blendTime;
            _duration = duration;
            _saved = true;
        }

        internal static void Restore(GameMainBase instance)
        {
            if (!_saved || _instance != instance)
                return;

            try
            {
                var gameConfig = (GameConfig?)GameConfigProperty.GetValue(instance);
                if (gameConfig == null)
                    return;

                var playerActor = PlayerActorField.GetValue(gameConfig);
                if (playerActor != null)
                {
                    BlendTimeField.SetValue(playerActor, _blendTime);
                    DurationField.SetValue(playerActor, _duration);
                }
            }
            finally
            {
                _saved = false;
                _instance = null;
            }
        }
    }
}
