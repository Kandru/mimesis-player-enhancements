using System.Reflection;
using System.Reflection.Emit;
using Steamworks;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    [HarmonyPatch]
    internal static class MaxPlayerCountFieldTranspiler
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(IVroom), "CanEnterChannel");
            yield return AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.AddPlayerSteamID));

            foreach (MethodBase lambda in MorePlayersPatchHelpers.FindEnterRoomLambdaMethods())
            {
                yield return lambda;
            }
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return MaxPlayerCountIl.ReplaceConstMaxPlayerCount(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
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

            __result = MorePlayersPatchHelpers.GetMaxPlayers();
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

            value = MorePlayersPatchHelpers.GetMaxPlayers();
            return true;
        }
    }

    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), MethodType.Constructor)]
    internal static class ServerSocketConstructorPatch
    {
        private const string Feature = "MorePlayers";

        [HarmonyPostfix]
        internal static void Postfix(object __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            try
            {
                GameNetworkApi.SetMaximumClients(__instance, MorePlayersPatchHelpers.GetMaxPlayers());
                MorePlayersPatchHelpers._lastAppliedMaxClients = MorePlayersPatchHelpers.GetMaxPlayers();
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
            return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
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
                    codes[i] = new CodeInstruction(OpCodes.Call, MorePlayersPatchHelpers.GetLobbyPlayerCountSuffixMethod);
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
            return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), typeof(bool), typeof(bool))]
    internal static class SteamLobbyCreationPatch
    {
        private const string Feature = "MorePlayers";

        [HarmonyPrefix]
        internal static bool Prefix(bool isOpenForRandomMatch, bool isRetryAttempt)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return true;
            }

            try
            {
                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MorePlayersPatchHelpers.GetMaxPlayers());
                if (!isRetryAttempt)
                {
                    PlayerPrefs.SetInt("TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0);
                }

                ModLog.Info(Feature, $"Steam lobby created — maxPlayers={MorePlayersPatchHelpers.GetMaxPlayers()}, openForMatchmaking={isOpenForRandomMatch}, retry={isRetryAttempt}.");
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
