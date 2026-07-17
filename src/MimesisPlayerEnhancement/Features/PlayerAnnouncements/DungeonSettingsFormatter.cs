namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class DungeonSettingsFormatter
    {
        internal static string? FormatForDungeonEntry(DungeonRoom room)
        {
            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            List<string> parts = [];

            if (playerCount > ScalingMath.VanillaPlayerBaseline)
            {
                parts.Add(ModL10n.Get("announce.players_count", new Dictionary<string, object> { ["count"] = playerCount }));
            }

            AppendSpawnSummary(parts, playerCount);
            AppendLootSummary(parts, playerCount);
            AppendMoneySummary(parts, playerCount);
            AppendRoundGoalSummary(parts);
            AppendDungeonTime(parts, playerCount);
            AppendDungeonRandomizer(parts);
            AppendWeather(parts);

            return parts.Count == 0
                ? null
                : ModL10n.Get("announce.dungeon_run_prefix", new Dictionary<string, object>
                {
                    ["summary"] = string.Join(ModL10n.Get("stats.list_separator"), parts),
                });
        }

        private static void AppendSpawnSummary(List<string> parts, int playerCount)
        {
            if (!SceneScopedConfigGate.Spawn.EnableSpawnScaling)
            {
                return;
            }

            AppendMultiplier(parts, ModL10n.Get("announce.boss_spawns"), SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Boss, playerCount));
            AppendMultiplier(parts, ModL10n.Get("announce.special_spawns"), SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Special, playerCount));
            AppendMultiplier(parts, ModL10n.Get("announce.monster_spawns"), SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount));
        }

        private static void AppendLootSummary(List<string> parts, int playerCount)
        {
            if (!SceneScopedConfigGate.Loot.EnableLootMultiplicator)
            {
                return;
            }

            AppendMultiplier(
                parts,
                ModL10n.Get("announce.map_loot"),
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, ItemType.Consumable, playerCount));

            AppendMultiplier(
                parts,
                ModL10n.Get("announce.drop_loot"),
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Drop, ItemType.Consumable, playerCount));
        }

        private static void AppendMoneySummary(List<string> parts, int playerCount)
        {
            if (!SceneScopedConfigGate.Economy.EnableEconomy)
            {
                return;
            }

            AppendMultiplier(
                parts,
                ModL10n.Get("announce.scrap_value"),
                EconomyResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount));
        }

        private static void AppendRoundGoalSummary(List<string> parts)
        {
            if (!RoundGoalScalingResolver.ShouldApply())
            {
                return;
            }

            if (IsDefaultMultiplier(ModConfig.RoundGoalMoneyMultiplier.Value))
            {
                return;
            }

            parts.Add(ModL10n.Get("announce.multiplier_prefix", new Dictionary<string, object>
            {
                ["label"] = ModL10n.Get("announce.quota"),
                ["multiplier"] = FormatMultiplier(ModConfig.RoundGoalMoneyMultiplier.Value),
            }));
        }

        private static void AppendDungeonTime(List<string> parts, int playerCount)
        {
            DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
            if (!config.EnableDungeonTime)
            {
                return;
            }

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount, config);
            if (bonusSeconds <= 0d)
            {
                return;
            }

            parts.Add(ModL10n.Get("announce.shift_time_bonus", new Dictionary<string, object>
            {
                ["seconds"] = FormatBonusSeconds(bonusSeconds),
            }));
        }

        private static void AppendDungeonRandomizer(List<string> parts)
        {
            if (!SceneScopedConfigGate.DungeonRandomizer.EnableDungeonRandomizer)
            {
                return;
            }

            parts.Add(ModL10n.Get("announce.dungeon_randomizer_on"));
        }

        private static void AppendWeather(List<string> parts)
        {
            if (!ModConfig.EnableWeather.Value)
            {
                return;
            }

            WeatherMode mode = WeatherResolver.GetMode();
            if (mode == WeatherMode.Fixed)
            {
                parts.Add(ModL10n.Get("announce.weather_fixed", new Dictionary<string, object>
                {
                    ["preset"] = ModConfig.FixedWeatherPreset.Value ?? "Sunny",
                }));
            }
            else if (mode == WeatherMode.Cycle)
            {
                parts.Add(ModL10n.Get("announce.weather_cycle"));
            }

            StartTimePreset startTime = WeatherTimeResolver.ParseStartTimePreset(ModConfig.StartTimePreset.Value);
            if (startTime != StartTimePreset.Vanilla)
            {
                parts.Add(ModL10n.Get("announce.start_time_preset", new Dictionary<string, object>
                {
                    ["preset"] = startTime.ToString(),
                }));
            }
        }

        private static void AppendMultiplier(List<string> parts, string label, float multiplier)
        {
            if (IsDefaultMultiplier(multiplier))
            {
                return;
            }

            parts.Add(ModL10n.Get("announce.multiplier_prefix", new Dictionary<string, object>
            {
                ["label"] = label,
                ["multiplier"] = FormatMultiplier(multiplier),
            }));
        }

        private static bool IsDefaultMultiplier(float multiplier)
        {
            return multiplier is >= 0.995f and <= 1.005f;
        }

        private static string FormatMultiplier(float multiplier)
        {
            return $"×{multiplier:0.##}";
        }

        private static string FormatBonusSeconds(double bonusSeconds)
        {
            return bonusSeconds.ToString("0.##");
        }
    }
}
