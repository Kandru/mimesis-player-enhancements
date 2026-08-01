namespace MimesisPlayerEnhancement.Util
{
    internal readonly struct LootMultiplicatorSceneConfig
    {
        internal LootMultiplicatorSceneConfig(
            bool enableLootMultiplicator,
            int lootMultiplicatorBaselinePlayerCount,
            float mapLootMultiplier,
            float mapLootPerPlayerMultiplier,
            float dropLootMultiplier,
            float dropLootPerPlayerMultiplier,
            string lootItemFilterMode,
            string lootAllowlist,
            string lootBlocklist,
            bool autoScaleMapLootBudgetForFilter,
            int convertFakeActorDyingDropChancePercent)
        {
            EnableLootMultiplicator = enableLootMultiplicator;
            LootMultiplicatorBaselinePlayerCount = lootMultiplicatorBaselinePlayerCount;
            MapLootMultiplier = mapLootMultiplier;
            MapLootPerPlayerMultiplier = mapLootPerPlayerMultiplier;
            DropLootMultiplier = dropLootMultiplier;
            DropLootPerPlayerMultiplier = dropLootPerPlayerMultiplier;
            LootItemFilterMode = lootItemFilterMode;
            LootAllowlist = lootAllowlist;
            LootBlocklist = lootBlocklist;
            AutoScaleMapLootBudgetForFilter = autoScaleMapLootBudgetForFilter;
            ConvertFakeActorDyingDropChancePercent = convertFakeActorDyingDropChancePercent;
        }

        internal bool EnableLootMultiplicator { get; }

        internal int LootMultiplicatorBaselinePlayerCount { get; }

        internal float MapLootMultiplier { get; }

        internal float MapLootPerPlayerMultiplier { get; }

        internal float DropLootMultiplier { get; }

        internal float DropLootPerPlayerMultiplier { get; }

        internal string LootItemFilterMode { get; }

        internal string LootAllowlist { get; }

        internal string LootBlocklist { get; }

        internal bool AutoScaleMapLootBudgetForFilter { get; }

        internal int ConvertFakeActorDyingDropChancePercent { get; }

        internal static LootMultiplicatorSceneConfig CaptureFromModConfig()
        {
            return new LootMultiplicatorSceneConfig(
                ModConfig.EnableLootMultiplicator.Value,
                ModConfig.LootMultiplicatorBaselinePlayerCount.Value,
                ModConfig.MapLootMultiplier.Value,
                ModConfig.MapLootPerPlayerMultiplier.Value,
                ModConfig.DropLootMultiplier.Value,
                ModConfig.DropLootPerPlayerMultiplier.Value,
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
            int spawnScalingBaselinePlayerCount,
            float mimicSpawnMultiplier,
            float mimicSpawnPerPlayerMultiplier,
            float bossSpawnMultiplier,
            float bossSpawnPerPlayerMultiplier,
            float jakoSpawnMultiplier,
            float jakoSpawnPerPlayerMultiplier,
            float specialSpawnMultiplier,
            float specialSpawnPerPlayerMultiplier,
            float trapSpawnMultiplier,
            float trapSpawnPerPlayerMultiplier,
            string trapRespawnMode,
            float trapRespawnDelaySeconds,
            float trapRespawnDelayMinSeconds,
            float trapRespawnDelayMaxSeconds,
            float trapRespawnMinPlayerDistanceMeters,
            float otherSpawnMultiplier,
            float otherSpawnPerPlayerMultiplier,
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
            SpawnScalingBaselinePlayerCount = spawnScalingBaselinePlayerCount;
            MimicSpawnMultiplier = mimicSpawnMultiplier;
            MimicSpawnPerPlayerMultiplier = mimicSpawnPerPlayerMultiplier;
            BossSpawnMultiplier = bossSpawnMultiplier;
            BossSpawnPerPlayerMultiplier = bossSpawnPerPlayerMultiplier;
            JakoSpawnMultiplier = jakoSpawnMultiplier;
            JakoSpawnPerPlayerMultiplier = jakoSpawnPerPlayerMultiplier;
            SpecialSpawnMultiplier = specialSpawnMultiplier;
            SpecialSpawnPerPlayerMultiplier = specialSpawnPerPlayerMultiplier;
            TrapSpawnMultiplier = trapSpawnMultiplier;
            TrapSpawnPerPlayerMultiplier = trapSpawnPerPlayerMultiplier;
            TrapRespawnMode = trapRespawnMode;
            TrapRespawnDelaySeconds = trapRespawnDelaySeconds;
            TrapRespawnDelayMinSeconds = trapRespawnDelayMinSeconds;
            TrapRespawnDelayMaxSeconds = trapRespawnDelayMaxSeconds;
            TrapRespawnMinPlayerDistanceMeters = trapRespawnMinPlayerDistanceMeters;
            OtherSpawnMultiplier = otherSpawnMultiplier;
            OtherSpawnPerPlayerMultiplier = otherSpawnPerPlayerMultiplier;
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

        internal int SpawnScalingBaselinePlayerCount { get; }

        internal float MimicSpawnMultiplier { get; }

        internal float MimicSpawnPerPlayerMultiplier { get; }

        internal float BossSpawnMultiplier { get; }

        internal float BossSpawnPerPlayerMultiplier { get; }

        internal float JakoSpawnMultiplier { get; }

        internal float JakoSpawnPerPlayerMultiplier { get; }

        internal float SpecialSpawnMultiplier { get; }

        internal float SpecialSpawnPerPlayerMultiplier { get; }

        internal float TrapSpawnMultiplier { get; }

        internal float TrapSpawnPerPlayerMultiplier { get; }

        internal string TrapRespawnMode { get; }

        internal float TrapRespawnDelaySeconds { get; }

        internal float TrapRespawnDelayMinSeconds { get; }

        internal float TrapRespawnDelayMaxSeconds { get; }

        internal float TrapRespawnMinPlayerDistanceMeters { get; }

        internal float OtherSpawnMultiplier { get; }

        internal float OtherSpawnPerPlayerMultiplier { get; }

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
                ModConfig.SpawnScalingBaselinePlayerCount.Value,
                ModConfig.MimicSpawnMultiplier.Value,
                ModConfig.MimicSpawnPerPlayerMultiplier.Value,
                ModConfig.BossSpawnMultiplier.Value,
                ModConfig.BossSpawnPerPlayerMultiplier.Value,
                ModConfig.JakoSpawnMultiplier.Value,
                ModConfig.JakoSpawnPerPlayerMultiplier.Value,
                ModConfig.SpecialSpawnMultiplier.Value,
                ModConfig.SpecialSpawnPerPlayerMultiplier.Value,
                ModConfig.TrapSpawnMultiplier.Value,
                ModConfig.TrapSpawnPerPlayerMultiplier.Value,
                ModConfig.TrapRespawnMode.Value ?? "",
                ModConfig.TrapRespawnDelaySeconds.Value,
                ModConfig.TrapRespawnDelayMinSeconds.Value,
                ModConfig.TrapRespawnDelayMaxSeconds.Value,
                ModConfig.TrapRespawnMinPlayerDistanceMeters.Value,
                ModConfig.OtherSpawnMultiplier.Value,
                ModConfig.OtherSpawnPerPlayerMultiplier.Value,
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
            int economyBaselinePlayerCount,
            float scrapSellValueMultiplier,
            float scrapSellValuePerPlayerMultiplier,
            float shopBuyPriceMultiplier,
            float shopBuyPricePerPlayerMultiplier,
            int shopDiscountMinPercent,
            int shopDiscountMaxPercent,
            int shopDiscountChancePercent,
            float reinforcePriceMultiplier,
            float reinforcePricePerPlayerMultiplier,
            bool retainUnspentCurrencyBetweenCycles)
        {
            EnableEconomy = enableEconomy;
            EconomyBaselinePlayerCount = economyBaselinePlayerCount;
            ScrapSellValueMultiplier = scrapSellValueMultiplier;
            ScrapSellValuePerPlayerMultiplier = scrapSellValuePerPlayerMultiplier;
            ShopBuyPriceMultiplier = shopBuyPriceMultiplier;
            ShopBuyPricePerPlayerMultiplier = shopBuyPricePerPlayerMultiplier;
            ShopDiscountMinPercent = shopDiscountMinPercent;
            ShopDiscountMaxPercent = shopDiscountMaxPercent;
            ShopDiscountChancePercent = shopDiscountChancePercent;
            ReinforcePriceMultiplier = reinforcePriceMultiplier;
            ReinforcePricePerPlayerMultiplier = reinforcePricePerPlayerMultiplier;
            RetainUnspentCurrencyBetweenCycles = retainUnspentCurrencyBetweenCycles;
        }

        internal bool EnableEconomy { get; }

        internal int EconomyBaselinePlayerCount { get; }

        internal float ScrapSellValueMultiplier { get; }

        internal float ScrapSellValuePerPlayerMultiplier { get; }

        internal float ShopBuyPriceMultiplier { get; }

        internal float ShopBuyPricePerPlayerMultiplier { get; }

        internal int ShopDiscountMinPercent { get; }

        internal int ShopDiscountMaxPercent { get; }

        internal int ShopDiscountChancePercent { get; }

        internal float ReinforcePriceMultiplier { get; }

        internal float ReinforcePricePerPlayerMultiplier { get; }

        internal bool RetainUnspentCurrencyBetweenCycles { get; }

        internal static EconomySceneConfig CaptureFromModConfig()
        {
            return new EconomySceneConfig(
                ModConfig.EnableEconomy.Value,
                ModConfig.EconomyBaselinePlayerCount.Value,
                ModConfig.ScrapSellValueMultiplier.Value,
                ModConfig.ScrapSellValuePerPlayerMultiplier.Value,
                ModConfig.ShopBuyPriceMultiplier.Value,
                ModConfig.ShopBuyPricePerPlayerMultiplier.Value,
                ModConfig.ShopDiscountMinPercent.Value,
                ModConfig.ShopDiscountMaxPercent.Value,
                ModConfig.ShopDiscountChancePercent.Value,
                ModConfig.ReinforcePriceMultiplier.Value,
                ModConfig.ReinforcePricePerPlayerMultiplier.Value,
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
