using System.Linq;
using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Local subset of <a href="https://github.com/NeoMimicry/MimicAPI">MimicAPI</a> used by MorePlayers.
    /// We intentionally do <b>not</b> take a runtime dependency on MimicAPI.dll so this mod ships as a single DLL.
    /// When MIMESIS or MimicAPI changes, compare against upstream and update this file.
    /// </summary>
    /// <remarks>
    /// Last synced with MimicAPI <b>0.3.0</b> (Thunderstore: NeoMimicry/MimicAPI).
    /// <list type="bullet">
    ///   <item><see cref="GameNetworkApi"/> — <c>MimicAPI/GameAPI/ServerNetworkAPI.cs</c></item>
    ///   <item><see cref="ReflectionHelper"/> — <c>MimicAPI/GameAPI/ReflectionHelper.cs</c></item>
    ///   <item><see cref="GetHub"/> / <see cref="GetVWorld"/> — <c>MimicAPI/GameAPI/CoreAPI.cs</c></item>
    /// </list>
    /// Upstream tree: https://github.com/NeoMimicry/MimicAPI/tree/main/MimicAPI/GameAPI
    /// </remarks>
    internal static class GameNetworkApi
    {
        // MimicAPI: ServerNetworkAPI.cs — GetGameAssembly()
        // https://github.com/NeoMimicry/MimicAPI/blob/main/MimicAPI/GameAPI/ServerNetworkAPI.cs
        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static Assembly? GetGameAssembly()
        {
            try
            {
                Assembly loaded = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                if (loaded != null)
                {
                    return loaded;
                }

                // MelonLoader can run mod init before Assembly-CSharp appears in GetAssemblies().
                // Touching a compile-time game type forces the same assembly Harmony uses for patches.
                return typeof(IVroom).Assembly;
            }
            catch
            {
                return null;
            }
        }

        // MimicAPI: ServerNetworkAPI.GetIVroomType()
        public static Type? GetIVroomType()
        {
            return GetGameAssembly()?.GetType("IVroom");
        }

        // MimicAPI: ServerNetworkAPI.GetGameSessionInfoType()
        public static Type? GetGameSessionInfoType()
        {
            return GetGameAssembly()?.GetType("GameSessionInfo");
        }

        // MimicAPI: ServerNetworkAPI.GetServerSocket() → GetSdrServer() ?? GetRudpServer()
        public static object? GetServerSocket()
        {
            object? vworld = GetVWorld();
            return vworld == null
                ? null
                : ReflectionHelper.GetFieldValue(vworld, "_sdrServer")
                   ?? ReflectionHelper.GetFieldValue(vworld, "_rudpServer");
        }

        // MimicAPI: ServerNetworkAPI.SetMaximumClients()
        public static void SetMaximumClients(object serverSocket, int value)
        {
            ReflectionHelper.SetFieldValue(serverSocket, "_maximumClients", value);
        }

        // MimicAPI: CoreAPI.GetVWorld() via ServerNetworkAPI.GetVWorld()
        // https://github.com/NeoMimicry/MimicAPI/blob/main/MimicAPI/GameAPI/CoreAPI.cs
        private static object? GetVWorld()
        {
            object? hub = GetHub();
            return hub == null ? null : ReflectionHelper.GetFieldValue(hub, "<VWorld>k__BackingField");
        }

        // MimicAPI: CoreAPI.GetHub() — upstream uses typed Hub.s; we resolve via reflection.
        public static object? GetHub()
        {
            Type? hubType = GetGameAssembly()?.GetType("Hub");
            return hubType?.GetProperty("s", StaticFlags)?.GetValue(null);
        }

    }

    /// <summary>
    /// Local subset of MimicAPI.ReflectionHelper (only methods used by MorePlayers).
    /// </summary>
    /// <remarks>
    /// Upstream: https://github.com/NeoMimicry/MimicAPI/blob/main/MimicAPI/GameAPI/ReflectionHelper.cs
    /// </remarks>
    internal static class ReflectionHelper
    {
        private const BindingFlags DefaultFlags =
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

        public static object? GetFieldValue(object target, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            FieldInfo field = target.GetType().GetField(fieldName, DefaultFlags);
            return field?.GetValue(target);
        }

        public static T GetFieldValue<T>(object target, string fieldName)
        {
            object? value = GetFieldValue(target, fieldName);
            return value == null ? default! : (T)value;
        }

        public static void SetFieldValue(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, DefaultFlags);
            field?.SetValue(target, value);
        }
    }
}
