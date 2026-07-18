using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Caches host/server status to avoid repeated reflection on hot paths.
    /// Invalidated on session start/end and deinit.
    /// </summary>
    internal static class HostStatusCache
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const int RecheckIntervalFrames = 60;

        private static readonly Type? NetworkManagerType =
            Type.GetType("FishNet.Managing.NetworkManager, FishNet.Runtime");

        private static readonly PropertyInfo? NetworkManagerInstanceProp =
            NetworkManagerType?.GetProperty("Instance", StaticFlags);

        private static readonly PropertyInfo? NetworkManagerIsServerProp =
            NetworkManagerType?.GetProperty("IsServer", InstanceFlags);

        private static FieldInfo? _sessionManagerField;
        private static FieldInfo? _hostSessionContextField;
        private static FieldInfo? _vPlayerField;
        private static PropertyInfo? _vPlayerIsHostProp;

        private static bool? _cachedIsHost;
        private static int _lastCheckFrame = -RecheckIntervalFrames;

        internal static void Invalidate()
        {
            _cachedIsHost = null;
            _lastCheckFrame = -RecheckIntervalFrames;
        }

        internal static bool IsHostFast()
        {
            int frame = UnityEngine.Time.frameCount;
            if (_cachedIsHost.HasValue && frame - _lastCheckFrame < RecheckIntervalFrames)
            {
                return _cachedIsHost.Value;
            }

            _cachedIsHost = ComputeIsHost();
            _lastCheckFrame = frame;
            return _cachedIsHost.Value;
        }

        private static bool ComputeIsHost()
        {
            return IsHostViaNetwork() || IsHostViaVWorld();
        }

        private static bool IsHostViaNetwork()
        {
            if (NetworkManagerType == null
                || NetworkManagerInstanceProp == null
                || NetworkManagerIsServerProp == null)
            {
                return false;
            }

            try
            {
                object? nm = NetworkManagerInstanceProp.GetValue(null);
                return nm != null && (bool)NetworkManagerIsServerProp.GetValue(nm);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHostViaVWorld()
        {
            try
            {
                VWorld? vworld = GameSessionAccess.TryGetVWorld();
                if (vworld == null)
                {
                    return false;
                }

                EnsureVWorldHostChainResolved(vworld.GetType());

                if (_sessionManagerField == null
                    || _hostSessionContextField == null
                    || _vPlayerField == null
                    || _vPlayerIsHostProp == null)
                {
                    return false;
                }

                object? sessionMgr = _sessionManagerField.GetValue(vworld);
                if (sessionMgr == null)
                {
                    return false;
                }

                object? hostCtx = _hostSessionContextField.GetValue(sessionMgr);
                if (hostCtx == null)
                {
                    return false;
                }

                object? vplayer = _vPlayerField.GetValue(hostCtx);
                return vplayer != null && (bool)_vPlayerIsHostProp.GetValue(vplayer);
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureVWorldHostChainResolved(Type vworldType)
        {
            if (_sessionManagerField != null)
            {
                return;
            }

            _sessionManagerField = vworldType.GetField("_sessionManager", InstanceFlags);
            if (_sessionManagerField == null)
            {
                return;
            }

            Type sessionMgrType = _sessionManagerField.FieldType;
            _hostSessionContextField = sessionMgrType.GetField("_hostSessionContext", InstanceFlags);
            if (_hostSessionContextField == null)
            {
                return;
            }

            Type hostCtxType = _hostSessionContextField.FieldType;
            _vPlayerField = hostCtxType.GetField("_vPlayer", InstanceFlags);
            if (_vPlayerField == null)
            {
                return;
            }

            Type vplayerType = _vPlayerField.FieldType;
            _vPlayerIsHostProp = vplayerType.GetProperty("IsHost", InstanceFlags);
        }
    }
}
