using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class MorePlayersPatches
    {
        private const string Feature = "MorePlayers";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MorePlayersPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            List<(string, MethodBase?)> checks =
            [
                ("CanEnterChannel/IVroom", AccessTools.Method(typeof(IVroom), "CanEnterChannel")),
                ("AddPlayerSteamID/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.AddPlayerSteamID))),
                ("GetMaximumClients/ServerSocket", AccessTools.Method(typeof(FishySteamworks.Server.ServerSocket), "GetMaximumClients")),
                ("SetMaximumClients/ServerSocket", AccessTools.Method(typeof(FishySteamworks.Server.ServerSocket), "SetMaximumClients")),
                ("ServerSocket.ctor", AccessTools.Constructor(typeof(FishySteamworks.Server.ServerSocket))),
                ("CreateLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), [typeof(bool), typeof(bool)])),
                ("UpdatePlayerGroupSize/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdatePlayerGroupSize))),
                ("SetRoomList/UIPrefab_PublicRoomList", AccessTools.Method(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.SetRoomList))),
                ("SetRoomData/UiPrefab_RoomCard", AccessTools.Method(typeof(UiPrefab_RoomCard), "SetRoomData")),
                ("SetRemoteVolumeController_v2/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))),
                ("SetPingImage/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetPingImage))),
                ("OnEnable/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnEnable")),
                ("ctor/IVroom", AccessTools.Constructor(typeof(IVroom), [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty), typeof(OnCreateRoomDelegate)])),
                ("RefreshTargetCurrency/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.RefreshTargetCurrency))),
                ("ClampTargetCurrencyToMin/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), "ClampTargetCurrencyToMin")),
            ];

            foreach (MethodBase lambda in MorePlayersPatchHelpers.FindEnterRoomLambdaMethods())
            {
                checks.Add(($"{lambda.Name}/VRoomManager (closure)", lambda));
            }

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony, checks);
        }

        /// <summary>Re-applies player-cap limits to live networking state after config changes.</summary>
        public static void RefreshFromConfig()
        {
            InGameMenuExtendedSlots.SyncFromConfig();

            if (!ModConfig.EnableMorePlayers.Value)
            {
                if (MorePlayersPatchHelpers._lastAppliedMaxClients > MorePlayersPatchHelpers.VanillaMaxPlayers)
                {
                    MorePlayersPatchHelpers.ApplyMaxClientsToSocket(MorePlayersPatchHelpers.VanillaMaxPlayers);
                }

                MorePlayersPatchHelpers._lastAppliedMaxClients = -1;
                return;
            }

            int maxPlayers = MorePlayersPatchHelpers.GetMaxPlayers();
            if (maxPlayers != MorePlayersPatchHelpers._lastAppliedMaxClients
                && MorePlayersPatchHelpers.ApplyMaxClientsToSocket(maxPlayers))
            {
                MorePlayersPatchHelpers._lastAppliedMaxClients = maxPlayers;
            }

            VActorDictCapacity.ApplyToAllRooms();
        }

        internal static void OnSessionEnded()
        {
            MorePlayersPatchHelpers.ResetSessionState();
            InGameMenuDebugPreview.OnSessionEnded();
        }
    }
}
