using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.UserInterface;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement
{
    internal readonly struct ModConfigEntryDependency
    {
        internal ModConfigEntryDependency(string dependsOnKey, string? dependsOnValue = null)
        {
            DependsOnKey = dependsOnKey;
            DependsOnValue = dependsOnValue;
        }

        internal string DependsOnKey { get; }
        internal string? DependsOnValue { get; }
    }

    /// <summary>
    /// Dashboard visibility rules mirroring backend resolver gating.
    /// </summary>
    internal static class ModConfigEntryDependencies
    {
        internal static void ApplyToEntry(WebDashboardConfigSectionDto section, WebDashboardConfigEntryDto entry)
        {
            if (!TryGetDependency(section.Id, entry.Key, out ModConfigEntryDependency dependency))
            {
                return;
            }

            entry.DependsOnKey = dependency.DependsOnKey;
            entry.DependsOnValue = dependency.DependsOnValue;
        }

        private static bool TryGetDependency(string sectionId, string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (sectionId == MimicTuningConfig.SectionId)
            {
                return TryGetMimicTuningDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_SpawnScaling")
            {
                return TryGetSpawnScalingDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_LootMultiplicator")
            {
                return TryGetLootDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_Economy")
            {
                return TryGetEconomyDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_DungeonRandomizer")
            {
                return TryGetDungeonRandomizerDependency(key, out dependency);
            }

            if (sectionId == "MimesisPlayerEnhancement_Weather")
            {
                return TryGetWeatherDependency(key, out dependency);
            }

            if (sectionId == UiConfig.SectionId)
            {
                return TryGetUiDependency(key, out dependency);
            }

            return false;
        }

        private static bool TryGetUiDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            if (string.Equals(key, "RoundStartSoundVariant", StringComparison.Ordinal))
            {
                dependency = new ModConfigEntryDependency("RoundStartSoundMode", "Specific");
                return true;
            }

            if (string.Equals(key, "RoundStartSoundRandomPool", StringComparison.Ordinal))
            {
                dependency = new ModConfigEntryDependency("RoundStartSoundMode", "Random");
                return true;
            }

            if (string.Equals(key, "CustomLoadingScreenVariant", StringComparison.Ordinal))
            {
                dependency = new ModConfigEntryDependency("CustomLoadingScreenMode", "Specific");
                return true;
            }

            if (string.Equals(key, "CustomLoadingScreenRandomPool", StringComparison.Ordinal))
            {
                dependency = new ModConfigEntryDependency("CustomLoadingScreenMode", "Random");
                return true;
            }

            return false;
        }

        private static bool TryGetMimicTuningDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "PeriodicVoiceIntervalMultiplier":
                case "PlayerVoiceResponseChancePercent":
                case "PlayerVoiceResponseCooldownSeconds":
                case "PlayerVoiceResponseDelayMinSeconds":
                case "PlayerVoiceResponseDelayMaxSeconds":
                case "PlayerVoiceResponseMaxDistance":
                case "ClipReuseCooldownSeconds":
                case "DeathMatchClipReuseCooldownSeconds":
                case "SpeakAudienceRangeMeters":
                case "PostReplyIntervalMode":
                case "MinRequiredSpeechClips":
                case "HearOwnVoiceFromMimic":
                case "VoiceInitIntervalMode":
                case "VoicePeriodicIntervalMode":
                case "VoiceDeathMatchIntervalMode":
                    dependency = new ModConfigEntryDependency("MimicVoiceTuningMode", "Custom");
                    return true;
                case "PostReplyIntervalFixedSeconds":
                    dependency = new ModConfigEntryDependency("PostReplyIntervalMode", "Fixed");
                    return true;
                case "PostReplyIntervalMinSeconds":
                case "PostReplyIntervalMaxSeconds":
                    dependency = new ModConfigEntryDependency("PostReplyIntervalMode", "Random");
                    return true;
                case "VoiceInitIntervalMin":
                case "VoiceInitIntervalMax":
                    dependency = new ModConfigEntryDependency("VoiceInitIntervalMode", "Random");
                    return true;
                case "VoicePeriodicIntervalMin":
                case "VoicePeriodicIntervalMax":
                    dependency = new ModConfigEntryDependency("VoicePeriodicIntervalMode", "Random");
                    return true;
                case "VoiceDeathMatchIntervalMin":
                case "VoiceDeathMatchIntervalMax":
                    dependency = new ModConfigEntryDependency("VoiceDeathMatchIntervalMode", "Random");
                    return true;
                case "TrustOutdoorMultiplier":
                case "TrustLookingDelta":
                case "TrustNotLookingDelta":
                case "TrustApproachDelta":
                case "TrustMaintainDelta":
                case "TrustWalkAwayDelta":
                case "TrustSprintAwayDelta":
                case "TrustHitDamageMultiplier":
                case "TrustFriendlyThreshold":
                case "TrustDistrustThreshold":
                case "TrustScoreValueMode":
                case "ChaseActivationDistanceMeters":
                case "ChaseForceRunDistanceMeters":
                    dependency = new ModConfigEntryDependency("MimicTrustMode", "Custom");
                    return true;
                case "TrustInitialFixed":
                    dependency = new ModConfigEntryDependency("TrustScoreValueMode", "Fixed");
                    return true;
                case "TrustInitialRandomMin":
                case "TrustInitialRandomMax":
                    dependency = new ModConfigEntryDependency("TrustScoreValueMode", "Random");
                    return true;
                case "TrustBehaviorFixed":
                    dependency = new ModConfigEntryDependency("TrustScoreValueMode", "Fixed");
                    return true;
                case "TrustBehaviorRandomMin":
                case "TrustBehaviorRandomMax":
                    dependency = new ModConfigEntryDependency("TrustScoreValueMode", "Random");
                    return true;
                case "MimicRunawayChance":
                case "JumpCopyChancePercent":
                case "SlotFollowChangeChancePercent":
                    dependency = new ModConfigEntryDependency("MimicSocialMode", "Custom");
                    return true;
                case "MimicInventoryCopyPickRule":
                    dependency = new ModConfigEntryDependency("MimicInventoryCopyMode", "Custom");
                    return true;
                case "RandomizeMimicPossessionDuration":
                    dependency = new ModConfigEntryDependency("EnableMimicPossessionTuning");
                    return true;
                case "MimicPossessionMinTimeSeconds":
                case "MimicPossessionMaxTimeSeconds":
                    dependency = new ModConfigEntryDependency("RandomizeMimicPossessionDuration");
                    return true;
                case "MimicPossessionCooltimeMultiplier":
                case "PossessionRangeMeters":
                case "PossessionBtGateMode":
                    dependency = new ModConfigEntryDependency("EnableMimicPossessionTuning");
                    return true;
                case "EmoteRespondChancePercent":
                case "EmoteSuggestChancePercent":
                case "ReactToSprinklerChancePercent":
                case "UseTrapSwitchChancePercent":
                case "UseChargerChancePercent":
                case "UseTransmitterChancePercent":
                case "UseShutterSwitchChancePercent":
                    dependency = new ModConfigEntryDependency("MimicEmotePropsMode", "Custom");
                    return true;
                case "AllowHornImitation":
                case "HornMaxRecordSeconds":
                case "HornRecordingGapSeconds":
                case "HornMaxStoredRecords":
                    dependency = new ModConfigEntryDependency("HornImitationMode", "Custom");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetSpawnScalingDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            if (IsAutoScaleMultiplier(key))
            {
                dependency = new ModConfigEntryDependency(GetAutoScaleToggleKey(key));
                return true;
            }

            switch (key)
            {
                case "InitialPeriodicSpawnWaitSeconds":
                case "PeriodicSpawnIntervalSeconds":
                    dependency = new ModConfigEntryDependency("PeriodicSpawnWaitMode", "Fixed");
                    return true;
                case "InitialPeriodicSpawnWaitMinSeconds":
                case "InitialPeriodicSpawnWaitMaxSeconds":
                case "PeriodicSpawnIntervalMinSeconds":
                case "PeriodicSpawnIntervalMaxSeconds":
                    dependency = new ModConfigEntryDependency("PeriodicSpawnWaitMode", "Random");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetLootDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "LootAllowlist":
                    dependency = new ModConfigEntryDependency("LootItemFilterMode", "AllowlistOnly");
                    return true;
                case "LootBlocklist":
                    dependency = new ModConfigEntryDependency("LootItemFilterMode", "BlocklistOnly");
                    return true;
                default:
                    if (IsAutoScaleMultiplier(key))
                    {
                        dependency = new ModConfigEntryDependency(GetAutoScaleToggleKey(key));
                        return true;
                    }

                    return false;
            }
        }

        private static bool TryGetEconomyDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "ShopDiscountMinPercent":
                case "ShopDiscountMaxPercent":
                    dependency = new ModConfigEntryDependency("ShopDiscountChancePercent", ">0");
                    return true;
                default:
                    if (IsAutoScaleMultiplier(key))
                    {
                        dependency = new ModConfigEntryDependency(GetAutoScaleToggleKey(key));
                        return true;
                    }

                    return false;
            }
        }

        private static bool TryGetDungeonRandomizerDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "DungeonPickPoolMode":
                case "DungeonAllowlist":
                case "DungeonBlocklist":
                case "IgnoreDungeonExcludeList":
                    dependency = new ModConfigEntryDependency("RandomizeDungeonPick");
                    return true;
                case "DungeonSeedFlavor":
                    dependency = new ModConfigEntryDependency("EnableDungeonRandomizer");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetWeatherDependency(string key, out ModConfigEntryDependency dependency)
        {
            dependency = default;
            switch (key)
            {
                case "FixedWeatherPreset":
                    dependency = new ModConfigEntryDependency("WeatherMode", "Fixed");
                    return true;
                case "DisableRandomWeather":
                    dependency = new ModConfigEntryDependency("WeatherMode", "Vanilla");
                    return true;
                case "WeatherCyclePresets":
                case "WeatherCycleMinDelaySeconds":
                case "WeatherCycleMaxDelaySeconds":
                    dependency = new ModConfigEntryDependency("WeatherMode", "Cycle");
                    return true;
                case "EnableRealtimeTramClock":
                    dependency = new ModConfigEntryDependency("EnableWeather");
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAutoScaleMultiplier(string key) =>
            MultiplierToAutoScaleToggle.ContainsKey(key);

        private static string GetAutoScaleToggleKey(string multiplierKey) =>
            MultiplierToAutoScaleToggle[multiplierKey];

        private static readonly Dictionary<string, string> MultiplierToAutoScaleToggle =
            new(StringComparer.Ordinal)
            {
                ["MimicSpawnMultiplier"] = "AutoScaleMimicSpawnsByPlayerCount",
                ["BossSpawnMultiplier"] = "AutoScaleBossSpawnsByPlayerCount",
                ["JakoSpawnMultiplier"] = "AutoScaleJakoSpawnsByPlayerCount",
                ["SpecialSpawnMultiplier"] = "AutoScaleSpecialSpawnsByPlayerCount",
                ["TrapSpawnMultiplier"] = "AutoScaleTrapSpawnsByPlayerCount",
                ["OtherSpawnMultiplier"] = "AutoScaleOtherSpawnsByPlayerCount",
                ["MapLootMultiplier"] = "AutoScaleMapLootByPlayerCount",
                ["DropLootMultiplier"] = "AutoScaleDropLootByPlayerCount",
                ["StartupMoneyMultiplier"] = "AutoScaleStartupMoneyByPlayerCount",
                ["ScrapSellValueMultiplier"] = "AutoScaleScrapSellValueByPlayerCount",
                ["ShopBuyPriceMultiplier"] = "AutoScaleShopBuyPriceByPlayerCount",
                ["ReinforcePriceMultiplier"] = "AutoScaleReinforcePriceByPlayerCount",
            };
    }
}
