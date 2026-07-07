using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Steamworks;
using UnityEngine;

// Patch logic derived from NeoMimicry/MorePlayers; networking helpers from MimicAPI are inlined in
// Util/GameNetworkApi.cs (see upstream: https://github.com/NeoMimicry/MorePlayers ,
// https://github.com/NeoMimicry/MimicAPI/tree/main/MimicAPI/GameAPI ).
namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    public static class MorePlayersPatches
    {
        private const string Feature = "MorePlayers";
        private const int VanillaMaxPlayers = 4;
        private static int _lastAppliedMaxClients = -1;

        private static readonly MethodInfo GetMaxPlayersMethod =
            AccessTools.Method(typeof(MorePlayersPatches), nameof(GetMaxPlayers));

        private static readonly MethodInfo GetLobbyPlayerCountSuffixMethod =
            AccessTools.Method(typeof(MorePlayersPatches), nameof(GetLobbyPlayerCountSuffix));

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNestedPatchTypes(typeof(MorePlayersPatches))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(SurvivalResultPatches)))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(InGameMenuPatches)))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(VActorDictCapacityPatches)));

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);

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
                ("PatchParameter/UIPrefab_SurvivalResult", AccessTools.Method(AccessTools.TypeByName("UIPrefab_SurvivalResult"), "PatchParameter")),
                ("SetRemoteVolumeController_v2/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))),
                ("SetPingImage/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetPingImage))),
                ("OnEnable/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnEnable")),
                ("ctor/IVroom", AccessTools.Constructor(typeof(IVroom), [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty), typeof(OnCreateRoomDelegate)])),
            ];

            foreach (MethodBase lambda in MaxPlayerCountFieldTranspiler.FindEnterRoomLambdaMethods())
            {
                checks.Add(($"{lambda.Name}/VRoomManager (closure)", lambda));
            }

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony, checks);
        }

        /// <summary>Re-applies player-cap limits to live networking state after config changes.</summary>
        public static void RefreshFromConfig()
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                // Revert an elevated live socket cap to vanilla so a disabled feature does not
                // keep accepting more than 4 players on the current host session.
                if (_lastAppliedMaxClients > VanillaMaxPlayers)
                {
                    ApplyMaxClientsToSocket(VanillaMaxPlayers);
                }

                _lastAppliedMaxClients = -1;
                return;
            }

            int maxPlayers = GetMaxPlayers();
            if (maxPlayers != _lastAppliedMaxClients
                && ApplyMaxClientsToSocket(maxPlayers))
            {
                _lastAppliedMaxClients = maxPlayers;
            }

            VActorDictCapacity.ApplyToAllRooms();
        }

        private static bool ApplyMaxClientsToSocket(int maxClients)
        {
            try
            {
                object? socket = GameNetworkApi.GetServerSocket();
                if (socket == null)
                {
                    return false;
                }

                GameNetworkApi.SetMaximumClients(socket, maxClients);
                ModLog.Info(Feature, $"Server socket max clients set to {maxClients}.");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Server socket refresh: {ex.Message}");
                return false;
            }
        }

        /// <summary>Called from transpiled game IL — must not bake config in at patch time.</summary>
        public static int GetMaxPlayers()
        {
            return ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : VanillaMaxPlayers;
        }

        /// <summary>Called from transpiled UI IL for room list player count labels (e.g. "3/32").</summary>
        public static string GetLobbyPlayerCountSuffix()
        {
            return "/" + GetMaxPlayers();
        }

        [HarmonyPatch]
        internal static class MaxPlayerCountFieldTranspiler
        {
            internal static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(IVroom), "CanEnterChannel");
                yield return AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.AddPlayerSteamID));

                // EnterWaitingRoom/EnterMaintenenceRoom run their player-cap check inside a
                // _commandExecutor.Invoke(delegate { ... }) closure, so the C_MaxPlayerCount load
                // lives in a compiler-generated lambda method — the outer methods contain nothing
                // to transpile. Patch the closure methods instead.
                foreach (MethodBase lambda in FindEnterRoomLambdaMethods())
                {
                    yield return lambda;
                }
            }

            internal static IEnumerable<MethodBase> FindEnterRoomLambdaMethods()
            {
                const BindingFlags allDeclared =
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                foreach (Type nestedType in typeof(VRoomManager).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (MethodInfo method in nestedType.GetMethods(allDeclared))
                    {
                        bool isEnterRoomLambda =
                            method.Name.Contains(nameof(VRoomManager.EnterWaitingRoom))
                            || method.Name.Contains(nameof(VRoomManager.EnterMaintenenceRoom));
                        if (isEnterRoomLambda && MethodReadsMaxPlayerCountField(method))
                        {
                            yield return method;
                        }
                    }
                }
            }

            private static bool MethodReadsMaxPlayerCountField(MethodBase method)
            {
                try
                {
                    foreach (KeyValuePair<OpCode, object> instruction in PatchProcessor.ReadMethodBody(method))
                    {
                        if (instruction.Value is FieldInfo field && field.Name == "C_MaxPlayerCount")
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to read IL of {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                }

                return false;
            }

            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return MaxPlayerCountIl.ReplaceConstMaxPlayerCount(instructions, GetMaxPlayersMethod);
            }
        }

        [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "GetMaximumClients")]
        internal static class GetMaximumClientsPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref int __result)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                __result = GetMaxPlayers();
                return false;
            }
        }

        [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "SetMaximumClients")]
        internal static class SetMaximumClientsPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref int value)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                value = GetMaxPlayers();
                return true;
            }
        }

        [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), MethodType.Constructor)]
        internal static class ServerSocketConstructorPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(object __instance)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return;
                }

                try
                {
                    GameNetworkApi.SetMaximumClients(__instance, GetMaxPlayers());
                    _lastAppliedMaxClients = GetMaxPlayers();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Server socket ctor postfix failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.SetRoomList))]
        internal static class PublicRoomListSetRoomListTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, GetMaxPlayersMethod);
            }
        }

        [HarmonyPatch(typeof(UiPrefab_RoomCard), "SetRoomData")]
        internal static class RoomCardSetRoomDataTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = [.. instructions];
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string literal && literal == "/4")
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, GetLobbyPlayerCountSuffixMethod);
                    }
                }

                return codes;
            }
        }

        [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdatePlayerGroupSize))]
        internal static class UpdatePlayerGroupSizeTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, GetMaxPlayersMethod);
            }
        }

        [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), typeof(bool), typeof(bool))]
        internal static class SteamLobbyCreationPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(bool isOpenForRandomMatch, bool isRetryAttempt)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                try
                {
                    SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, GetMaxPlayers());
                    if (!isRetryAttempt)
                    {
                        PlayerPrefs.SetInt("TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0);
                    }

                    ModLog.Info(Feature, $"Steam lobby created — maxPlayers={GetMaxPlayers()}, openForMatchmaking={isOpenForRandomMatch}, retry={isRetryAttempt}.");
                    return false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Steam lobby creation patch error: {ex.Message}");
                    return true;
                }
            }
        }
    }
}
