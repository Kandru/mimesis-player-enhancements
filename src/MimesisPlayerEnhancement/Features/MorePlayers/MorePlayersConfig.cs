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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MorePlayers");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMorePlayers = ModConfig.CreateTrackedEntry(_category,
                "EnableMorePlayers",
                false);

            ModConfig.MaxPlayers = ModConfig.CreateTrackedEntry(_category,
                "MaxPlayers",
                32);

            ModConfig.EnableScalingRoundGoals = ModConfig.CreateTrackedEntry(_category,
                "EnableScalingRoundGoals",
                true);

            ModConfig.RoundGoalBasePerZone = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalBasePerZone",
                RoundGoalScalingResolver.DefaultBasePerZone);

            ModConfig.RoundGoalMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalMoneyMultiplier",
                1f);

            ModConfig.RoundGoalRandomSpreadPercent = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalRandomSpreadPercent",
                RoundGoalScalingResolver.DefaultRandomSpreadPercent);

            ModConfig.RoundGoalCurveExponent = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalCurveExponent",
                RoundGoalScalingResolver.DefaultCurveExponent);
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
