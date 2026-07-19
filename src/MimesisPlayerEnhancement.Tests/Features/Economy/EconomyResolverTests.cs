using MimesisPlayerEnhancement.Features.Economy;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class EconomyResolverTests
    {
        private static EconomySceneConfig Config(
            bool enabled = true,
            float scaleRate = ScalingMath.DefaultPlayerCountScaleRate,
            bool autoScaleStartup = true,
            float startupMultiplier = 1f,
            bool autoScaleScrap = true,
            float scrapMultiplier = 1f,
            bool autoScaleShop = true,
            float shopMultiplier = 1f,
            int shopDiscountMin = 0,
            int shopDiscountMax = 100,
            int shopDiscountChance = 0,
            bool autoScaleReinforce = true,
            float reinforceMultiplier = 1f,
            bool retainCurrency = false) =>
            new(
                enabled,
                scaleRate,
                autoScaleStartup,
                startupMultiplier,
                autoScaleScrap,
                scrapMultiplier,
                autoScaleShop,
                shopMultiplier,
                shopDiscountMin,
                shopDiscountMax,
                shopDiscountChance,
                autoScaleReinforce,
                reinforceMultiplier,
                retainCurrency);

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void GetEffectiveMultiplier_returns_neutral_when_feature_disabled(int moneyTypeValue)
        {
            var type = (MoneyType)moneyTypeValue;
            EconomySceneConfig config = Config(enabled: false, startupMultiplier: 2f, scrapMultiplier: 2f);

            float multiplier = EconomyResolver.GetEffectiveMultiplier(type, playerCount: 8, config);

            Assert.Equal(FeatureToggleGate.NeutralMultiplier, multiplier);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        public void IsAutoScaleEnabled_reflects_config_flags(int moneyTypeValue, bool expected)
        {
            var type = (MoneyType)moneyTypeValue;
            EconomySceneConfig config = Config(
                autoScaleStartup: true,
                autoScaleScrap: true,
                autoScaleShop: true,
                autoScaleReinforce: true);

            Assert.Equal(expected, EconomyResolver.IsAutoScaleEnabled(type, config));
        }

        [Theory]
        [InlineData(0, 1.5f)]
        [InlineData(1, 2f)]
        [InlineData(2, 0.5f)]
        [InlineData(3, 3f)]
        public void GetPerTypeMultiplier_returns_configured_value(int moneyTypeValue, float configured)
        {
            var type = (MoneyType)moneyTypeValue;
            EconomySceneConfig config = Config(
                startupMultiplier: 1.5f,
                scrapMultiplier: 2f,
                shopMultiplier: 0.5f,
                reinforceMultiplier: 3f);

            float multiplier = EconomyResolver.GetPerTypeMultiplier(type, config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(4, 1f)]
        [InlineData(5, 1.1f)]
        [InlineData(8, 1.4f)]
        public void GetPlayerScale_uses_vanilla_baseline_and_scale_rate(int playerCount, float expectedScale)
        {
            EconomySceneConfig config = Config(scaleRate: 0.10f, autoScaleStartup: true);

            float scale = EconomyResolver.GetPlayerScale(MoneyType.Startup, playerCount, config);

            Assert.Equal(expectedScale, scale);
        }

        [Fact]
        public void GetPlayerScale_returns_one_when_auto_scale_disabled()
        {
            EconomySceneConfig config = Config(autoScaleStartup: false, scaleRate: 0.50f);

            float scale = EconomyResolver.GetPlayerScale(MoneyType.Startup, playerCount: 8, config);

            Assert.Equal(1f, scale);
        }

        [Theory]
        [InlineData(8, 2f, 2.8f)]
        [InlineData(4, 1.5f, 1.5f)]
        public void GetEffectiveMultiplier_combines_per_type_and_player_scale(
            int playerCount,
            float startupMultiplier,
            float expected)
        {
            EconomySceneConfig config = Config(
                startupMultiplier: startupMultiplier,
                autoScaleStartup: true,
                scaleRate: 0.10f);

            float multiplier = EconomyResolver.GetEffectiveMultiplier(MoneyType.Startup, playerCount, config);

            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(100, 1.5f, 150)]
        [InlineData(0, 2f, 0)]
        [InlineData(10, 0f, 0)]
        [InlineData(7, 1.2f, 8)]
        public void ScaleAmount_rounds_like_scaling_math(int vanilla, float multiplier, int expected)
        {
            int scaled = EconomyResolver.ScaleAmount(vanilla, multiplier);

            Assert.Equal(expected, scaled);
        }
    }
}
