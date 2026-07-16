namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsPatches
    {
        private const string Feature = "Statistics";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(StatisticsPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("FlushCurrentToAccumulated/PlayReportManager", AccessTools.Method(typeof(PlayReportManager), nameof(PlayReportManager.FlushCurrentToAccumulated))),
                ("IncreaseStageCount/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.IncreaseStageCount))),
                ("Reset/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.Reset))),
                ("TerminateSession/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.TerminateSession))),
                ("HandlePutIntoToilet/VPlayer", AccessTools.Method(typeof(VPlayer), nameof(VPlayer.HandlePutIntoToilet))),
                ("OnAddItemByLooting/InventoryController", AccessTools.Method(typeof(InventoryController), "OnAddItemByLooting", [typeof(ItemElement)])),
                ("OnActorEvent/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), nameof(DungeonRoom.OnActorEvent))),
                ("OnActorEvent/DeathMatchRoom", AccessTools.Method(typeof(DeathMatchRoom), nameof(DeathMatchRoom.OnActorEvent))),
                ("OnActorEvent/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnActorEvent))),
                ("OnDying/VCreature", AccessTools.Method(typeof(VCreature), nameof(VCreature.OnDying))),
                ("SetDeathMatchSurvivor/PlayReportManager", AccessTools.Method(typeof(PlayReportManager), nameof(PlayReportManager.SetDeathMatchSurvivor))),
                ("SetDungeonState/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "SetDungeonState")),
                ("Revive/VPlayer", AccessTools.Method(typeof(VPlayer), nameof(VPlayer.Revive))),
                ("OnRegistPlayer/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))),
                ("OnUnregistPlayer/VWorld", AccessTools.Method(typeof(VWorld), nameof(VWorld.OnUnregistPlayer))),
                ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(Mimic.Voice.SpeechSystem.SpeechEventArchive), "OnStartClient")),
                ("AddPlayerInfo/UIPrefab_PlayerEnterInfo", AccessTools.Method(typeof(UIPrefab_PlayerEnterInfo), nameof(UIPrefab_PlayerEnterInfo.AddPlayerInfo))),
                ("UpdatePlayerInfos/UIPrefab_PlayerEnterInfo", AccessTools.Method(typeof(UIPrefab_PlayerEnterInfo), "UpdatePlayerInfos")),
            ]);
        }
    }
}
