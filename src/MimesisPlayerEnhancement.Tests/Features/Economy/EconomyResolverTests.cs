using MimesisPlayerEnhancement.Features.Economy;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Economy
{
    public sealed class EconomyResolverTests
    {
        private static EconomySceneConfig Config(
            bool enabled = true,
            int baseline = ScalingMath.VanillaPlayerBaseline,
            float scrapMultiplier = 1f,
            float scrapPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            float shopMultiplier = 1f,
            float shopPerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            int shopDiscountMin = 0,
            int shopDiscountMax = 100,
            int shopDiscountChance = 0,
            float reinforceMultiplier = 1f,
            float reinforcePerPlayer = ScalingMath.DefaultPerPlayerMultiplier,
            bool retainCurrency = false) =>
            new(
                enabled,
                baseline,
                scrapMultiplier,
                scrapPerPlayer,
                shopMultiplier,
                shopPerPlayer,
                shopDiscountMin,
                shopDiscountMax,
                shopDiscountChance,
                reinforceMultiplier,
                reinforcePerPlayer,
                retainCurrency);

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void GetEffectiveMultiplier_returns_neutral_when_feature_disabled(int moneyTypeValue)
        {
            var type = (MoneyType)moneyTypeValue;
            EconomySceneConfig config = Config(enabled: false, scrapMultiplier: 2f);

            float multiplier = EconomyResolver.GetEffectiveMultiplier(type, playerCount: 8, config);

            Assert.Equal(FeatureToggleGate.NeutralMultiplier, multiplier);
        }

        [Theory]
        [InlineData(0, 2f)]
        [InlineData(1, 0.5f)]
        [InlineData(2, 3f)]
        public void GetPerTypeMultiplier_returns_configured_value(int moneyTypeValue, float configured)
        {
            var type = (MoneyType)moneyTypeValue;
            EconomySceneConfig config = Config(
                scrapMultiplier: 2f,
                shopMultiplier: 0.5f,
                reinforceMultiplier: 3f);

            float multiplier = EconomyResolver.GetPerTypeMultiplier(type, config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(0, 0.15f)]
        [InlineData(1, 0.20f)]
        [InlineData(2, 0.08f)]
        public void GetPerPlayerMultiplier_returns_configured_value(int moneyTypeValue, float configured)
        {
            var type = (MoneyType)moneyTypeValue;
            EconomySceneConfig config = Config(
                scrapPerPlayer: 0.15f,
                shopPerPlayer: 0.20f,
                reinforcePerPlayer: 0.08f);

            float multiplier = EconomyResolver.GetPerPlayerMultiplier(type, config);

            Assert.Equal(configured, multiplier);
        }

        [Theory]
        [InlineData(4, 1f)]
        [InlineData(5, 1.1f)]
        [InlineData(8, 1.4f)]
        public void GetEffectiveMultiplier_uses_additive_scaling_at_default_baseline(int playerCount, float expectedScale)
        {
            EconomySceneConfig config = Config(scrapMultiplier: 1f, scrapPerPlayer: 0.10f);

            float scale = EconomyResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount, config);

            Assert.Equal(expectedScale, scale);
        }

        [Fact]
        public void GetEffectiveMultiplier_returns_general_when_per_player_is_zero()
        {
            EconomySceneConfig config = Config(scrapMultiplier: 1.5f, scrapPerPlayer: 0f);

            float multiplier = EconomyResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount: 8, config);

            Assert.Equal(1.5f, multiplier);
        }

        [Theory]
        [InlineData(8, 2f, 2.4f)]
        [InlineData(4, 1.5f, 1.5f)]
        public void GetEffectiveMultiplier_combines_general_and_per_player_additive(
            int playerCount,
            float scrapMultiplier,
            float expected)
        {
            EconomySceneConfig config = Config(
                scrapMultiplier: scrapMultiplier,
                scrapPerPlayer: 0.10f);

            float multiplier = EconomyResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount, config);

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
