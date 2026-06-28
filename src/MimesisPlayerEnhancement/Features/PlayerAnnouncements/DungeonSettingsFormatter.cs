using System.Collections.Generic;
using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Features.DungeonSizeScaling;
using MimesisPlayerEnhancement.Features.DungeonTime;
using MimesisPlayerEnhancement.Features.LootMultiplicator;
using MimesisPlayerEnhancement.Features.MoneyMultiplier;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Util;

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
                parts.Add($"{playerCount} players");
            }

            AppendSpawnSummary(parts, playerCount);
            AppendLootSummary(parts, playerCount);
            AppendMoneySummary(parts, playerCount);
            AppendDungeonTime(parts, playerCount);
            AppendDungeonSize(parts, playerCount);
            AppendDungeonRandomizer(parts);

            return parts.Count == 0 ? null : $"This run: {string.Join(", ", parts)}.";
        }

        private static void AppendSpawnSummary(List<string> parts, int playerCount)
        {
            if (!ModConfig.EnableSpawnScaling.Value)
            {
                return;
            }

            AppendMultiplier(parts, "boss spawns", SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Boss, playerCount));
            AppendMultiplier(parts, "special spawns", SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Special, playerCount));
            AppendMultiplier(parts, "monster spawns", SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount));
        }

        private static void AppendLootSummary(List<string> parts, int playerCount)
        {
            if (!ModConfig.EnableLootMultiplicator.Value)
            {
                return;
            }

            float mapLoot = AverageNonDefaultMultiplier(
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, ItemType.Consumable, playerCount),
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, ItemType.Equipment, playerCount),
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, ItemType.Miscellany, playerCount));
            AppendMultiplier(parts, "map loot", mapLoot);

            float dropLoot = AverageNonDefaultMultiplier(
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Drop, ItemType.Consumable, playerCount),
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Drop, ItemType.Equipment, playerCount),
                LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Drop, ItemType.Miscellany, playerCount));
            AppendMultiplier(parts, "drop loot", dropLoot);
        }

        private static void AppendMoneySummary(List<string> parts, int playerCount)
        {
            if (!ModConfig.EnableMoneyMultiplier.Value)
            {
                return;
            }

            AppendMultiplier(
                parts,
                "quota",
                MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.RoundGoal, playerCount));
            AppendMultiplier(
                parts,
                "scrap value",
                MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount));
        }

        private static void AppendDungeonTime(List<string> parts, int playerCount)
        {
            if (!ModConfig.EnableDungeonTime.Value)
            {
                return;
            }

            double bonusSeconds = DungeonTimeResolver.GetBonusSeconds(playerCount);
            if (bonusSeconds <= 0d)
            {
                return;
            }

            parts.Add($"+{(int)bonusSeconds}s shift time");
        }

        private static void AppendDungeonSize(List<string> parts, int playerCount)
        {
            if (!ModConfig.EnableDungeonSizeScaling.Value)
            {
                return;
            }

            float multiplier = DungeonSizeScalingResolver.GetLengthMultiplier(playerCount);
            AppendMultiplier(parts, "dungeon size", multiplier);
        }

        private static void AppendDungeonRandomizer(List<string> parts)
        {
            if (!ModConfig.EnableDungeonRandomizer.Value)
            {
                return;
            }

            parts.Add("dungeon randomizer on");
        }

        private static void AppendMultiplier(List<string> parts, string label, float multiplier)
        {
            if (IsDefaultMultiplier(multiplier))
            {
                return;
            }

            parts.Add($"{label} {FormatMultiplier(multiplier)}");
        }

        private static float AverageNonDefaultMultiplier(params float[] multipliers)
        {
            float sum = 0f;
            int count = 0;
            foreach (float multiplier in multipliers)
            {
                if (IsDefaultMultiplier(multiplier))
                {
                    continue;
                }

                sum += multiplier;
                count++;
            }

            return count == 0 ? 1f : sum / count;
        }

        private static bool IsDefaultMultiplier(float multiplier)
        {
            return multiplier is >= 0.995f and <= 1.005f;
        }

        private static string FormatMultiplier(float multiplier)
        {
            return $"×{multiplier:0.##}";
        }
    }
}
