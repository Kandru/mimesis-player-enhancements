using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_MorePlayers] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class MorePlayersConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MorePlayers", "More Players");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMorePlayers = ModConfig.CreateTrackedEntry(_category,
                "EnableMorePlayers",
                false,
                "Enable More Players",
                "Raise the multiplayer player cap above 4.");

            ModConfig.MaxPlayers = ModConfig.CreateTrackedEntry(_category,
                "MaxPlayers",
                32,
                "Max Players",
                "Maximum players in a session including the host (1 = solo, 2 = host + 1 client, etc.).");

            ModConfig.EnableScalingRoundGoals = ModConfig.CreateTrackedEntry(_category,
                "EnableScalingRoundGoals",
                true,
                "Enable Scaling Round Goals",
                "Scale tram repair quota by zone instead of capping at vanilla stage 5. Requires More Players. Host only.");

            ModConfig.RoundGoalBasePerZone = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalBasePerZone",
                RoundGoalScalingResolver.DefaultBasePerZone,
                "Round Goal Base Per Zone",
                "Base dollars multiplied by the zone curve (zone 1 at defaults ≈ $200 before spread and multiplier).");

            ModConfig.RoundGoalMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalMoneyMultiplier",
                1f,
                "Round Goal Money Multiplier",
                "Global multiplier on the computed tram repair quota (1 = formula only, 2 = double).");

            ModConfig.RoundGoalRandomSpreadPercent = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalRandomSpreadPercent",
                RoundGoalScalingResolver.DefaultRandomSpreadPercent,
                "Round Goal Random Spread (percent)",
                "Random ±% band around the computed center quota when departing maintenance (save load uses the low bound).");

            ModConfig.RoundGoalCurveExponent = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalCurveExponent",
                RoundGoalScalingResolver.DefaultCurveExponent,
                "Round Goal Curve Exponent",
                "Zone growth curve: 1 = linear, below 1 = flatter late-game growth, above 1 = steeper.");
        }

        /// <summary>Clamps persisted values once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            if (ModConfig.MaxPlayers.Value < 1)
            {
                logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                ModConfig.MaxPlayers.Value = 1;
            }

            OnRoundGoalRandomSpreadPercentChanged(logger, ModConfig.RoundGoalRandomSpreadPercent.Value, ModConfig.RoundGoalRandomSpreadPercent);
            OnRoundGoalCurveExponentChanged(logger, ModConfig.RoundGoalCurveExponent.Value, ModConfig.RoundGoalCurveExponent);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.MaxPlayers.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                    ModConfig.MaxPlayers.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.MaxPlayers);
            });

            ModConfig.EnableMorePlayers.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableMorePlayers));
            ModConfig.EnableScalingRoundGoals.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableScalingRoundGoals));
            ModConfig.RoundGoalBasePerZone.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.RoundGoalBasePerZone));
            ModConfig.RoundGoalMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.RoundGoalMoneyMultiplier));
            ModConfig.RoundGoalRandomSpreadPercent.OnEntryValueChanged.Subscribe((_, value) =>
                OnRoundGoalRandomSpreadPercentChanged(logger, value, ModConfig.RoundGoalRandomSpreadPercent));
            ModConfig.RoundGoalCurveExponent.OnEntryValueChanged.Subscribe((_, value) =>
                OnRoundGoalCurveExponentChanged(logger, value, ModConfig.RoundGoalCurveExponent));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.RoundGoalBasePerZone);
            ModConfig.TrackFloatEntry(ModConfig.RoundGoalMoneyMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.RoundGoalCurveExponent);
        }

        private static void OnRoundGoalRandomSpreadPercentChanged(
            MelonLogger.Instance logger,
            int value,
            MelonPreferences_Entry<int> entry)
        {
            if (value < 0)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0;
                return;
            }

            if (value > 100)
            {
                logger.Warning($"{entry.Identifier} must be <= 100; resetting to 100.");
                entry.Value = 100;
                return;
            }

            ModConfig.NotifyChanged(entry);
        }

        private static void OnRoundGoalCurveExponentChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < RoundGoalScalingResolver.MinCurveExponent)
            {
                logger.Warning($"{entry.Identifier} must be >= {RoundGoalScalingResolver.MinCurveExponent}; resetting.");
                entry.Value = RoundGoalScalingResolver.MinCurveExponent;
                return;
            }

            if (value > RoundGoalScalingResolver.MaxCurveExponent)
            {
                logger.Warning($"{entry.Identifier} must be <= {RoundGoalScalingResolver.MaxCurveExponent}; resetting.");
                entry.Value = RoundGoalScalingResolver.MaxCurveExponent;
                return;
            }

            ModConfig.NotifyChanged(entry);
        }
    }
}
