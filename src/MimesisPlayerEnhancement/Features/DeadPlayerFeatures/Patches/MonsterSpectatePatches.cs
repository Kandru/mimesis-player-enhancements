using System;
using System.Reflection;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.Patches
{
    internal static class MonsterSpectatePatchSupport
    {
        internal const string Feature = "DeadPlayerFeatures";

        internal const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static readonly MethodInfo? SetupSpectatorCameraMethod =
            AccessTools.Method(typeof(CameraManager), "SetupSpectatorCamera");

        internal static readonly MethodInfo? SwitchRenderablesMethod =
            AccessTools.Method(typeof(GamePlayScene), "SwitchToIndoorOrOutdoorRenderables");

        internal static readonly MethodInfo? NotifyAmbientSpectatorChangedMethod =
            AccessTools.Method(typeof(GameMainBase), "NotifyAmbient3DSoundSpectatorChanged");

        internal static readonly FieldInfo? SpectatorCameraField =
            AccessTools.Field(typeof(CameraManager), "spectatorCamera");

        internal static void ApplyDetectorAudioSettings(ProtoActor? myAvatar, bool isIndoor)
        {
            if (myAvatar == null)
            {
                return;
            }

            object? detector = myAvatar.GetIndoorOutdoorDetector();
            if (detector == null)
            {
                return;
            }

            PropertyInfo? isIndoorProperty = detector.GetType().GetProperty("IsIndoor", InstanceFlags);
            MethodInfo? applyAudioSettingsMethod =
                detector.GetType().GetMethod("ApplyAudioSettings", InstanceFlags, null, [typeof(bool)], null);

            if (isIndoorProperty?.GetValue(detector) is bool detectorIndoor
                && detectorIndoor != isIndoor)
            {
                applyAudioSettingsMethod?.Invoke(detector, [isIndoor]);
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), "GetAliveSpectator")]
    internal static class CameraManagerGetAliveSpectatorPostfix
    {
        [HarmonyPostfix]
        internal static void Postfix(List<ProtoActor> __result)
        {
            try
            {
                MonsterSpectateResolver.AppendMonsters(__result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(MonsterSpectatePatchSupport.Feature, $"GetAliveSpectator postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.ChangeSpectatorCameraTarget), [typeof(string)])]
    internal static class CameraManagerChangeSpectatorCameraTargetByNamePrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(string actorName, CameraManager __instance)
        {
            try
            {
                if (!MonsterSpectateResolver.IsEnabled
                    || !MonsterSpectateResolver.TryResolveTargetByName(actorName, out ProtoActor? target)
                    || target == null
                    || MonsterSpectatePatchSupport.SetupSpectatorCameraMethod == null)
                {
                    return true;
                }

                _ = MonsterSpectatePatchSupport.SetupSpectatorCameraMethod.Invoke(__instance, [target]);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(MonsterSpectatePatchSupport.Feature, $"ChangeSpectatorCameraTarget prefix failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), "SetupSpectatorCamera")]
    internal static class CameraManagerSetupSpectatorCameraPostfix
    {
        [HarmonyPostfix]
        internal static void Postfix(ProtoActor inActor, CameraManager __instance)
        {
            try
            {
                if (!MonsterSpectateResolver.IsEnabled
                    || inActor == null
                    || MonsterSpectatePatchSupport.SpectatorCameraField?.GetValue(__instance) is not Component spectatorCamera)
                {
                    return;
                }

                PropertyInfo? followProperty = spectatorCamera.GetType().GetProperty("Follow", MonsterSpectatePatchSupport.InstanceFlags);
                if (followProperty?.GetValue(spectatorCamera) is Transform existingFollow && existingFollow != null)
                {
                    return;
                }

                Transform fallback = inActor.transform;
                PropertyInfo? lookAtProperty = spectatorCamera.GetType().GetProperty("LookAt", MonsterSpectatePatchSupport.InstanceFlags);
                followProperty?.SetValue(spectatorCamera, fallback);
                lookAtProperty?.SetValue(spectatorCamera, fallback);
            }
            catch (Exception ex)
            {
                ModLog.Warn(MonsterSpectatePatchSupport.Feature, $"SetupSpectatorCamera postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GamePlayScene), "OnActorTeleported_RenderableCulling")]
    internal static class GamePlaySceneOnActorTeleportedRenderableCullingPrefix
    {
        [HarmonyPrefix]
        internal static bool Prefix(ProtoActor actor, GamePlayScene __instance)
        {
            try
            {
                if (!MonsterSpectateResolver.ShouldHandleMonsterTeleportCulling(actor))
                {
                    return true;
                }

                CameraManager? cameraman = DeadPlayerPhoneGameAccess.TryGetCameraManager();
                if (cameraman == null
                    || !cameraman.TryGetCurrentSpectatorTarget(out ProtoActor? target)
                    || target == null)
                {
                    return true;
                }

                bool isIndoor = __instance.CheckActorIsIndoor(target);
                MonsterSpectatePatchSupport.SwitchRenderablesMethod?.Invoke(__instance, [isIndoor]);
                DeadPlayerPhoneGameAccess.TryGetVoiceManager()?.SetTransmitterChannelRecv(isIndoor);
                MonsterSpectatePatchSupport.ApplyDetectorAudioSettings(__instance.GetMyAvatar(), isIndoor);
                MonsterSpectatePatchSupport.NotifyAmbientSpectatorChangedMethod?.Invoke(__instance, null);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(MonsterSpectatePatchSupport.Feature, $"OnActorTeleported_RenderableCulling prefix failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "UpdateSpectatorHUD", [typeof(ProtoActor), typeof(CameraManager.CameraMode)])]
    internal static class GameMainBaseUpdateSpectatorHudPostfix
    {
        [HarmonyPostfix]
        internal static void Postfix(ProtoActor actor, GameMainBase __instance)
        {
            try
            {
                if (!MonsterSpectateResolver.IsEnabled
                    || actor == null
                    || actor.ActorType != ActorType.Monster
                    || __instance.spectatorui == null)
                {
                    return;
                }

                string displayName = MonsterSpectateResolver.GetMonsterDisplayName(actor);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    __instance.spectatorui.SetSpectatedPlayerName(displayName);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(MonsterSpectatePatchSupport.Feature, $"UpdateSpectatorHUD postfix failed — {ex.Message}");
            }
        }
    }
}
