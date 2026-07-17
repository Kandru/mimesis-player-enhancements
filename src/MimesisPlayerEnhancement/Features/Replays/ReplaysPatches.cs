using ReluReplay;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplaysPatches
    {
        private const string Feature = "Replays";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ReplaysPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("IsReplayPlayMode/ReplayManager", AccessTools.PropertyGetter(typeof(ReplayManager), nameof(ReplayManager.IsReplayPlayMode))),
                ("OnTryDequeueMsg/ReplayManager", AccessTools.Method(typeof(ReplayManager), nameof(ReplayManager.OnTryDequeueMsg))),
                ("OnPlayerSpawn/ReplayManager", AccessTools.Method(typeof(ReplayManager), nameof(ReplayManager.OnPlayerSpawn))),
                ("RemapLevelObjectID/ReplayManager", AccessTools.Method(typeof(ReplayManager), nameof(ReplayManager.RemapLevelObjectID))),
                ("NextDungeonMasterID/ReplayManager", AccessTools.PropertyGetter(typeof(ReplayManager), nameof(ReplayManager.NextDungeonMasterID))),
                ("RandDungeonSeed/ReplayManager", AccessTools.PropertyGetter(typeof(ReplayManager), nameof(ReplayManager.RandDungeonSeed))),
                ("PickedMapID/ReplayManager", AccessTools.PropertyGetter(typeof(ReplayManager), nameof(ReplayManager.PickedMapID))),
                ("OnStopRecording/ReplayData", AccessTools.Method(typeof(ReluReplay.Data.ReplayData), "OnStopRecording")),
                ("TryCreateGameRoom/GamePlayScene", AccessTools.Method(typeof(GamePlayScene), "TryCreateGameRoom")),
                ("TryEnterGameRoom/GamePlayScene", AccessTools.Method(typeof(GamePlayScene), "TryEnterGameRoom")),
                ("SpawnMyAvatar/GameMainBase", AccessTools.Method(typeof(GameMainBase), "SpawnMyAvatar")),
                ("TryLevelLoad/GameMainBase", AccessTools.Method(typeof(GameMainBase), "TryLevelLoad")),
                ("CheckNetworkConnection/GameMainBase", AccessTools.Method(typeof(GameMainBase), "CheckNetworkConnection")),
                ("StartSceneLoading/GameMainBase", AccessTools.Method(typeof(GameMainBase), "StartSceneLoading")),
                ("EndSceneLoading/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.EndSceneLoading))),
                ("WaitForMinimumLoadingTime/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.WaitForMinimumLoadingTime))),
                ("Show/UIPrefabScript (replay UI block)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Show))),
                ("OnEnterDungeon/ReplayManager", AccessTools.Method(typeof(ReplayManager), nameof(ReplayManager.OnEnterDungeon))),
                ("Start/MainMenu", AccessTools.Method(typeof(MainMenu), "Start")),
                ("OnEnable/UIPrefab_MainMenu", AccessTools.Method(typeof(UIPrefab_MainMenu), "OnEnable")),
                ("Update/UIManager", AccessTools.Method(typeof(UIManager), "Update")),
            ]);
        }
    }
}
