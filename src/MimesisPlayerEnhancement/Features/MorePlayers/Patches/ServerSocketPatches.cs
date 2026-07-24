namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    // game@0.3.1 Assembly-CSharp/FishySteamworks.Server/ServerSocket.cs:L378-382
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "GetMaximumClients")]
    internal static class GetMaximumClientsPatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref int __result)
        {
            if (!ModConfig.EnableMorePlayers.Value
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return true;
            }

            __result = MorePlayersPatchHelpers.GetMaxPlayers();
            return false;
        }
    }

    // game@0.3.1 Assembly-CSharp/FishySteamworks.Server/ServerSocket.cs:L373-377
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), "SetMaximumClients")]
    internal static class SetMaximumClientsPatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref int value)
        {
            if (!ModConfig.EnableMorePlayers.Value
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return true;
            }

            value = MorePlayersPatchHelpers.GetMaxPlayers();
            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/FishySteamworks.Server/ServerSocket.cs (parameterless ctor)
    [HarmonyPatch(typeof(FishySteamworks.Server.ServerSocket), MethodType.Constructor)]
    internal static class ServerSocketConstructorPatch
    {
        private const string Feature = "MorePlayers";

        [HarmonyPostfix]
        internal static void Postfix(object __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
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
}
