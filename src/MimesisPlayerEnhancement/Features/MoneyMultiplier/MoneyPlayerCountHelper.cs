using System.Reflection;
using MimesisPlayerEnhancement.Features.LootMultiplicator;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyPlayerCountHelper
{
    private const int VanillaPlayerBaseline = 4;

    internal static int ResolveFromRoom(MaintenanceRoom? room) =>
        LootPlayerCountHelper.ResolvePlayerCount(room);

    internal static int ResolveFromSession(GameSessionInfo? info)
    {
        if (info?.TotalPlayerSteamIDs != null && info.TotalPlayerSteamIDs.Count > 0)
            return info.TotalPlayerSteamIDs.Count;

        return VanillaPlayerBaseline;
    }

    internal static int ResolveForItemPrices()
    {
        if (Hub.s == null)
            return VanillaPlayerBaseline;

        var hubType = typeof(Hub);
        var vworldProp = hubType.GetProperty("vworld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (vworldProp?.GetValue(Hub.s) is not VWorld vworld)
            return VanillaPlayerBaseline;

        var roomManagerProp = typeof(VWorld).GetProperty("VRoomManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (roomManagerProp?.GetValue(vworld) is not VRoomManager roomManager)
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
