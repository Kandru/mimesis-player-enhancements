using System.Reflection;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotSteamListGuard
    {
        private static readonly FieldInfo? SteamInviteField =
            AccessTools.Field(typeof(Hub), "steamInviteDispatcher");

        internal static void DetachSavePickerFromSteam(UIPrefab_PublicRoomList list)
        {
            SteamInviteDispatcher? dispatcher = TryGetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            if (ReferenceEquals(dispatcher.roomListUI, list))
            {
                dispatcher.roomListUI = null;
            }
        }

        private static SteamInviteDispatcher? TryGetSteamInviteDispatcher()
        {
            if (Hub.s == null || SteamInviteField == null)
            {
                return null;
            }

            return SteamInviteField.GetValue(Hub.s) as SteamInviteDispatcher;
        }
    }
}
