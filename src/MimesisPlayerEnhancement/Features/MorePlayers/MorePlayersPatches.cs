using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

// Patch logic derived from NeoMimicry/MorePlayers; networking helpers from MimicAPI are inlined in
// Util/GameNetworkApi.cs (see upstream: https://github.com/NeoMimicry/MorePlayers ,
// https://github.com/NeoMimicry/MimicAPI/tree/main/MimicAPI/GameAPI ).
namespace MimesisPlayerEnhancement.Features.MorePlayers;

public static class MorePlayersPatches
{
    private const string Feature = "MorePlayers";
    private const int VanillaMaxPlayers = 4;

    private const BindingFlags InstanceMethodFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly MethodInfo GetMaxPlayersMethod =
        AccessTools.Method(typeof(MorePlayersPatches), nameof(GetMaxPlayers))!;

    private static readonly MethodInfo GetLobbyPlayerCountSuffixMethod =
        AccessTools.Method(typeof(MorePlayersPatches), nameof(GetLobbyPlayerCountSuffix))!;

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        _ = GameNetworkApi.GetGameAssembly();

        var patchTypes = typeof(MorePlayersPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

        foreach (var patchType in patchTypes)
        {
            try
            {
                var patchedMethods = harmony.CreateClassProcessor(patchType).Patch();
                if (patchedMethods == null || patchedMethods.Count == 0)
                    ModLog.Warn(Feature, $"Patch class {patchType.Name} did not apply to any methods.");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Patch class {patchType.Name} failed: {ex.Message}");
            }
        }

        LogPatchAudit(harmony);
        ModLog.Info(Feature, "Patches applied.");
    }

    private static bool IsPatched(IReadOnlyCollection<MethodBase> patched, MethodBase? expected)
    {
        if (expected == null)
            return false;

        foreach (var candidate in patched)
        {
            if (candidate.MetadataToken == expected.MetadataToken && ReferenceEquals(candidate.Module, expected.Module))
                return true;

            if (candidate.Name != expected.Name)
                continue;

            if (candidate.DeclaringType != expected.DeclaringType)
                continue;

            var candidateParams = candidate.GetParameters();
            var expectedParams = expected.GetParameters();
            if (candidateParams.Length != expectedParams.Length)
                continue;

            bool paramsMatch = true;
            for (int i = 0; i < candidateParams.Length; i++)
            {
                if (candidateParams[i].ParameterType != expectedParams[i].ParameterType)
                {
                    paramsMatch = false;
                    break;
                }
            }

            if (paramsMatch)
                return true;
        }

        return false;
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        var patched = harmony.GetPatchedMethods().ToList();
        var applied = new List<string>();
        var missing = new List<string>();

        void Check(string label, MethodBase? method)
        {
            if (method == null)
                missing.Add($"{label} (type/method not found)");
            else if (IsPatched(patched, method))
                applied.Add(label);
            else
                missing.Add(label);
        }

        Check("CanEnterChannel/IVroom", ResolveRoomMethod("IVroom", "CanEnterChannel"));
        Check("CanEnterChannel/VWaitingRoom", ResolveRoomMethod("VWaitingRoom", "CanEnterChannel"));
        Check("CanEnterChannel/MaintenanceRoom", ResolveRoomMethod("MaintenanceRoom", "CanEnterChannel"));
        Check("GetMemberCount/IVroom", GameNetworkApi.GetIVroomType()?.GetMethod("GetMemberCount", InstanceMethodFlags));

        foreach (var method in FindSocketMethods("GetMaximumClients"))
            Check("GetMaximumClients/ServerSocket", method);

        foreach (var method in FindSocketMethods("SetMaximumClients"))
            Check("SetMaximumClients/ServerSocket", method);

        Check("ServerSocket.ctor", ResolveServerSocketConstructor());
        Check("AddPlayerSteamID/GameSessionInfo", ResolveGameSessionInfoMethod("AddPlayerSteamID"));
        Check("CreateLobby/SteamInviteDispatcher", ResolveSteamInviteDispatcherMethod("CreateLobby"));
        Check("UpdatePlayerGroupSize/SteamInviteDispatcher", ResolveSteamInviteDispatcherMethod("UpdatePlayerGroupSize"));
        Check("SetRoomList/UIPrefab_PublicRoomList", ResolveGameTypeMethod("UIPrefab_PublicRoomList", "SetRoomList"));
        Check("SetRoomData/UiPrefab_RoomCard", ResolveGameTypeMethod("UiPrefab_RoomCard", "SetRoomData"));
        Check("EnterWaitingRoom/VRoomManager", ResolveVRoomManagerMethod("EnterWaitingRoom"));
        Check("EnterMaintenenceRoom/VRoomManager", ResolveVRoomManagerMethod("EnterMaintenenceRoom"));

        if (applied.Count > 0)
            ModLog.Debug(Feature, $"Patch audit — applied: {string.Join(", ", applied)}");

        foreach (string label in missing)
            ModLog.Warn(Feature, $"Patch audit — not applied: {label}");
    }

    /// <summary>Re-applies player-cap limits to live networking state after config changes.</summary>
    public static void RefreshFromConfig()
    {
        int cap = ModConfig.EnableMorePlayers.Value ? MaxClientConnections : 4;

        try
        {
            var socket = GameNetworkApi.GetServerSocket();
            if (socket != null)
            {
                GameNetworkApi.SetMaximumClients(socket, cap);
                ModLog.Debug(
                    Feature,
                    $"Server socket max clients refreshed to {cap} (session cap {(ModConfig.EnableMorePlayers.Value ? MaxPlayers : 4)}).");
            }
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"Server socket refresh: {ex.Message}");
        }

        try
        {
            UpdateRoomMaxPlayers(GameNetworkApi.GetVRoomManager(), logRefresh: true);
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"Room max players refresh: {ex.Message}");
        }
    }

    private static void UpdateRoomMaxPlayers(object? vroomManager, bool logRefresh = false)
    {
        if (vroomManager == null)
            return;

        var vrooms = ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") as IDictionary;
        if (vrooms == null)
            return;

        foreach (var room in vrooms.Values)
        {
            if (room == null)
                continue;

            var roomType = room.GetType();
            while (roomType != null)
            {
                var maxPlayersField = roomType.GetField("_maxPlayers", BindingFlags.NonPublic | BindingFlags.Instance);
                if (maxPlayersField != null)
                {
                    int cap = ModConfig.EnableMorePlayers.Value ? MaxPlayers : VanillaMaxPlayers;
                    maxPlayersField.SetValue(room, cap);
                    if (logRefresh)
                        ModLog.Debug(Feature, $"Room {room.GetType().Name} max players refreshed to {cap}.");
                    break;
                }

                roomType = roomType.BaseType;
            }
        }
    }

    /// <summary>Called from transpiled game IL — must not bake config in at patch time.</summary>
    public static int GetMaxPlayers() =>
        ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : VanillaMaxPlayers;

    /// <summary>Called from transpiled UI IL for room list player count labels (e.g. "3/32").</summary>
    public static string GetLobbyPlayerCountSuffix() => "/" + GetMaxPlayers();

    internal static int MaxPlayers => GetMaxPlayers();

    internal static int MaxClientConnections => MaxPlayers;

    private static int PlayersInRoom(IDictionary? vPlayerDict) =>
        vPlayerDict?.Count ?? 0;

    private static CodeInstruction LoadMaxPlayers() =>
        new(OpCodes.Call, GetMaxPlayersMethod);

    private static IEnumerable<CodeInstruction> ReplaceVanillaLobbyCap(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldc_I4_4)
                codes[i] = LoadMaxPlayers();
        }

        return codes;
    }

    private static IEnumerable<CodeInstruction> ReplaceLobbyPlayerCountSuffix(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string literal && literal == "/4")
                codes[i] = new CodeInstruction(OpCodes.Call, GetLobbyPlayerCountSuffixMethod);
        }

        return codes;
    }

    private static MethodBase? ResolveGameTypeMethod(string typeName, string methodName) =>
        GameNetworkApi.GetGameAssembly()?.GetType(typeName)?.GetMethod(methodName, InstanceMethodFlags);

    private static MethodBase? ResolveRoomMethod(string typeName, string methodName) =>
        GameNetworkApi.GetGameAssembly()?.GetType(typeName)?.GetMethod(methodName, InstanceMethodFlags);

    private static MethodBase? ResolveGameSessionInfoMethod(string methodName) =>
        GameNetworkApi.GetGameSessionInfoType()?.GetMethod(methodName, InstanceMethodFlags);

    private static MethodBase? ResolveVRoomManagerMethod(string methodName) =>
        GameNetworkApi.GetGameAssembly()?.GetType("VRoomManager")?.GetMethod(methodName, InstanceMethodFlags);

    private static MethodBase? ResolveSteamInviteDispatcherMethod(string methodName) =>
        GameNetworkApi.GetGameAssembly()?.GetTypes().FirstOrDefault(t => t.Name == "SteamInviteDispatcher")
            ?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static MethodBase? ResolveServerSocketConstructor()
    {
        var assembly = GameNetworkApi.GetGameAssembly();
        foreach (var typeName in new[] { "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" })
        {
            var type = assembly?.GetType(typeName);
            var ctor = type?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
            if (ctor != null)
                return ctor;
        }

        return null;
    }

    private static IEnumerable<MethodBase> FindSocketMethods(string methodName)
    {
        var assembly = GameNetworkApi.GetGameAssembly();
        if (assembly == null)
            yield break;

        foreach (var typeName in new[] { "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" })
        {
            var type = assembly.GetType(typeName);
            var method = type?.GetMethod(methodName, InstanceMethodFlags);
            if (method != null)
                yield return method;
        }
    }

    [HarmonyPatch]
    public static class IVroomCanEnterChannelTranspiler
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var method = GameNetworkApi.GetIVroomType()?.GetMethod("CanEnterChannel", InstanceMethodFlags);
            return method != null ? new[] { method } : Array.Empty<MethodBase>();
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld
                    && codes[i].operand is FieldInfo field
                    && field.Name == "C_MaxPlayerCount")
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes.Insert(i + 1, LoadMaxPlayers());
                    break;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch]
    public static class GetMaximumClientsPatch
    {
        public static IEnumerable<MethodBase> TargetMethods() => FindSocketMethods("GetMaximumClients");

        [HarmonyPrefix]
        public static bool Prefix(ref int __result)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            __result = MaxClientConnections;
            return false;
        }
    }

    [HarmonyPatch]
    public static class SetMaximumClientsPatch
    {
        public static IEnumerable<MethodBase> TargetMethods() => FindSocketMethods("SetMaximumClients");

        [HarmonyPrefix]
        public static bool Prefix(ref int value)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            value = MaxClientConnections;
            return true;
        }
    }

    [HarmonyPatch]
    public static class ServerSocketConstructorPatch
    {
        public static MethodBase? TargetMethod() => ResolveServerSocketConstructor();

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return;

            try
            {
                GameNetworkApi.SetMaximumClients(__instance, MaxClientConnections);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Server socket ctor postfix failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class GetMemberCountSmartPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var method = GameNetworkApi.GetIVroomType()?.GetMethod("GetMemberCount", InstanceMethodFlags);
            return method != null ? new[] { method } : Array.Empty<MethodBase>();
        }

        [HarmonyPrefix]
        public static bool Prefix(ref int __result, object __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            try
            {
                int actualCount = GameNetworkApi.GetRoomPlayerCount(__instance);

                var stackTrace = new System.Diagnostics.StackTrace();
                bool isFromEnterCheck = false;
                bool isFromSessionCount = false;

                for (int i = 0; i < Math.Min(stackTrace.FrameCount, 10); i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null)
                        continue;

                    string methodName = method.Name;
                    if (methodName.Contains("EnterWaitingRoom")
                        || methodName.Contains("EnterMaintenenceRoom")
                        || methodName.Contains("EnterMaintenanceRoom")
                        || methodName.Contains("CanEnter"))
                    {
                        isFromEnterCheck = true;
                        break;
                    }

                    if (methodName.Contains("GetSessionCount") || methodName.Contains("GetRoomMemberCount"))
                    {
                        isFromSessionCount = true;
                        break;
                    }
                }

                if (isFromEnterCheck)
                {
                    __result = 0;
                    ModLog.Debug(
                        Feature,
                        $"GetMemberCount returning 0 for enter check in {__instance.GetType().Name}");
                    return false;
                }

                if (isFromSessionCount)
                {
                    __result = actualCount;
                    return false;
                }

                __result = actualCount;
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetMemberCount patch error: {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch]
    public static class AllRoomsCanEnterChannelPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = GameNetworkApi.GetGameAssembly();
            if (assembly == null)
                return Array.Empty<MethodBase>();

            var methods = new List<MethodBase>();
            foreach (var typeName in new[] { "VWaitingRoom", "MaintenanceRoom", "IVroom" })
            {
                var method = assembly.GetType(typeName)?.GetMethod("CanEnterChannel", InstanceMethodFlags);
                if (method != null && !methods.Contains(method))
                    methods.Add(method);
            }

            return methods;
        }

        [HarmonyPrefix]
        public static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            try
            {
                var msgErrorCodeType = GameNetworkApi.GetGameAssembly()?.GetTypes().FirstOrDefault(t => t.Name == "MsgErrorCode");
                if (msgErrorCodeType == null || !msgErrorCodeType.IsEnum)
                    return true;

                var vPlayerDict = ReflectionHelper.GetFieldValue(__instance, "_vPlayerDict") as IDictionary;
                if (vPlayerDict != null)
                {
                    foreach (var player in vPlayerDict.Values)
                    {
                        if (player == null)
                            continue;

                        var uid = ReflectionHelper.GetFieldValue<long>(player, "UID");
                        if (uid == 0)
                        {
                            var uidProp = player.GetType().GetProperty("UID", BindingFlags.Public | BindingFlags.Instance);
                            if (uidProp != null)
                                uid = Convert.ToInt64(uidProp.GetValue(player));
                        }

                        if (uid == playerUID)
                        {
                            __result = Enum.Parse(msgErrorCodeType, "DuplicatePlayer");
                            ModLog.Info(
                                Feature,
                                $"Join denied — duplicate player uid={playerUID} in {__instance.GetType().Name}.");
                            return false;
                        }
                    }

                    if (PlayersInRoom(vPlayerDict) >= MaxPlayers)
                    {
                        __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                        ModLog.Info(
                            Feature,
                            $"Join denied — session full ({PlayersInRoom(vPlayerDict)}/{MaxPlayers} players) in {__instance.GetType().Name}, uid={playerUID}.");
                        return false;
                    }
                }
                else
                {
                    ModLog.Warn(
                        Feature,
                        $"Join denied — player dictionary unavailable in {__instance.GetType().Name}, uid={playerUID}.");
                    __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                    return false;
                }

                __result = Enum.Parse(msgErrorCodeType, "Success");
                int totalAfterJoin = PlayersInRoom(vPlayerDict) + 1;
                ModLog.Debug(
                    Feature,
                    $"Join allowed — uid={playerUID} in {__instance.GetType().Name} ({totalAfterJoin}/{MaxPlayers} players).");
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"CanEnterChannel patch error: {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch]
    public static class VRoomManagerEnterRoomPatches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var vroomManagerType = GameNetworkApi.GetGameAssembly()?.GetType("VRoomManager");
            if (vroomManagerType == null)
                return Array.Empty<MethodBase>();

            var methods = new List<MethodBase>();
            foreach (var name in new[] { "EnterWaitingRoom", "EnterMaintenenceRoom", "EnterMaintenanceRoom" })
            {
                var method = vroomManagerType.GetMethod(name, InstanceMethodFlags);
                if (method != null)
                    methods.Add(method);
            }

            return methods;
        }

        [HarmonyPrefix]
        public static void Prefix(object __instance, MethodBase __originalMethod)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return;

            try
            {
                UpdateRoomMaxPlayers(__instance);
                string roomName = __originalMethod.Name switch
                {
                    "EnterWaitingRoom" => "WaitingRoom",
                    "EnterMaintenenceRoom" or "EnterMaintenanceRoom" => "MaintenanceRoom",
                    _ => __originalMethod.Name
                };
                ModLog.Info(
                    Feature,
                    $"Enter room — {roomName} (session cap {MaxPlayers}).");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"VRoomManager enter room patch error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class GameSessionInfoAddPlayerSteamIdPatch
    {
        public static MethodBase? TargetMethod() => ResolveGameSessionInfoMethod("AddPlayerSteamID");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = LoadMaxPlayers();
                }
                else if (codes[i].opcode == OpCodes.Ldfld
                         && codes[i].operand is FieldInfo field
                         && field.Name == "C_MaxPlayerCount")
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes.Insert(i + 1, LoadMaxPlayers());
                    i++;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch]
    public static class PublicRoomListSetRoomListTranspiler
    {
        public static MethodBase? TargetMethod() => ResolveGameTypeMethod("UIPrefab_PublicRoomList", "SetRoomList");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceVanillaLobbyCap(instructions);
    }

    [HarmonyPatch]
    public static class RoomCardSetRoomDataTranspiler
    {
        public static MethodBase? TargetMethod() => ResolveGameTypeMethod("UiPrefab_RoomCard", "SetRoomData");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceLobbyPlayerCountSuffix(instructions);
    }

    [HarmonyPatch]
    public static class UpdatePlayerGroupSizeTranspiler
    {
        public static MethodBase? TargetMethod() => ResolveSteamInviteDispatcherMethod("UpdatePlayerGroupSize");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceVanillaLobbyCap(instructions);
    }

    [HarmonyPatch]
    public static class SteamLobbyCreationPatch
    {
        public static MethodBase? TargetMethod() => ResolveSteamInviteDispatcherMethod("CreateLobby");

        [HarmonyPrefix]
        public static bool Prefix(bool isOpenForRandomMatch)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            try
            {
                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    ModLog.Warn(Feature, "Steam lobby creation skipped — Steamworks or PlayerPrefs types not found.");
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt", BindingFlags.Public | BindingFlags.Static);
                if (createLobbyMethod == null || setIntMethod == null)
                {
                    ModLog.Warn(Feature, "Steam lobby creation skipped — CreateLobby or PlayerPrefs.SetInt not found.");
                    return true;
                }

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MaxPlayers });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });
                ModLog.Info(
                    Feature,
                    $"Steam lobby created — maxPlayers={MaxPlayers}, openForMatchmaking={isOpenForRandomMatch}.");
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
