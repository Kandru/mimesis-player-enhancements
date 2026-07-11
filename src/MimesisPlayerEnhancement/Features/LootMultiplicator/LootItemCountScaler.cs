namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootItemCountScaler
    {
        internal static bool TryScaleElementStack(IVroom? room, ItemElement element, LootSource source)
        {
            if (element == null || element.ItemMasterID <= 0)
            {
                return false;
            }

            int before = ItemElementStackHelper.GetStackCount(element);
            int scaled = before;
            if (!TryScaleItemCount(room, source, element.ItemMasterID, ref scaled))
            {
                return false;
            }

            if (scaled == before)
            {
                return false;
            }

            ItemElementStackHelper.SetStackCount(element, scaled);
            return true;
        }

        private static bool TryScaleItemCount(IVroom? room, LootSource source, int masterId, ref int itemCount)
        {
            if (!LootScalingGate.ShouldScale() || masterId <= 0)
            {
                return false;
            }

            // Map loot is spread across unused markers; never stack at the spawn point.
            if (source.Equals(LootSource.Map))
            {
                return false;
            }

            ItemType itemType = ItemTypeLookup.GetItemType(masterId);
            if (!itemType.Equals(ItemType.Consumable))
            {
                return false;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                source,
                itemType,
                playerCount,
                masterId);
            if (multiplier <= 1f)
            {
                return false;
            }

            int baseCount = itemCount > 0 ? itemCount : 1;
            int scaled = LootMultiplierResolver.ScaleCount(baseCount, multiplier);
            if (scaled == itemCount)
            {
                return false;
            }

            itemCount = scaled;
            return true;
        }
    }
}
