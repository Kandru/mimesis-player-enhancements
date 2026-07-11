using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPatches
    {
        private const string Feature = "WebDashboard";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WebDashboardPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnPacket/GameMainBase (NetworkGradeSig)", AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(NetworkGradeSig)])),
                ("GetSteamAvatar/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.GetSteamAvatar))),
                ("SetRemoteVolumeController_v2/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))),
                ("OnPlayerDeath/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))),
                ("OnPlayerRevive/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.OnPlayerRevive))),
                ("SetLobbyName/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyName))),
                ("LeaveLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.LeaveLobby))),
                ("ChangeStatus/DungeonGenerator", AccessTools.Method(typeof(DungeonGenerator), "ChangeStatus")),
                ("BuildDungeonInfo/RuntimeDungeon", AccessTools.Method(typeof(RuntimeDungeon), "BuildDungeonInfo")),
                ("InitSpawn/DungeonRoom (minimap)", AccessTools.Method(typeof(DungeonRoom), "InitSpawn")),
                ("ForcedDying/VCreature", AccessTools.Method(typeof(VCreature), nameof(VCreature.ForcedDying))),
                ("CheckFallDamage/MovementController", AccessTools.Method(typeof(MovementController), nameof(MovementController.CheckFallDamage))),
                ("ValidPosition/IVroom", AccessTools.Method(typeof(IVroom), nameof(IVroom.ValidPosition))),
                ("HandleGlobalPacket/NetworkManagerV2", AccessTools.Method(typeof(NetworkManagerV2), "HandleGlobalPacket")),
                ("UpdateControl/ProtoActor", AccessTools.Method(typeof(ProtoActor), "UpdateControl")),
            ]);
        }
    }
}
