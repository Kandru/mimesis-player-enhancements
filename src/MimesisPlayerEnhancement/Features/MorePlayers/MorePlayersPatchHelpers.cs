using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class MorePlayersPatchHelpers
    {
        internal const int VanillaMaxPlayers = 4;
        internal static int _lastAppliedMaxClients = -1;

        internal static readonly MethodInfo GetMaxPlayersMethod =
            AccessTools.Method(typeof(MorePlayersPatchHelpers), nameof(GetMaxPlayers));

        internal static readonly MethodInfo GetLobbyPlayerCountSuffixMethod =
            AccessTools.Method(typeof(MorePlayersPatchHelpers), nameof(GetLobbyPlayerCountSuffix));

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

        internal static bool ApplyMaxClientsToSocket(int maxClients)
        {
            try
            {
                object? socket = GameNetworkApi.GetServerSocket();
                if (socket == null)
                {
                    return false;
                }

                GameNetworkApi.SetMaximumClients(socket, maxClients);
                ModLog.Info("MorePlayers", $"Server socket max clients set to {maxClients}.");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn("MorePlayers", $"Server socket refresh: {ex.Message}");
                return false;
            }
        }

        internal static void ResetSessionState()
        {
            _lastAppliedMaxClients = -1;
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
                ModLog.Warn("MorePlayers", $"Failed to read IL of {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
            }

            return false;
        }
    }
}
