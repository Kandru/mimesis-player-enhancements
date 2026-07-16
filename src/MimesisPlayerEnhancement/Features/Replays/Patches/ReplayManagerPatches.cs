using ReluReplay;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.IsReplayPlayMode), MethodType.Getter)]
    internal static class ReplayManagerIsReplayPlayModePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (ReplaySharedData.IsReplayPlayMode)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.OnTryDequeueMsg))]
    internal static class ReplayManagerOnTryDequeueMsgPatch
    {
        private const string Feature = "Replays";

        [HarmonyPrefix]
        private static bool Prefix(ref bool __result, out IMsg msg)
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                msg = null!;
                return true;
            }

            try
            {
                if (ReplayPlaybackEngine.TryDequeueMessage(out IMsg? dequeued) && dequeued != null)
                {
                    msg = dequeued;
                    __result = true;
                    return false;
                }

                msg = null!;
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnTryDequeueMsg patch failed — {ex.Message}");
                msg = null!;
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.OnGamePlaySceneLoadedComplete))]
    internal static class ReplayManagerOnGamePlaySceneLoadedCompletePatch
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
                ReplayPlaybackEngine.ScheduleScenePresentationReady();
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnGamePlaySceneLoadedComplete patch failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.OnEnterDungeon))]
    internal static class ReplayManagerOnEnterDungeonPatch
    {
        private const string Feature = "Replays";

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!ReplaySharedData.IsReplayPlayMode || !ReplayPlaybackEngine.IsActive)
            {
                return;
            }

            try
            {
                ReplayPlaybackEngine.ScheduleScenePresentationReady();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnEnterDungeon patch failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.OnPlayerSpawn))]
    internal static class ReplayManagerOnPlayerSpawnPatch
    {
        private const string Feature = "Replays";

        [HarmonyPrefix]
        private static bool Prefix(ProtoActor playerActor)
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return true;
            }

            try
            {
                ReplayPlaybackEngine.OnPlayerSpawned(playerActor);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnPlayerSpawn patch failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.RemapLevelObjectID))]
    internal static class ReplayManagerRemapLevelObjectIdPatch
    {
        private const string Feature = "Replays";

        [HarmonyPrefix]
        private static bool Prefix(int recordedID, ref int __result)
        {
            if (!ReplaySharedData.IsReplayPlayMode)
            {
                return true;
            }

            try
            {
                __result = ReplayPlaybackEngine.Remapper.Remap(recordedID);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"RemapLevelObjectID patch failed — {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.NextDungeonMasterID), MethodType.Getter)]
    internal static class ReplayManagerNextDungeonMasterIdPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref int __result)
        {
            if (ReplaySharedData.IsReplayPlayMode)
            {
                __result = ReplayPlaybackEngine.GetReplayDungeonMasterId();
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.RandDungeonSeed), MethodType.Getter)]
    internal static class ReplayManagerRandDungeonSeedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref int __result)
        {
            if (ReplaySharedData.IsReplayPlayMode)
            {
                __result = ReplayPlaybackEngine.GetReplayRandSeed();
            }
        }
    }

    [HarmonyPatch(typeof(ReplayManager), nameof(ReplayManager.PickedMapID), MethodType.Getter)]
    internal static class ReplayManagerPickedMapIdPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref int __result)
        {
            if (ReplaySharedData.IsReplayPlayMode)
            {
                __result = ReplayPlaybackEngine.GetReplayPickedMapId();
            }
        }
    }

}
