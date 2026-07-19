using MimesisPlayerEnhancement.Features.PlayerTuning;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerTuning
{
    public sealed class PlayerTuningResolverTests
    {
        private static PlayerTuningConfigSnapshot Config(
            bool enabled = true,
            float moveSpeedMultiplier = 1f,
            float noClipSpeedMultiplier = 3f,
            float maxStaminaMultiplier = 1f,
            float staminaDrainMultiplier = 1f,
            float staminaRegenMultiplier = 1f,
            float staminaRegenDelayMultiplier = 1f,
            float maxCarryWeightMultiplier = 1f,
            bool disablePlayerCollision = true) =>
            new(
                enabled,
                moveSpeedMultiplier,
                noClipSpeedMultiplier,
                maxStaminaMultiplier,
                staminaDrainMultiplier,
                staminaRegenMultiplier,
                staminaRegenDelayMultiplier,
                maxCarryWeightMultiplier,
                disablePlayerCollision);

        [Theory]
        [InlineData(2f, 1f)]
        [InlineData(0.5f, 1f)]
        [InlineData(5f, 1f)]
        public void GetMoveSpeedMultiplier_returns_neutral_when_feature_disabled(
            float configured,
            float expected)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: false, moveSpeedMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetMoveSpeedMultiplier(config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(0.5f)]
        [InlineData(5f)]
        public void GetMoveSpeedMultiplier_passes_through_when_feature_enabled(float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: true, moveSpeedMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetMoveSpeedMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(false, 4f)]
        [InlineData(true, 4f)]
        public void GetNoClipSpeedMultiplier_is_not_feature_gated(bool enabled, float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: enabled, noClipSpeedMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetNoClipSpeedMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(2f, 1f)]
        [InlineData(0.5f, 1f)]
        public void GetMaxStaminaMultiplier_returns_neutral_when_feature_disabled(float configured, float expected)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: false, maxStaminaMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetMaxStaminaMultiplier(config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(0.5f)]
        public void GetMaxStaminaMultiplier_passes_through_when_feature_enabled(float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: true, maxStaminaMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetMaxStaminaMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(2f, 1f)]
        [InlineData(0.5f, 1f)]
        public void GetStaminaDrainMultiplier_returns_neutral_when_feature_disabled(float configured, float expected)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: false, staminaDrainMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetStaminaDrainMultiplier(config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(0.5f)]
        public void GetStaminaDrainMultiplier_passes_through_when_feature_enabled(float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: true, staminaDrainMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetStaminaDrainMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(2f, 1f)]
        [InlineData(0.5f, 1f)]
        public void GetStaminaRegenMultiplier_returns_neutral_when_feature_disabled(float configured, float expected)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: false, staminaRegenMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetStaminaRegenMultiplier(config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(0.5f)]
        public void GetStaminaRegenMultiplier_passes_through_when_feature_enabled(float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: true, staminaRegenMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetStaminaRegenMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(2f, 1f)]
        [InlineData(0.5f, 1f)]
        public void GetStaminaRegenDelayMultiplier_returns_neutral_when_feature_disabled(float configured, float expected)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: false, staminaRegenDelayMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetStaminaRegenDelayMultiplier(config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(0.5f)]
        public void GetStaminaRegenDelayMultiplier_passes_through_when_feature_enabled(float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: true, staminaRegenDelayMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetStaminaRegenDelayMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(2f, 1f)]
        [InlineData(0.5f, 1f)]
        public void GetMaxCarryWeightMultiplier_returns_neutral_when_feature_disabled(float configured, float expected)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: false, maxCarryWeightMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetMaxCarryWeightMultiplier(config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(2f)]
        [InlineData(0.5f)]
        public void GetMaxCarryWeightMultiplier_passes_through_when_feature_enabled(float configured)
        {
            PlayerTuningConfigSnapshot config = Config(enabled: true, maxCarryWeightMultiplier: configured);

            float multiplier = PlayerTuningResolver.GetMaxCarryWeightMultiplier(config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void GetDisablePlayerCollision_requires_both_flags(
            bool enabled,
            bool disableCollision,
            bool expected)
        {
            PlayerTuningConfigSnapshot config = Config(
                enabled: enabled,
                disablePlayerCollision: disableCollision);

            bool result = PlayerTuningResolver.GetDisablePlayerCollision(config);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void MinMultiplier_matches_config_bounds()
        {
            Assert.Equal(0.1f, PlayerTuningResolver.MinMultiplier);
        }

        [Fact]
        public void MaxMultiplier_matches_config_bounds()
        {
            Assert.Equal(5f, PlayerTuningResolver.MaxMultiplier);
        }
    }
}
