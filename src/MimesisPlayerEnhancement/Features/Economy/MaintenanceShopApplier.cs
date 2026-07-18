using System.Runtime.CompilerServices;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class MaintenanceShopApplier
    {
        private const string Feature = "Economy";

        private sealed class RoomState
        {
            internal int AppliedConfigGeneration = -1;
            internal int AppliedPlayerCount = -1;
            internal bool LoadedFromSave;
        }

        private static int _configGeneration;
        private static readonly ConditionalWeakTable<MaintenanceRoom, RoomState> States = [];
        private static readonly ConditionalWeakTable<MaintenanceRoom, Dictionary<int, int>> BasePricesByRoom = [];
        private static readonly List<WeakReference<MaintenanceRoom>> TouchedRooms = [];

        internal static void NotifyConfigChanged()
        {
            _ = Interlocked.Increment(ref _configGeneration);
            RefreshTouchedRooms();
        }

        internal static void RefreshForPlayerCountChange()
        {
            for (int i = TouchedRooms.Count - 1; i >= 0; i--)
            {
                if (!TouchedRooms[i].TryGetTarget(out MaintenanceRoom? room) || room == null)
                {
                    TouchedRooms.RemoveAt(i);
                    continue;
                }

                RoomState state = GetState(room);
                if (state.LoadedFromSave)
                {
                    continue;
                }

                MarkDirty(room);
                EnsureApplied(room);
            }
        }

        internal static void RefreshTouchedRooms()
        {
            for (int i = TouchedRooms.Count - 1; i >= 0; i--)
            {
                if (!TouchedRooms[i].TryGetTarget(out MaintenanceRoom? room) || room == null)
                {
                    TouchedRooms.RemoveAt(i);
                    continue;
                }

                GetState(room).LoadedFromSave = false;
                EnsureApplied(room);
            }
        }

        internal static void ClearSessionState()
        {
            TouchedRooms.Clear();
        }

        /// <summary>
        /// Reverts scaled shop prices back to their cached vanilla base when the feature is
        /// toggled off mid-session. Vanilla discount rates cannot be recovered once mod
        /// discounts overwrote them, so prices revert to the undiscounted base.
        /// </summary>
        internal static void RestoreVanillaPrices()
        {
            for (int i = TouchedRooms.Count - 1; i >= 0; i--)
            {
                if (!TouchedRooms[i].TryGetTarget(out MaintenanceRoom? room) || room == null)
                {
                    TouchedRooms.RemoveAt(i);
                    continue;
                }

                if (!BasePricesByRoom.TryGetValue(room, out Dictionary<int, int>? basePrices)
                    || basePrices == null
                    || basePrices.Count == 0)
                {
                    continue;
                }

                if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                    || priceForItems.Count == 0)
                {
                    continue;
                }

                int restored = 0;
                foreach (KeyValuePair<int, ShopItemPriceInfo> entry in priceForItems)
                {
                    ShopItemPriceInfo? info = entry.Value;
                    if (info == null || !basePrices.TryGetValue(entry.Key, out int basePrice) || basePrice <= 0)
                    {
                        continue;
                    }

                    if (info.Price == basePrice && info.DiscountRate == 0f)
                    {
                        continue;
                    }

                    info.Price = basePrice;
                    info.DiscountRate = 0f;
                    restored++;
                }

                if (restored > 0)
                {
                    MaintenanceRoomAccess.SyncVendingMachines(room, priceForItems);
                    MarkDirty(room);
                    ModLog.Info(Feature, $"Shop prices restored to vanilla — {restored} items");
                }
            }
        }

        private static void TrackRoom(MaintenanceRoom room)
        {
            for (int i = TouchedRooms.Count - 1; i >= 0; i--)
            {
                if (!TouchedRooms[i].TryGetTarget(out MaintenanceRoom? existing))
                {
                    TouchedRooms.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(existing, room))
                {
                    return;
                }
            }

            TouchedRooms.Add(new WeakReference<MaintenanceRoom>(room));
        }

        internal static void PrepareForShopInit(MaintenanceRoom room)
        {
            MarkDirty(room);
            ClearBasePrices(room);
            GetState(room).LoadedFromSave = false;
        }

        internal static void ApplyAfterShopInit(MaintenanceRoom room)
        {
            ApplyBuyPrices(room);
            ApplyDiscounts(room);
            MarkApplied(room);
        }

        internal static void ApplyAfterLoad(MaintenanceRoom room)
        {
            GetState(room).LoadedFromSave = true;

            if (MaintenanceRoomAccess.GetPriceForItems(room) is Dictionary<int, ShopItemPriceInfo> priceForItems
                && priceForItems.Count > 0)
            {
                MaintenanceRoomAccess.SyncVendingMachines(room, priceForItems);
            }

            MarkApplied(room);
        }

        internal static void EnsureApplied(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            EconomyApplier.SyncIfSessionPlayerCountChanged();

            RoomState state = GetState(room);
            if (state.LoadedFromSave)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            if (state.AppliedConfigGeneration == _configGeneration
                && state.AppliedPlayerCount == playerCount)
            {
                return;
            }

            ApplyBuyPrices(room);
            ApplyDiscounts(room);
            state.AppliedConfigGeneration = _configGeneration;
            state.AppliedPlayerCount = playerCount;
        }

        private static void MarkDirty(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            GetState(room).AppliedConfigGeneration = -1;
        }

        private static void MarkApplied(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            GetState(room).AppliedConfigGeneration = Volatile.Read(ref _configGeneration);
        }

        private static void ClearBasePrices(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            _ = BasePricesByRoom.Remove(room);
        }

        private static void ApplyBuyPrices(MaintenanceRoom room)
        {
            if (!EconomyApplier.IsEnabled())
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                || priceForItems.Count == 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            float effective = EconomyResolver.GetEffectiveMultiplier(MoneyType.ShopBuyPrice, playerCount);
            EconomySceneConfig economy = SceneScopedConfigGate.Economy;
            bool modDiscountsEnabled = economy.ShopDiscountChancePercent > 0;

            Dictionary<int, int> basePrices = BasePricesByRoom.GetOrCreateValue(room);
            TrackRoom(room);

            int scaledCount = 0;
            foreach (KeyValuePair<int, ShopItemPriceInfo> entry in priceForItems)
            {
                ShopItemPriceInfo? info = entry.Value;
                if (info == null || info.Price <= 0)
                {
                    continue;
                }

                if (!basePrices.TryGetValue(entry.Key, out int basePrice))
                {
                    basePrice = GetBasePrice(info.Price, info.DiscountRate);
                    basePrices[entry.Key] = basePrice;
                }

                int scaledBase = effective == 1f ? basePrice : EconomyResolver.ScaleAmount(basePrice, effective);
                int newPrice;
                if (modDiscountsEnabled)
                {
                    info.DiscountRate = 0f;
                    newPrice = scaledBase;
                }
                else
                {
                    newPrice = ApplyDiscountRate(scaledBase, info.DiscountRate);
                }

                if (info.Price == newPrice)
                {
                    continue;
                }

                info.Price = newPrice;
                scaledCount++;
            }

            if (scaledCount == 0 && effective == 1f)
            {
                return;
            }

            MaintenanceRoomAccess.SyncVendingMachines(room, priceForItems);

            if (scaledCount > 0 || ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Info(Feature, $"Shop buy prices scaled — {scaledCount}/{priceForItems.Count} items " +
                    $"(players={playerCount}, effective={effective:0.##}×)");
            }
        }

        private static void ApplyDiscounts(MaintenanceRoom room)
        {
            if (!EconomyApplier.IsEnabled())
            {
                return;
            }

            EconomySceneConfig economy = SceneScopedConfigGate.Economy;
            if (economy.ShopDiscountChancePercent <= 0)
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                || priceForItems.Count == 0)
            {
                return;
            }

            int minPercent = economy.ShopDiscountMinPercent;
            int maxPercent = economy.ShopDiscountMaxPercent;
            int chancePercent = economy.ShopDiscountChancePercent;
            if (maxPercent < minPercent)
            {
                maxPercent = minPercent;
            }

            int discounted = 0;
            foreach (ShopItemPriceInfo info in priceForItems.Values)
            {
                if (info == null)
                {
                    continue;
                }

                int basePrice = GetBasePrice(info.Price, info.DiscountRate);
                if (basePrice <= 0)
                {
                    continue;
                }

                if (!RollDiscount(chancePercent))
                {
                    info.DiscountRate = 0f;
                    info.Price = basePrice;
                    continue;
                }

                int discountPercent = RollDiscountPercent(minPercent, maxPercent);
                info.DiscountRate = discountPercent / 100f;
                info.Price = ApplyDiscountRate(basePrice, info.DiscountRate);
                discounted++;
            }

            MaintenanceRoomAccess.SyncVendingMachines(room, priceForItems);

            if (discounted > 0 || ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Shop discounts applied — {discounted}/{priceForItems.Count} items discounted " +
                    $"(chance={chancePercent}%, range={minPercent}-{maxPercent}%)");
            }
        }

        private static int GetBasePrice(int price, float discountRate)
        {
            return price <= 0 ? 0 : discountRate is <= 0f or >= 1f ? price : Math.Max(1, (int)Math.Round(price / (1f - discountRate)));
        }

        private static int ApplyDiscountRate(int basePrice, float discountRate)
        {
            if (basePrice <= 0)
            {
                return 0;
            }

            return discountRate is <= 0f or >= 1f
                ? basePrice
                : Math.Max(1, (int)Math.Round(basePrice * (1f - discountRate)));
        }

        private static bool RollDiscount(int chancePercent)
        {
            return chancePercent >= 100 || chancePercent > 0 && SimpleRandUtil.Next(0, 10000) < chancePercent * 100;
        }

        private static int RollDiscountPercent(int minPercent, int maxPercent)
        {
            return maxPercent <= minPercent ? minPercent : SimpleRandUtil.Next(minPercent, maxPercent + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RoomState GetState(MaintenanceRoom room)
        {
            return States.GetOrCreateValue(room);
        }
    }
}
