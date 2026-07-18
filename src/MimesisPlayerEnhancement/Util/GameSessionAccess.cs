using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Cached, typed access to Hub session state (vworld, pdata, timeutil).
    /// Hub members are internal to the game assembly; one reflection hop, then typed APIs.
    /// </summary>
    internal static class GameSessionAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HubVworldField =
            typeof(Hub).GetField("vworld", InstanceFlags);

        private static readonly PropertyInfo? HubVworldProperty =
            typeof(Hub).GetProperty("vworld", InstanceFlags);

        private static readonly FieldInfo? HubPdataField =
            typeof(Hub).GetField("pdata", InstanceFlags);

        private static readonly FieldInfo? HubTimeutilField =
            typeof(Hub).GetField("timeutil", InstanceFlags);

        private static readonly PropertyInfo? HubTimeutilProperty =
            typeof(Hub).GetProperty("timeutil", InstanceFlags);

        private static readonly FieldInfo? SessionManagerField =
            typeof(VWorld).GetField("_sessionManager", InstanceFlags);

        private static readonly FieldInfo? HostSessionContextField =
            typeof(SessionManager).GetField("_hostSessionContext", InstanceFlags);

        private static readonly FieldInfo? ContextsField =
            typeof(SessionManager).GetField("m_Contexts", InstanceFlags);

        private static readonly FieldInfo? SessionVPlayerField =
            typeof(SessionContext).GetField("_vPlayer", InstanceFlags);

        internal static VWorld? TryGetVWorld()
        {
            if (Hub.s == null)
            {
                return null;
            }

            return HubVworldField?.GetValue(Hub.s) as VWorld
                   ?? HubVworldProperty?.GetValue(Hub.s) as VWorld;
        }

        internal static Hub.PersistentData? TryGetPdata()
        {
            if (Hub.s == null || HubPdataField == null)
            {
                return null;
            }

            return HubPdataField.GetValue(Hub.s) as Hub.PersistentData;
        }

        internal static TimeUtil? TryGetTimeUtil()
        {
            if (Hub.s == null)
            {
                return null;
            }

            return HubTimeutilField?.GetValue(Hub.s) as TimeUtil
                   ?? HubTimeutilProperty?.GetValue(Hub.s) as TimeUtil;
        }

        internal static int GetSaveSlotId()
        {
            return TryGetVWorld()?.SaveSlotID ?? -1;
        }

        internal static bool IsValidSaveSlotId(int slotId)
        {
            if (slotId == -1)
            {
                return false;
            }

            if (ModConfig.IsInitialized && ModConfig.EnableExtendedSaveSlots.Value)
            {
                if (slotId == SaveSlotLimits.AutosaveSlotId)
                {
                    return true;
                }

                int maxManual = SaveSlotDiscovery.GetMaxManualSlots();
                return slotId >= SaveSlotLimits.MinManualSlotId && slotId <= maxManual;
            }

            return MMSaveGameData.CheckSaveSlotID(slotId, true);
        }

        internal static bool TryGetActiveSaveSlotId(out int slotId)
        {
            slotId = GetSaveSlotId();
            return HostStatusCache.IsHostFast() && IsValidSaveSlotId(slotId);
        }

        internal static float GetCurrentTickSec()
        {
            try
            {
                return TryGetTimeUtil()?.GetCurrentTickSec() ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// Resolve SteamID for a player. Remote players via actorUIDToSteamID; host via pdata.GetUserSteamIDUInt64().
        /// </summary>
        internal static ulong ResolveSteamId(long playerUid, bool isLocal)
        {
            if (playerUid != 0)
            {
                try
                {
                    Hub.PersistentData? pdata = TryGetPdata();
                    if (pdata?.actorUIDToSteamID != null
                        && pdata.actorUIDToSteamID.TryGetValue(playerUid, out ulong steamId))
                    {
                        return steamId;
                    }
                }
                catch
                {
                    /* Hub may be tearing down */
                }
            }

            if (isLocal)
            {
                return GetLocalSteamId();
            }

            return 0;
        }

        internal static ulong GetLocalSteamId()
        {
            try
            {
                return TryGetPdata()?.GetUserSteamIDUInt64() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        internal static GameSessionInfo? TryGetGameSessionInfo()
        {
            return TryGetVWorld()?.VRoomManager?.GetGameSessionInfo();
        }

        internal static PlayReportManager? TryGetPlayReportManager()
        {
            return TryGetGameSessionInfo()?.PlayReportManager;
        }

        internal static bool IsSteamIdRegisteredInSession(ulong steamId)
        {
            if (steamId == 0)
            {
                return false;
            }

            return TryGetGameSessionInfo()?.TotalPlayerSteamIDs?.ContainsKey(steamId) == true;
        }

        internal static SessionManager? TryGetSessionManager()
        {
            try
            {
                return SessionManagerField?.GetValue(TryGetVWorld()) as SessionManager;
            }
            catch
            {
                return null;
            }
        }

        internal static IEnumerable<SessionContext> EnumerateSessionContexts(SessionManager sessionManager)
        {
            HashSet<SessionContext> seen = [];

            if (HostSessionContextField?.GetValue(sessionManager) is SessionContext host
                && host != null
                && seen.Add(host))
            {
                yield return host;
            }

            if (ContextsField?.GetValue(sessionManager) is Dictionary<long, SessionContext> contexts)
            {
                List<SessionContext> sessionContexts = [.. contexts.Values];
                foreach (SessionContext context in sessionContexts)
                {
                    if (context != null && seen.Add(context))
                    {
                        yield return context;
                    }
                }
            }
        }

        internal static VPlayer? TryGetVPlayer(SessionContext context)
        {
            return SessionVPlayerField?.GetValue(context) as VPlayer;
        }
    }
}
