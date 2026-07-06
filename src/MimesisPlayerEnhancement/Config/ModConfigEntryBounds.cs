using System;
using System.Globalization;
using MimesisPlayerEnhancement.Features.PlayerTuning;

namespace MimesisPlayerEnhancement
{
    internal readonly struct ModConfigEntryBound
    {
        internal ModConfigEntryBound(string? minValue, string? maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        internal string? MinValue { get; }
        internal string? MaxValue { get; }
    }

    /// <summary>
    /// Web-dashboard min/max metadata for numeric config entries. Bounds mirror feature WireValidation handlers.
    /// </summary>
    internal static class ModConfigEntryBounds
    {
        private static readonly Dictionary<(string SectionId, string Key), ModConfigEntryBound> Bounds =
            new(SectionKeyComparer.Instance);

        static ModConfigEntryBounds()
        {
            const string Main = ModConfigRegistry.MainSectionId;
            const string MorePlayers = "MimesisPlayerEnhancement_MorePlayers";
            const string MoreVoices = "MimesisPlayerEnhancement_MoreVoices";
            const string Statistics = "MimesisPlayerEnhancement_Statistics";
            const string PlayerTuning = "MimesisPlayerEnhancement_PlayerTuning";
            const string Economy = "MimesisPlayerEnhancement_Economy";
            const string LootMultiplicator = "MimesisPlayerEnhancement_LootMultiplicator";
            const string SpawnScaling = "MimesisPlayerEnhancement_SpawnScaling";
            const string JoinAnytime = "MimesisPlayerEnhancement_JoinAnytime";
            const string DungeonTime = "MimesisPlayerEnhancement_DungeonTime";
            const string Weather = "MimesisPlayerEnhancement_Weather";
            const string DeadPlayerFeatures = "MimesisPlayerEnhancement_DeadPlayerFeatures";

            Float(Main, "ModToastDurationSeconds", 1f);

            Int(MorePlayers, "MaxPlayers", 1);
            Int(MoreVoices, "MaxIndoorVoiceEvents", 1);
            Int(MoreVoices, "MaxDeathMatchVoiceEvents", 1);
            Int(MoreVoices, "MaxOutdoorVoiceEvents", 1);
            Int(Statistics, "SessionReconnectGraceMinutes", 1);

            FloatRange(PlayerTuning, "MoveSpeedMultiplier");
            FloatRange(PlayerTuning, "MaxStaminaMultiplier");
            FloatRange(PlayerTuning, "StaminaDrainMultiplier");
            FloatRange(PlayerTuning, "StaminaRegenMultiplier");
            FloatRange(PlayerTuning, "StaminaRegenDelayMultiplier");
            FloatRange(PlayerTuning, "MaxCarryWeightMultiplier");

            MinZeroFloat(Economy,
                "EconomyPlayerCountScaleRate",
                "StartupMoneyMultiplier",
                "RoundGoalMoneyMultiplier",
                "ScrapSellValueMultiplier",
                "ShopBuyPriceMultiplier",
                "ReinforcePriceMultiplier");
            IntRange(Economy, "ShopDiscountMinPercent", 0, 100);
            IntRange(Economy, "ShopDiscountMaxPercent", 0, 100);
            IntRange(Economy, "ShopDiscountChancePercent", 0, 100);

            MinZeroFloat(LootMultiplicator,
                "LootMultiplicatorPlayerCountScaleRate",
                "MapLootMultiplier",
                "DropLootMultiplier");
            IntRange(LootMultiplicator, "ConvertFakeActorDyingDropChancePercent", 0, 100);

            MinZeroFloat(SpawnScaling,
                "SpawnScalingPlayerCountScaleRate",
                "MimicSpawnMultiplier",
                "BossSpawnMultiplier",
                "JakoSpawnMultiplier",
                "SpecialSpawnMultiplier",
                "TrapSpawnMultiplier",
                "OtherSpawnMultiplier",
                "MapPlacedEncounterDelayMinSeconds",
                "MapPlacedEncounterDelayMaxSeconds",
                "MapPlacedEncounterMinPlayerDistanceMeters");

            Int(JoinAnytime, "JoinConnectionGraceSeconds", 1);

            Int(DungeonTime, "DungeonTimeBaselinePlayerCount", 1);
            Float(DungeonTime, "ExtraShiftSecondsPerPlayerAboveBaseline", 0f);

            Float(Weather, "WeatherCycleMinDelaySeconds", 0f);
            Float(Weather, "WeatherCycleMaxDelaySeconds", 0f);

            FloatRange(DeadPlayerFeatures, "MimicPossessionMinTimeSeconds",
                MimicPossessionResolver.MinDurationSeconds,
                MimicPossessionResolver.MaxDurationSeconds);
            FloatRange(DeadPlayerFeatures, "MimicPossessionMaxTimeSeconds",
                MimicPossessionResolver.MinDurationSeconds,
                MimicPossessionResolver.MaxDurationSeconds);
            FloatRange(DeadPlayerFeatures, "MimicPossessionCooltimeMultiplier",
                MimicPossessionResolver.MinCooltimeMultiplier,
                MimicPossessionResolver.MaxCooltimeMultiplier);
        }

        internal static bool TryGet(string sectionId, string key, out ModConfigEntryBound bound)
        {
            return Bounds.TryGetValue((sectionId, key), out bound);
        }

        private static void Float(string sectionId, string key, float min, float? max = null)
        {
            Bounds[(sectionId, key)] = new ModConfigEntryBound(
                ModConfigFloatHelper.Format(min),
                max.HasValue ? ModConfigFloatHelper.Format(max.Value) : null);
        }

        private static void FloatRange(string sectionId, string key, float min, float max)
        {
            Float(sectionId, key, min, max);
        }

        private static void FloatRange(string sectionId, string key)
        {
            FloatRange(sectionId, key, PlayerTuningResolver.MinMultiplier, PlayerTuningResolver.MaxMultiplier);
        }

        private static void Int(string sectionId, string key, int min, int? max = null)
        {
            Bounds[(sectionId, key)] = new ModConfigEntryBound(
                min.ToString(CultureInfo.InvariantCulture),
                max?.ToString(CultureInfo.InvariantCulture));
        }

        private static void IntRange(string sectionId, string key, int min, int max)
        {
            Int(sectionId, key, min, max);
        }

        private static void MinZeroFloat(string sectionId, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Float(sectionId, keys[i], 0f);
            }
        }

        private sealed class SectionKeyComparer : IEqualityComparer<(string SectionId, string Key)>
        {
            internal static readonly SectionKeyComparer Instance = new();

            public bool Equals((string SectionId, string Key) x, (string SectionId, string Key) y)
            {
                return string.Equals(x.SectionId, y.SectionId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string SectionId, string Key) obj)
            {
                return HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SectionId),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key));
            }
        }
    }
}
