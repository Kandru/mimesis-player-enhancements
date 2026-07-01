using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MaintenanceShopApplier
    {
        private const string Feature = "MoneyMultiplier";

        private sealed class RoomState
        {
            internal int AppliedConfigGeneration = -1;
            internal bool LoadedFromSave;
        }

        private static int _configGeneration;
        private static readonly ConditionalWeakTable<MaintenanceRoom, RoomState> States = [];
        private static readonly ConditionalWeakTable<MaintenanceRoom, Dictionary<int, int>> BasePricesByRoom = [];

        internal static void NotifyConfigChanged()
        {
            _ = Interlocked.Increment(ref _configGeneration);
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

            RoomState state = GetState(room);
            if (state.AppliedConfigGeneration == _configGeneration || state.LoadedFromSave)
            {
                return;
            }

            ApplyBuyPrices(room);
            ApplyDiscounts(room);
            state.AppliedConfigGeneration = _configGeneration;
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
            if (!MoneyMultiplierApplier.IsEnabled())
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                || priceForItems.Count == 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopBuyPrice, playerCount);
            bool modDiscountsEnabled = ModConfig.ShopDiscountChancePercent.Value > 0;

            Dictionary<int, int> basePrices = BasePricesByRoom.GetOrCreateValue(room);

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

                int scaledBase = effective == 1f ? basePrice : MoneyMultiplierResolver.ScaleAmount(basePrice, effective);
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
                ModLog.Info(
                    Feature,
                    $"Shop buy prices scaled — {scaledCount}/{priceForItems.Count} items " +
                    $"(players={playerCount}, effective={effective:0.##}×)");
            }
        }

        private static void ApplyDiscounts(MaintenanceRoom room)
        {
            if (!MoneyMultiplierApplier.IsEnabled())
            {
                return;
            }

            if (ModConfig.ShopDiscountChancePercent.Value <= 0)
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                || priceForItems.Count == 0)
            {
                return;
            }

            int minPercent = ModConfig.ShopDiscountMinPercent.Value;
            int maxPercent = ModConfig.ShopDiscountMaxPercent.Value;
            int chancePercent = ModConfig.ShopDiscountChancePercent.Value;
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
                ModLog.Debug(
                    Feature,
                    $"Shop discounts applied — {discounted}/{priceForItems.Count} items discounted " +
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
