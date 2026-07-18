using System.Reflection;
using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeHub
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? SteamInviteField =
            typeof(Hub).GetField("steamInviteDispatcher", InstanceFlags);

        private static readonly FieldInfo? IsPublicRoomField =
            typeof(SteamInviteDispatcher).GetField("isPublicRoom", InstanceFlags);

        private static FieldInfo? _uimanField;
        private static PropertyInfo? _uimanProperty;

        private static bool _warnedMissingSteamInviteField;

        internal static SteamInviteDispatcher? GetSteamInviteDispatcher()
        {
            if (Hub.s == null)
            {
                return null;
            }

            if (SteamInviteField == null)
            {
                WarnMissingSteamInviteFieldOnce();
                return null;
            }

            return SteamInviteField.GetValue(Hub.s) as SteamInviteDispatcher;
        }

        internal static bool IsHostLobbyPublic(SteamInviteDispatcher? dispatcher)
        {
            if (dispatcher == null)
            {
                return false;
            }

            if (JoinAnytimeLobbyController.HostWantsPublicMatchmaking())
            {
                return true;
            }

            if (IsPublicRoomField != null && IsPublicRoomField.GetValue(dispatcher) is true)
            {
                return true;
            }

            return ReadPublicRoomFromSteam(dispatcher);
        }

        internal static bool ReadPublicRoomFromSteam(SteamInviteDispatcher dispatcher)
        {
            if (dispatcher.joinedLobbyID == CSteamID.Nil)
            {
                return false;
            }

            try
            {
                string value = SteamMatchmaking.GetLobbyData(
                    dispatcher.joinedLobbyID,
                    SteamInviteDispatcher.IS_PUBLIC_KEY);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Read PublicRoom lobby data failed — {ex.Message}");
                return false;
            }
        }

        internal static void SyncIsPublicRoomField(SteamInviteDispatcher dispatcher, bool isPublic)
        {
            if (IsPublicRoomField == null)
            {
                return;
            }

            try
            {
                IsPublicRoomField.SetValue(dispatcher, isPublic);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Sync isPublicRoom field failed — {ex.Message}");
            }
        }

        internal static void SyncIsPublicLobby(bool isPublic)
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return;
            }

            try
            {
                pdata.IsPublicLobby = isPublic;
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Sync IsPublicLobby failed — {ex.Message}");
            }
        }

        internal static bool IsHost()
        {
            return GameSessionAccess.TryGetPdata()?.ClientMode == NetworkClientMode.Host;
        }

        internal static UIPrefab_InGameMenu? GetInGameMenu()
        {
            if (Hub.s == null)
            {
                return null;
            }

            UIManager? uiman = ResolveUiManager();
            return uiman?.inGameMenu;
        }

        private static UIManager? ResolveUiManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            _uimanProperty ??= typeof(Hub).GetProperty("uiman", InstanceFlags);
            if (_uimanProperty?.GetValue(Hub.s) is UIManager propertyManager)
            {
                return propertyManager;
            }

            _uimanField ??= typeof(Hub).GetField("uiman", InstanceFlags)
                ?? typeof(Hub).GetField("<uiman>k__BackingField", InstanceFlags);
            return _uimanField?.GetValue(Hub.s) as UIManager;
        }

        private static void WarnMissingSteamInviteFieldOnce()
        {
            if (_warnedMissingSteamInviteField)
            {
                return;
            }

            _warnedMissingSteamInviteField = true;
            ModLog.Warn(Feature, "Hub reflection field missing — steamInviteDispatcher not found");
        }
    }
}
