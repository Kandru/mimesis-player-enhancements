using System.Runtime.CompilerServices;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class TrainDepositTracker
    {
        private sealed class CarrierAttribution
        {
            internal ulong SteamId;
        }

        private static readonly ConditionalWeakTable<ItemElement, CarrierAttribution> FirstCarrierByItem = new();

        private static ulong _pendingDepositor;
        private static ItemElement? _pendingItem;
        private static int _pendingValue;

        internal static void OnItemPickedUp(ItemElement item, ulong steamId)
        {
            if (item == null || steamId == 0)
            {
                return;
            }

            _ = FirstCarrierByItem.GetValue(item, _ => new CarrierAttribution { SteamId = steamId });
        }

        internal static void BeginDeposit(VPlayer player)
        {
            if (player == null || player.SteamID == 0)
            {
                return;
            }

            _pendingDepositor = player.SteamID;
            _pendingItem = null;
            _pendingValue = 0;

            InventoryController? inventory = player.InventoryControlUnit;
            if (inventory == null)
            {
                return;
            }

            Dictionary<int, ItemElement>? items = inventory.GetAllItemElements();
            if (items == null || !items.TryGetValue(0, out ItemElement? item) || item == null)
            {
                return;
            }

            _pendingItem = item;
            _pendingValue = item.FinalPrice;
        }

        internal static void CompleteDeposit()
        {
            if (_pendingValue <= 0)
            {
                ClearPending();
                return;
            }

            ulong creditTo = _pendingDepositor;
            if (_pendingItem != null
                && FirstCarrierByItem.TryGetValue(_pendingItem, out CarrierAttribution? attribution)
                && attribution!.SteamId != 0)
            {
                creditTo = attribution.SteamId;
            }

            StatisticsCounterWriter.Modify(creditTo, counters =>
            {
                counters.TrainValueDeposited += _pendingValue;
                counters.CurrencyEarned += _pendingValue;
            });
            ClearPending();
        }

        internal static void ClearDungeonState()
        {
            ClearPending();
        }

        private static void ClearPending()
        {
            _pendingDepositor = 0;
            _pendingItem = null;
            _pendingValue = 0;
        }
    }
}
