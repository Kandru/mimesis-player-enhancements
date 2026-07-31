namespace MimesisPlayerEnhancement.Util
{
    internal readonly struct LootMultiplicatorSceneConfig
    {
        internal LootMultiplicatorSceneConfig(
            bool enableLootMultiplicator,
            float lootMultiplicatorPlayerCountScaleRate,
            bool autoScaleMapLootByPlayerCount,
            float mapLootMultiplier,
            bool autoScaleDropLootByPlayerCount,
            float dropLootMultiplier,
            string lootItemFilterMode,
            string lootAllowlist,
            string lootBlocklist,
            bool autoScaleMapLootBudgetForFilter,
            int convertFakeActorDyingDropChancePercent)
        {
            EnableLootMultiplicator = enableLootMultiplicator;
            LootMultiplicatorPlayerCountScaleRate = lootMultiplicatorPlayerCountScaleRate;
            AutoScaleMapLootByPlayerCount = autoScaleMapLootByPlayerCount;
            MapLootMultiplier = mapLootMultiplier;
            AutoScaleDropLootByPlayerCount = autoScaleDropLootByPlayerCount;
            DropLootMultiplier = dropLootMultiplier;
            LootItemFilterMode = lootItemFilterMode;
            LootAllowlist = lootAllowlist;
            LootBlocklist = lootBlocklist;
            AutoScaleMapLootBudgetForFilter = autoScaleMapLootBudgetForFilter;
            ConvertFakeActorDyingDropChancePercent = convertFakeActorDyingDropChancePercent;
        }

        internal bool EnableLootMultiplicator { get; }

        internal float LootMultiplicatorPlayerCountScaleRate { get; }

        internal bool AutoScaleMapLootByPlayerCount { get; }

        internal float MapLootMultiplier { get; }

        internal bool AutoScaleDropLootByPlayerCount { get; }

        internal float DropLootMultiplier { get; }

        internal string LootItemFilterMode { get; }

        internal string LootAllowlist { get; }

        internal string LootBlocklist { get; }

        internal bool AutoScaleMapLootBudgetForFilter { get; }

        internal int ConvertFakeActorDyingDropChancePercent { get; }

        internal static LootMultiplicatorSceneConfig CaptureFromModConfig()
        {
            return new LootMultiplicatorSceneConfig(
                ModConfig.EnableLootMultiplicator.Value,
                ModConfig.LootMultiplicatorPlayerCountScaleRate.Value,
                ModConfig.AutoScaleMapLootByPlayerCount.Value,
                ModConfig.MapLootMultiplier.Value,
                ModConfig.AutoScaleDropLootByPlayerCount.Value,
                ModConfig.DropLootMultiplier.Value,
                ModConfig.LootItemFilterMode.Value ?? "",
                ModConfig.LootAllowlist.Value ?? "",
                ModConfig.LootBlocklist.Value ?? "",
                ModConfig.AutoScaleMapLootBudgetForFilter.Value,
                ModConfig.ConvertFakeActorDyingDropChancePercent.Value);
        }
    }

    internal readonly struct SpawnScalingSceneConfig
    {
        internal SpawnScalingSceneConfig(
            bool enableSpawnScaling,
            float spawnScalingPlayerCountScaleRate,
            bool autoScaleMimicSpawnsByPlayerCount,
            float mimicSpawnMultiplier,
            bool autoScaleBossSpawnsByPlayerCount,
            float bossSpawnMultiplier,
            bool autoScaleJakoSpawnsByPlayerCount,
            float jakoSpawnMultiplier,
            bool autoScaleSpecialSpawnsByPlayerCount,
            float specialSpawnMultiplier,
            bool autoScaleTrapSpawnsByPlayerCount,
            float trapSpawnMultiplier,
            string trapRespawnMode,
            float trapRespawnDelaySeconds,
            float trapRespawnDelayMinSeconds,
            float trapRespawnDelayMaxSeconds,
            float trapRespawnMinPlayerDistanceMeters,
            bool autoScaleOtherSpawnsByPlayerCount,
            float otherSpawnMultiplier,
            string ambientMonsterWaveMode,
            float ambientMonsterWaveInitialDelaySeconds,
            float ambientMonsterWaveInitialDelayMinSeconds,
            float ambientMonsterWaveInitialDelayMaxSeconds,
            float ambientMonsterWaveIntervalSeconds,
            float ambientMonsterWaveIntervalMinSeconds,
            float ambientMonsterWaveIntervalMaxSeconds,
            float bonusEncounterDelayMinSeconds,
            float bonusEncounterDelayMaxSeconds,
            float bonusEncounterMinPlayerDistanceMeters)
        {
            EnableSpawnScaling = enableSpawnScaling;
            SpawnScalingPlayerCountScaleRate = spawnScalingPlayerCountScaleRate;
            AutoScaleMimicSpawnsByPlayerCount = autoScaleMimicSpawnsByPlayerCount;
            MimicSpawnMultiplier = mimicSpawnMultiplier;
            AutoScaleBossSpawnsByPlayerCount = autoScaleBossSpawnsByPlayerCount;
            BossSpawnMultiplier = bossSpawnMultiplier;
            AutoScaleJakoSpawnsByPlayerCount = autoScaleJakoSpawnsByPlayerCount;
            JakoSpawnMultiplier = jakoSpawnMultiplier;
            AutoScaleSpecialSpawnsByPlayerCount = autoScaleSpecialSpawnsByPlayerCount;
            SpecialSpawnMultiplier = specialSpawnMultiplier;
            AutoScaleTrapSpawnsByPlayerCount = autoScaleTrapSpawnsByPlayerCount;
            TrapSpawnMultiplier = trapSpawnMultiplier;
            TrapRespawnMode = trapRespawnMode;
            TrapRespawnDelaySeconds = trapRespawnDelaySeconds;
            TrapRespawnDelayMinSeconds = trapRespawnDelayMinSeconds;
            TrapRespawnDelayMaxSeconds = trapRespawnDelayMaxSeconds;
            TrapRespawnMinPlayerDistanceMeters = trapRespawnMinPlayerDistanceMeters;
            AutoScaleOtherSpawnsByPlayerCount = autoScaleOtherSpawnsByPlayerCount;
            OtherSpawnMultiplier = otherSpawnMultiplier;
            AmbientMonsterWaveMode = ambientMonsterWaveMode;
            AmbientMonsterWaveInitialDelaySeconds = ambientMonsterWaveInitialDelaySeconds;
            AmbientMonsterWaveInitialDelayMinSeconds = ambientMonsterWaveInitialDelayMinSeconds;
            AmbientMonsterWaveInitialDelayMaxSeconds = ambientMonsterWaveInitialDelayMaxSeconds;
            AmbientMonsterWaveIntervalSeconds = ambientMonsterWaveIntervalSeconds;
            AmbientMonsterWaveIntervalMinSeconds = ambientMonsterWaveIntervalMinSeconds;
            AmbientMonsterWaveIntervalMaxSeconds = ambientMonsterWaveIntervalMaxSeconds;
            BonusEncounterDelayMinSeconds = bonusEncounterDelayMinSeconds;
            BonusEncounterDelayMaxSeconds = bonusEncounterDelayMaxSeconds;
            BonusEncounterMinPlayerDistanceMeters = bonusEncounterMinPlayerDistanceMeters;
        }

        internal bool EnableSpawnScaling { get; }

        internal float SpawnScalingPlayerCountScaleRate { get; }

        internal bool AutoScaleMimicSpawnsByPlayerCount { get; }

        internal float MimicSpawnMultiplier { get; }

        internal bool AutoScaleBossSpawnsByPlayerCount { get; }

        internal float BossSpawnMultiplier { get; }

        internal bool AutoScaleJakoSpawnsByPlayerCount { get; }

        internal float JakoSpawnMultiplier { get; }

        internal bool AutoScaleSpecialSpawnsByPlayerCount { get; }

        internal float SpecialSpawnMultiplier { get; }

        internal bool AutoScaleTrapSpawnsByPlayerCount { get; }

        internal float TrapSpawnMultiplier { get; }

        internal string TrapRespawnMode { get; }

        internal float TrapRespawnDelaySeconds { get; }

        internal float TrapRespawnDelayMinSeconds { get; }

        internal float TrapRespawnDelayMaxSeconds { get; }

        internal float TrapRespawnMinPlayerDistanceMeters { get; }

        internal bool AutoScaleOtherSpawnsByPlayerCount { get; }

        internal float OtherSpawnMultiplier { get; }

        internal string AmbientMonsterWaveMode { get; }

        internal float AmbientMonsterWaveInitialDelaySeconds { get; }

        internal float AmbientMonsterWaveInitialDelayMinSeconds { get; }

        internal float AmbientMonsterWaveInitialDelayMaxSeconds { get; }

        internal float AmbientMonsterWaveIntervalSeconds { get; }

        internal float AmbientMonsterWaveIntervalMinSeconds { get; }

        internal float AmbientMonsterWaveIntervalMaxSeconds { get; }

        internal float BonusEncounterDelayMinSeconds { get; }

        internal float BonusEncounterDelayMaxSeconds { get; }

        internal float BonusEncounterMinPlayerDistanceMeters { get; }

        internal static SpawnScalingSceneConfig CaptureFromModConfig()
        {
            return new SpawnScalingSceneConfig(
                ModConfig.EnableSpawnScaling.Value,
                ModConfig.SpawnScalingPlayerCountScaleRate.Value,
                ModConfig.AutoScaleMimicSpawnsByPlayerCount.Value,
                ModConfig.MimicSpawnMultiplier.Value,
                ModConfig.AutoScaleBossSpawnsByPlayerCount.Value,
                ModConfig.BossSpawnMultiplier.Value,
                ModConfig.AutoScaleJakoSpawnsByPlayerCount.Value,
                ModConfig.JakoSpawnMultiplier.Value,
                ModConfig.AutoScaleSpecialSpawnsByPlayerCount.Value,
                ModConfig.SpecialSpawnMultiplier.Value,
                ModConfig.AutoScaleTrapSpawnsByPlayerCount.Value,
                ModConfig.TrapSpawnMultiplier.Value,
                ModConfig.TrapRespawnMode.Value ?? "",
                ModConfig.TrapRespawnDelaySeconds.Value,
                ModConfig.TrapRespawnDelayMinSeconds.Value,
                ModConfig.TrapRespawnDelayMaxSeconds.Value,
                ModConfig.TrapRespawnMinPlayerDistanceMeters.Value,
                ModConfig.AutoScaleOtherSpawnsByPlayerCount.Value,
                ModConfig.OtherSpawnMultiplier.Value,
                ModConfig.AmbientMonsterWaveMode.Value ?? "",
                ModConfig.AmbientMonsterWaveInitialDelaySeconds.Value,
                ModConfig.AmbientMonsterWaveInitialDelayMinSeconds.Value,
                ModConfig.AmbientMonsterWaveInitialDelayMaxSeconds.Value,
                ModConfig.AmbientMonsterWaveIntervalSeconds.Value,
                ModConfig.AmbientMonsterWaveIntervalMinSeconds.Value,
                ModConfig.AmbientMonsterWaveIntervalMaxSeconds.Value,
                ModConfig.BonusEncounterDelayMinSeconds.Value,
                ModConfig.BonusEncounterDelayMaxSeconds.Value,
                ModConfig.BonusEncounterMinPlayerDistanceMeters.Value);
        }
    }

    internal readonly struct EconomySceneConfig
    {
        internal EconomySceneConfig(
            bool enableEconomy,
            float economyPlayerCountScaleRate,
            bool autoScaleScrapSellValueByPlayerCount,
            float scrapSellValueMultiplier,
            bool autoScaleShopBuyPriceByPlayerCount,
            float shopBuyPriceMultiplier,
            int shopDiscountMinPercent,
            int shopDiscountMaxPercent,
            int shopDiscountChancePercent,
            bool autoScaleReinforcePriceByPlayerCount,
            float reinforcePriceMultiplier,
            bool retainUnspentCurrencyBetweenCycles)
        {
            EnableEconomy = enableEconomy;
            EconomyPlayerCountScaleRate = economyPlayerCountScaleRate;
            AutoScaleScrapSellValueByPlayerCount = autoScaleScrapSellValueByPlayerCount;
            ScrapSellValueMultiplier = scrapSellValueMultiplier;
            AutoScaleShopBuyPriceByPlayerCount = autoScaleShopBuyPriceByPlayerCount;
            ShopBuyPriceMultiplier = shopBuyPriceMultiplier;
            ShopDiscountMinPercent = shopDiscountMinPercent;
            ShopDiscountMaxPercent = shopDiscountMaxPercent;
            ShopDiscountChancePercent = shopDiscountChancePercent;
            AutoScaleReinforcePriceByPlayerCount = autoScaleReinforcePriceByPlayerCount;
            ReinforcePriceMultiplier = reinforcePriceMultiplier;
            RetainUnspentCurrencyBetweenCycles = retainUnspentCurrencyBetweenCycles;
        }

        internal bool EnableEconomy { get; }

        internal float EconomyPlayerCountScaleRate { get; }

        internal bool AutoScaleScrapSellValueByPlayerCount { get; }

        internal float ScrapSellValueMultiplier { get; }

        internal bool AutoScaleShopBuyPriceByPlayerCount { get; }

        internal float ShopBuyPriceMultiplier { get; }

        internal int ShopDiscountMinPercent { get; }

        internal int ShopDiscountMaxPercent { get; }

        internal int ShopDiscountChancePercent { get; }

        internal bool AutoScaleReinforcePriceByPlayerCount { get; }

        internal float ReinforcePriceMultiplier { get; }

        internal bool RetainUnspentCurrencyBetweenCycles { get; }

        internal static EconomySceneConfig CaptureFromModConfig()
        {
            return new EconomySceneConfig(
                ModConfig.EnableEconomy.Value,
                ModConfig.EconomyPlayerCountScaleRate.Value,
                ModConfig.AutoScaleScrapSellValueByPlayerCount.Value,
                ModConfig.ScrapSellValueMultiplier.Value,
                ModConfig.AutoScaleShopBuyPriceByPlayerCount.Value,
                ModConfig.ShopBuyPriceMultiplier.Value,
                ModConfig.ShopDiscountMinPercent.Value,
                ModConfig.ShopDiscountMaxPercent.Value,
                ModConfig.ShopDiscountChancePercent.Value,
                ModConfig.AutoScaleReinforcePriceByPlayerCount.Value,
                ModConfig.ReinforcePriceMultiplier.Value,
                ModConfig.RetainUnspentCurrencyBetweenCycles.Value);
        }
    }

    internal readonly struct DungeonTimeSceneConfig
    {
        internal DungeonTimeSceneConfig(
            bool enableDungeonTime,
            int dungeonTimeBaselinePlayerCount,
            float extraShiftSecondsPerPlayerAboveBaseline)
        {
            EnableDungeonTime = enableDungeonTime;
            DungeonTimeBaselinePlayerCount = dungeonTimeBaselinePlayerCount;
            ExtraShiftSecondsPerPlayerAboveBaseline = extraShiftSecondsPerPlayerAboveBaseline;
        }

        internal bool EnableDungeonTime { get; }

        internal int DungeonTimeBaselinePlayerCount { get; }

        internal float ExtraShiftSecondsPerPlayerAboveBaseline { get; }

        internal static DungeonTimeSceneConfig CaptureFromModConfig()
        {
            return new DungeonTimeSceneConfig(
                ModConfig.EnableDungeonTime.Value,
                ModConfig.DungeonTimeBaselinePlayerCount.Value,
                ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.Value);
        }
    }

    internal readonly struct DungeonRandomizerSceneConfig
    {
        internal DungeonRandomizerSceneConfig(
            bool enableDungeonRandomizer,
            bool randomizeDungeonPick,
            string dungeonPickPoolMode,
            string dungeonAllowlist,
            string dungeonBlocklist,
            bool ignoreDungeonExcludeList,
            bool randomizeMapVariant,
            DungeonSeedFlavor seedFlavor)
        {
            EnableDungeonRandomizer = enableDungeonRandomizer;
            RandomizeDungeonPick = randomizeDungeonPick;
            DungeonPickPoolMode = dungeonPickPoolMode;
            DungeonAllowlist = dungeonAllowlist;
            DungeonBlocklist = dungeonBlocklist;
            IgnoreDungeonExcludeList = ignoreDungeonExcludeList;
            RandomizeMapVariant = randomizeMapVariant;
            SeedFlavor = seedFlavor;
        }

        internal bool EnableDungeonRandomizer { get; }

        internal bool RandomizeDungeonPick { get; }

        internal string DungeonPickPoolMode { get; }

        internal string DungeonAllowlist { get; }

        internal string DungeonBlocklist { get; }

        internal bool IgnoreDungeonExcludeList { get; }

        internal bool RandomizeMapVariant { get; }

        internal DungeonSeedFlavor SeedFlavor { get; }

        internal static DungeonRandomizerSceneConfig CaptureFromModConfig()
        {
            string dungeonSeedFlavor = ModConfig.DungeonSeedFlavor.Value ?? "Vanilla";
            if (!DungeonSeedFlavorUtil.TryParse(dungeonSeedFlavor, out DungeonSeedFlavor seedFlavor))
            {
                seedFlavor = DungeonSeedFlavor.Vanilla;
            }

            return new DungeonRandomizerSceneConfig(
                ModConfig.EnableDungeonRandomizer.Value,
                ModConfig.RandomizeDungeonPick.Value,
                ModConfig.DungeonPickPoolMode.Value ?? "",
                ModConfig.DungeonAllowlist.Value ?? "",
                ModConfig.DungeonBlocklist.Value ?? "",
                ModConfig.IgnoreDungeonExcludeList.Value,
                ModConfig.RandomizeMapVariant.Value,
                seedFlavor);
        }
    }
}
