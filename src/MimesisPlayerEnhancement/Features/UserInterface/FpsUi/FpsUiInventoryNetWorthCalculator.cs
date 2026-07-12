using Mimic;

namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi
{
    internal static class FpsUiInventoryNetWorthCalculator
    {
        internal static int ComputeTotal(ProtoActor actor)
        {
            if (TryComputeFromItemElements(actor.ActorID, out int total))
            {
                return total;
            }

            return ComputeFromInventoryItems(actor.GetInventoryItems());
        }

        internal static int ComputeFromInventoryItems(IReadOnlyList<InventoryItem?> inventoryItems)
        {
            int total = 0;
            foreach (InventoryItem? item in inventoryItems)
            {
                if (item == null || item.IsFake)
                {
                    continue;
                }

                total += ResolveSellPrice(item);
            }

            return total;
        }

        private static bool TryComputeFromItemElements(int actorId, out int total)
        {
            total = 0;
            VPlayer? player = TryFindPlayer(actorId);
            if (player?.InventoryControlUnit == null)
            {
                return false;
            }

            foreach (ItemElement? element in player.InventoryControlUnit.GetAllItemElements().Values)
            {
                if (element == null || element.IsFake)
                {
                    continue;
                }

                total += element.FinalPrice;
            }

            return true;
        }

        private static VPlayer? TryFindPlayer(int actorId)
        {
            if (actorId == 0)
            {
                return null;
            }

            VRoomManager? vroomManager = GameSessionAccess.TryGetVWorld()?.VRoomManager;
            if (vroomManager == null)
            {
                return null;
            }

            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return null;
            }

            foreach (IVroom room in rooms.Values)
            {
                VPlayer? player = room.FindPlayerByObjectID(actorId);
                if (player != null)
                {
                    return player;
                }
            }

            return null;
        }

        private static int ResolveSellPrice(InventoryItem item)
        {
            ItemMasterInfo info = item.MasterInfo;
            int price = item.Price;
            if (info is ItemEquipmentInfo equipInfo)
            {
                if (item.RemainGauge == -1 && equipInfo.OverflowPrice != 0)
                {
                    return equipInfo.OverflowPrice;
                }

                if (equipInfo.PriceIncPerGauge > 0)
                {
                    return (int)(price + equipInfo.PriceIncPerGauge * item.RemainGauge * 0.01);
                }
            }

            return price;
        }
    }
}
