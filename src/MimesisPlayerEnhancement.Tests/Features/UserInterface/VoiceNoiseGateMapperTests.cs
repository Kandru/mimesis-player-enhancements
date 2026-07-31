using MimesisPlayerEnhancement.Features.UserInterface.VoiceNoiseGate;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class VoiceNoiseGateMapperTests
    {
        [Theory]
        [InlineData(0f, "MediumSensitivity", "High")]
        [InlineData(0.49f, "MediumSensitivity", "High")]
        [InlineData(0.5f, "LowSensitivity", "VeryHigh")]
        [InlineData(1f, "LowSensitivity", "VeryHigh")]
        public void MapStrength_maps_expected_targets(float strength, string expectedVad, string expectedDenoise)
        {
            VoiceNoiseGateTargets targets = VoiceNoiseGateMapper.MapStrength(strength);

            Assert.Equal(expectedVad, targets.VadSensitivityLevelName);
            Assert.Equal(expectedDenoise, targets.DenoiseLevelName);
        }

        [Theory]
        [InlineData("VoiceActivation", true)]
        [InlineData("Open", true)]
        [InlineData("PushToTalk", false)]
        [InlineData("None", false)]
        [InlineData(null, false)]
        public void IsVoiceActivationTalkMode_matches_expected_modes(string? modeName, bool expected)
        {
            Assert.Equal(expected, VoiceNoiseGateMapper.IsVoiceActivationTalkMode(modeName));
        }
    }
}
