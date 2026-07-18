namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyApplier
    {
        private const string Feature = "Economy";

        private static int _scrapScaleFrame = -1;
        private static int _scrapScalePlayerCount = SessionPlayerCountHelper.VanillaPlayerBaseline;
        private static float _scrapEffectiveMultiplier = FeatureToggleGate.NeutralMultiplier;
        private static bool _scrapScalingAnnounced;
        private static int _staticScrapPriceCacheGeneration;
        private static int _trackedSessionPlayerCount = -1;
        private static readonly Dictionary<ScaledMeanPriceKey, int> _scaledMeanPriceByMasterId = [];
        private static readonly Dictionary<StaticScrapPriceKey, int> _staticScrapPriceByValue = [];

        internal static bool IsEnabled()
        {
            return SceneScopedConfigGate.Economy.EnableEconomy && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static bool ShouldRetainUnspentCurrency()
        {
            EconomySceneConfig config = SceneScopedConfigGate.Economy;
            return config.EnableEconomy
                && config.RetainUnspentCurrencyBetweenCycles
                && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static bool TryGetVanillaInitialMoney(out int value)
        {
            value = 0;
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            try
            {
                value = excel.Consts.C_InitialMoney;
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Vanilla initial money unavailable — startup money scaling skipped — {ex.Message}");
                return false;
            }
        }

        internal static int ScaleForType(MoneyType type, int vanilla, int playerCount, bool logAsInfo = false)
        {
            float effective = EconomyResolver.GetEffectiveMultiplier(type, playerCount);
            int scaled = EconomyResolver.ScaleAmount(vanilla, effective);
            if (logAsInfo)
            {
                EconomyLog.InfoApplied(type, vanilla, scaled, playerCount, effective);
            }
            else
            {
                EconomyLog.DebugScaled(type, vanilla, scaled, playerCount, effective);
            }

            return scaled;
        }

        internal static void ApplyStartupMoney(MaintenanceRoom room, ref int currency)
        {
            if (!IsEnabled() || StartupMoneyLoadGuard.IsActive)
            {
                return;
            }

            if (!TryGetVanillaInitialMoney(out int vanillaInitial) || currency != vanillaInitial)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            currency = ScaleForType(MoneyType.Startup, currency, playerCount, logAsInfo: true);
        }

        internal static void InvalidateScrapScaling()
        {
            _scrapScaleFrame = -1;
            _scrapEffectiveMultiplier = FeatureToggleGate.NeutralMultiplier;
            _scrapScalingAnnounced = false;
            _staticScrapPriceCacheGeneration++;
            _scaledMeanPriceByMasterId.Clear();
            _staticScrapPriceByValue.Clear();
        }

        internal static void OnSessionEnded()
        {
            InvalidateScrapScaling();
            _trackedSessionPlayerCount = -1;
            MaintenanceShopApplier.ClearSessionState();
        }

        internal static void SyncIfSessionPlayerCountChanged()
        {
            if (!IsEnabled())
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromSession();
            if (playerCount == _trackedSessionPlayerCount)
            {
                return;
            }

            _trackedSessionPlayerCount = playerCount;
            MaintenanceShopApplier.RefreshForPlayerCountChange();
        }

        internal static void WarmStaticScrapPriceCache(ItemElement item)
        {
            if (!IsEnabled() || item.IsFake || item.Price <= 0 || HasDynamicScrapPricing(item))
            {
                return;
            }

            ResolveScrapEffectiveMultiplier(out int playerCount);
            CacheStaticScrapPrice(item.ItemMasterID, item.Price, playerCount);
        }

        internal static int ResolveScrapSellPrice(ItemElement item, int vanillaPrice)
        {
            if (!IsEnabled() || vanillaPrice == 0 || item.IsFake)
            {
                return vanillaPrice;
            }

            if (HasDynamicScrapPricing(item))
            {
                return ScaleScrapValue(vanillaPrice);
            }

            ResolveScrapEffectiveMultiplier(out int playerCount);
            if (TryGetCachedStaticScrapPrice(item.ItemMasterID, vanillaPrice, playerCount, out int cachedPrice))
            {
                return cachedPrice;
            }

            return CacheStaticScrapPrice(item.ItemMasterID, vanillaPrice, playerCount);
        }

        internal static int ScaleScrapValue(int vanilla)
        {
            if (!IsEnabled() || vanilla == 0)
            {
                return vanilla;
            }

            float effective = ResolveScrapEffectiveMultiplier(out int playerCount);
            if (effective == 1f)
            {
                return vanilla;
            }

            AnnounceScrapScalingOnce(playerCount, effective);
            return EconomyResolver.ScaleAmount(vanilla, effective);
        }

        internal static int ScaleCachedScrapMeanPrice(int masterId, int vanillaMean)
        {
            if (!IsEnabled() || vanillaMean == 0 || masterId <= 0)
            {
                return vanillaMean;
            }

            float effective = ResolveScrapEffectiveMultiplier(out int playerCount);
            if (effective == 1f)
            {
                return vanillaMean;
            }

            ScaledMeanPriceKey meanKey = new(masterId, playerCount);
            if (_scaledMeanPriceByMasterId.TryGetValue(meanKey, out int cached))
            {
                return cached;
            }

            AnnounceScrapScalingOnce(playerCount, effective);
            int scaled = EconomyResolver.ScaleAmount(vanillaMean, effective);
            _scaledMeanPriceByMasterId[meanKey] = scaled;
            return scaled;
        }

        internal static int ScaleReinforcePrice(MaintenanceRoom room, int vanilla)
        {
            if (!IsEnabled())
            {
                return vanilla;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            return ScaleForType(MoneyType.ReinforcePrice, vanilla, playerCount);
        }

        internal static int ScaleReinforceCost(int upgradeCost, MaintenanceRoom room)
        {
            return ScaleReinforcePrice(room, upgradeCost);
        }

        private static int CacheStaticScrapPrice(int masterId, int vanillaPrice, int playerCount)
        {
            StaticScrapPriceKey key = new(masterId, vanillaPrice, _staticScrapPriceCacheGeneration, playerCount);
            if (_staticScrapPriceByValue.TryGetValue(key, out int cached))
            {
                return cached;
            }

            int scaled = ScaleScrapValue(vanillaPrice);
            _staticScrapPriceByValue[key] = scaled;
            return scaled;
        }

        private static bool TryGetCachedStaticScrapPrice(int masterId, int vanillaPrice, int playerCount, out int scaledPrice)
        {
            return _staticScrapPriceByValue.TryGetValue(
                new StaticScrapPriceKey(masterId, vanillaPrice, _staticScrapPriceCacheGeneration, playerCount),
                out scaledPrice);
        }

        private readonly struct ScaledMeanPriceKey(int masterId, int playerCount) : IEquatable<ScaledMeanPriceKey>
        {
            private readonly int _masterId = masterId;
            private readonly int _playerCount = playerCount;

            public bool Equals(ScaledMeanPriceKey other) =>
                _masterId == other._masterId && _playerCount == other._playerCount;

            public override bool Equals(object? obj) => obj is ScaledMeanPriceKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_masterId, _playerCount);
        }

        private readonly struct StaticScrapPriceKey(int masterId, int vanillaPrice, int generation, int playerCount)
            : IEquatable<StaticScrapPriceKey>
        {
            private readonly int _masterId = masterId;
            private readonly int _vanillaPrice = vanillaPrice;
            private readonly int _generation = generation;
            private readonly int _playerCount = playerCount;

            public bool Equals(StaticScrapPriceKey other) =>
                _masterId == other._masterId
                && _vanillaPrice == other._vanillaPrice
                && _generation == other._generation
                && _playerCount == other._playerCount;

            public override bool Equals(object? obj) => obj is StaticScrapPriceKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_masterId, _vanillaPrice, _generation, _playerCount);
        }

        private static bool HasDynamicScrapPricing(ItemElement item)
        {
            if (item is not EquipmentItemElement equipment)
            {
                return false;
            }

            ItemMasterInfo? info = HubGameDataAccess.Excel?.GetItemInfo(item.ItemMasterID);
            if (info is not ItemEquipmentInfo equipmentInfo)
            {
                return false;
            }

            if (equipment.RemainAmount == -1 && equipmentInfo.OverflowPrice != 0)
            {
                return true;
            }

            return equipmentInfo.PriceIncPerGauge > 0;
        }

        private static float ResolveScrapEffectiveMultiplier(out int playerCount)
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame != _scrapScaleFrame)
            {
                _scrapScaleFrame = frame;
                _scrapScalePlayerCount = SessionPlayerCountHelper.ResolveFromSession();
                _scrapEffectiveMultiplier = EconomyResolver.GetEffectiveMultiplier(
                    MoneyType.ScrapSellValue,
                    _scrapScalePlayerCount);
                SyncIfSessionPlayerCountChanged();
            }

            playerCount = _scrapScalePlayerCount;
            return _scrapEffectiveMultiplier;
        }

        private static void AnnounceScrapScalingOnce(int playerCount, float effectiveMultiplier)
        {
            if (_scrapScalingAnnounced)
            {
                return;
            }

            _scrapScalingAnnounced = true;
            EconomyLog.InfoScrapScalingActive(playerCount, effectiveMultiplier);
        }
    }
}
