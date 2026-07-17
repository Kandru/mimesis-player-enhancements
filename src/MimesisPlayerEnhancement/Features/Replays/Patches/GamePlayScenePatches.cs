using System.Collections;
using ReluProtocol.Enum;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(GamePlayScene), "TryCreateGameRoom")]
    internal static class GamePlaySceneTryCreateGameRoomPatch
    {
        private const string Feature = "Replays";

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return true;
            }

            try
            {
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                if (pdata == null)
                {
                    return false;
                }

                pdata.completeMakingRoomSig = new MakeRoomCompleteSig
                {
                    nextRoomInfo =
                    {
                        roomType = VRoomType.Game,
                        roomUID = 1L,
                    },
                };
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"TryCreateGameRoom patch failed — {ex.Message}");
                return true;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(GamePlayScene __instance)
        {
            if (!ReplaySharedData.IsReplayPlayMode || !ReplayPlaybackEngine.IsActive)
            {
                return;
            }

            try
            {
                __instance.EndSceneLoading();
                ReplayPlaybackEngine.NotifyGameplaySceneReady();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"TryCreateGameRoom replay bootstrap failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GamePlayScene), "TryEnterGameRoom")]
    internal static class GamePlaySceneTryEnterGameRoomPatch
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
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                if (pdata != null)
                {
                    pdata.lastResponseError = MsgErrorCode.Success;
                }

                __result = EmptyCoroutine();
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"TryEnterGameRoom patch failed — {ex.Message}");
                return true;
            }
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break;
        }
    }
}
