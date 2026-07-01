using ReluNetwork.ConstEnum;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Host ESC menu: unlock public-room controls outside maintenance and mirror intent on the toggle.
    /// The toggle listener is installed once in Start — never re-bound on OnEnable.
    /// </summary>
    internal static class JoinAnytimeInGameMenuTools
    {
        private const string Feature = "JoinAnytime";

        private static int _programmaticSyncDepth;

        internal static bool IsProgrammaticSync => _programmaticSyncDepth > 0;

        internal static void OnMenuStart(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host
                || menu == null)
            {
                return;
            }

            InstallHostPublicRoomListener(menu);
            // Vanilla Start() sets isOn=false after our first OnEnable sync (Unity runs OnEnable before Start).
            SyncToggleDisplay(menu, JoinAnytimeLobbyController.HostWantsPublicMatchmaking());
        }

        internal static void InstallHostPublicRoomListener(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host
                || menu == null)
            {
                return;
            }

            try
            {
                Toggle toggle = menu.UE_PublicRoomToggle.GetComponent<Toggle>();
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(OnPublicRoomToggleChanged);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug(Feature, $"Public room listener install failed — {ex.Message}");
            }
        }

        internal static void EnsurePublicRoomControlsAccessible(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host
                || menu == null)
            {
                return;
            }

            try
            {
                menu.UE_PublicRoom.GetComponent<RectTransform>().SetAsLastSibling();
                menu.UE_RoomPassword.GetComponent<RectTransform>().SetAsLastSibling();
                SyncToggleDisplay(menu, JoinAnytimeLobbyController.HostWantsPublicMatchmaking());
            }
            catch (System.Exception ex)
            {
                ModLog.Debug(Feature, $"Public room menu unlock failed — {ex.Message}");
            }
        }

        internal static void SyncToggleDisplay(UIPrefab_InGameMenu? menu, bool isPublic)
        {
            if (menu == null)
            {
                return;
            }

            try
            {
                _programmaticSyncDepth++;
                menu.UE_PublicRoomToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(isPublic);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug(Feature, $"Public room toggle sync failed — {ex.Message}");
            }
            finally
            {
                _programmaticSyncDepth--;
            }
        }

        private static void OnPublicRoomToggleChanged(bool isPublic)
        {
            if (IsProgrammaticSync)
            {
                return;
            }

            JoinAnytimeLobbyController.ApplyUserPublicMatchmakingChoice(isPublic);
        }
    }
}
