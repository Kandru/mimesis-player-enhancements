using System.Collections;
using System.Reflection;
using MimesisPlayerEnhancement.Ui;
using ReluProtocol.Enum;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(GameMainBase), "CheckNetworkConnection")]
    internal static class GameMainBaseCheckNetworkConnectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (ReplaySharedData.IsReplayPlayMode)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "SpawnMyAvatar")]
    internal static class GameMainBaseSpawnMyAvatarPatch
    {
        private const string Feature = "Replays";

        [HarmonyPrefix]
        private static bool Prefix(ref IEnumerator __result)
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return true;
            }

            try
            {
                __result = EmptyCoroutine();
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SpawnMyAvatar patch failed — {ex.Message}");
                return true;
            }
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break;
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "TryLevelLoad")]
    internal static class GameMainBaseTryLevelLoadPatch
    {
        private const string Feature = "Replays";

        private static readonly FieldInfo? EnteringCompleteAllField =
            AccessTools.Field(typeof(GameMainBase), "EnteringCompleteAll");

        [HarmonyPrefix]
        private static bool Prefix(GameMainBase __instance, ref IEnumerator __result)
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return true;
            }

            try
            {
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                if (pdata != null)
                {
                    pdata.lastResponseError = MsgErrorCode.Success;
                }

                EnteringCompleteAllField?.SetValue(__instance, true);
                __result = EmptyCoroutine();
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"TryLevelLoad patch failed — {ex.Message}");
                return true;
            }
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break;
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "StartSceneLoading")]
    internal static class GameMainBaseStartSceneLoadingPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return;
            }

            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager?.ui_sceneloading != null)
            {
                uiManager.ui_sceneloading.Hide();
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))]
    internal static class GameMainBaseEndSceneLoadingPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return;
            }

            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager?.ui_sceneloading != null)
            {
                uiManager.ui_sceneloading.Hide();
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.WaitForMinimumLoadingTime))]
    internal static class GameMainBaseWaitForMinimumLoadingTimePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref IEnumerator __result)
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return true;
            }

            __result = EmptyCoroutine();
            return false;
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break;
        }
    }
}
