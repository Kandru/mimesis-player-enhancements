using System.Runtime.CompilerServices;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class ShopBuyPriceState
{
    private sealed class RoomState
    {
        internal int AppliedConfigGeneration = -1;
    }

    private static int _configGeneration;
    private static readonly ConditionalWeakTable<MaintenanceRoom, RoomState> States = new();

    internal static void NotifyConfigChanged() =>
        Interlocked.Increment(ref _configGeneration);

    internal static void MarkDirty(MaintenanceRoom room)
    {
        if (room == null)
            return;

        GetState(room).AppliedConfigGeneration = -1;
    }

    internal static void MarkApplied(MaintenanceRoom room)
    {
        if (room == null)
            return;

        GetState(room).AppliedConfigGeneration = Volatile.Read(ref _configGeneration);
    }

    internal static void EnsureApplied(MaintenanceRoom room)
    {
        if (room == null)
            return;

        RoomState state = GetState(room);
        if (state.AppliedConfigGeneration == _configGeneration)
            return;

        ShopBuyPriceApplier.ApplyInPlace(room);
        state.AppliedConfigGeneration = _configGeneration;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RoomState GetState(MaintenanceRoom room) =>
        States.GetOrCreateValue(room);
}
