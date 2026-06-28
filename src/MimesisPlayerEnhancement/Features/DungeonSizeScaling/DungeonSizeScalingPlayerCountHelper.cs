using System.Reflection;

namespace MimesisPlayerEnhancement.Features.DungeonSizeScaling;

internal static class DungeonSizeScalingPlayerCountHelper
{
    private const int VanillaPlayerBaseline = 4;

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly PropertyInfo? HubVWorldProperty =
        typeof(Hub).GetProperty("vworld", InstanceFlags);

    private static readonly PropertyInfo? VWorldRoomManagerProperty =
        typeof(VWorld).GetProperty("VRoomManager", InstanceFlags);

    internal static int ResolveSessionPlayerCount()
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
}
