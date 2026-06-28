namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyPlayerCountHelper
{
    internal static int ResolveFromRoom(MaintenanceRoom? room) =>
        Util.SessionPlayerCountHelper.ResolveFromRoom(room);

    internal static int ResolveFromSession(GameSessionInfo? info) =>
        Util.SessionPlayerCountHelper.ResolveFromSession(info);

    internal static int ResolveForItemPrices() =>
        Util.SessionPlayerCountHelper.ResolveFromSession();
}
