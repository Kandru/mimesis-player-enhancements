using System.Globalization;
using MimesisPlayerEnhancement;
using MimesisPlayerEnhancement.Features.PlayerTuning;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerTuning
{
    public sealed class PlayerTuningConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_PlayerTuning";

        [Theory]
        [InlineData("MoveSpeedMultiplier")]
        [InlineData("NoClipSpeedMultiplier")]
        [InlineData("MaxStaminaMultiplier")]
        [InlineData("StaminaDrainMultiplier")]
        [InlineData("StaminaRegenMultiplier")]
        [InlineData("StaminaRegenDelayMultiplier")]
        [InlineData("MaxCarryWeightMultiplier")]
        public void Float_multipliers_are_clamped_to_player_tuning_range(string key)
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, key, out ModConfigEntryBound bound));
            Assert.Equal(
                PlayerTuningResolver.MinMultiplier,
                float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Equal(
                PlayerTuningResolver.MaxMultiplier,
                float.Parse(bound.MaxValue!, CultureInfo.InvariantCulture));
        }
    }
}
