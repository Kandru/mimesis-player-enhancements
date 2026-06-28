using System.Reflection;

namespace MimesisPlayerEnhancement.Util;

internal static class SessionPlayerCountHelper
{
    internal const int VanillaPlayerBaseline = 4;

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly PropertyInfo? HubVWorldProperty =
        typeof(Hub).GetProperty("vworld", InstanceFlags);

    private static readonly PropertyInfo? VWorldRoomManagerProperty =
        typeof(VWorld).GetProperty("VRoomManager", InstanceFlags);

    internal static int ResolveFromRoom(IVroom? room)
    {
        if (room != null)
        {
            try
            {
                return room.GetMemberCount();
            }
            catch
            {
                // Fall through to session count.
            }
        }

        return ResolveFromSession();
    }

    internal static int ResolveFromSession()
    {
        if (Hub.s == null)
            return VanillaPlayerBaseline;

        if (HubVWorldProperty?.GetValue(Hub.s) is not VWorld vworld)
            return VanillaPlayerBaseline;

        if (VWorldRoomManagerProperty?.GetValue(vworld) is not VRoomManager roomManager)
            return VanillaPlayerBaseline;

        try
        {
            int count = roomManager.GetPlayerCountInSession();
            return count > 0 ? count : VanillaPlayerBaseline;
        }
        catch
        {
            return VanillaPlayerBaseline;
        }
    }

    internal static int ResolveFromSession(GameSessionInfo? info)
    {
        if (info?.TotalPlayerSteamIDs != null && info.TotalPlayerSteamIDs.Count > 0)
            return info.TotalPlayerSteamIDs.Count;

        return ResolveFromSession();
    }
}
