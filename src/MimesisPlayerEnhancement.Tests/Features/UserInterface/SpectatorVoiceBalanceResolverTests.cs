using MimesisPlayerEnhancement.Features.UserInterface.SpectatorVoiceBalance;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class SpectatorVoiceBalanceResolverTests
    {
        [Theory]
        [InlineData("Vanilla")]
        [InlineData("SpeechDucking")]
        [InlineData("StaticAttenuation")]
        [InlineData("speechducking")]
        public void TryParseMode_accepts_valid_modes(string raw)
        {
            Assert.True(SpectatorVoiceBalanceResolver.TryParseMode(raw, out SpectatorVoiceBalanceMode mode));
            Assert.Equal(
                Enum.Parse<SpectatorVoiceBalanceMode>(raw, ignoreCase: true),
                mode);
        }

        [Fact]
        public void TryParseMode_rejects_unknown_mode()
        {
            Assert.False(SpectatorVoiceBalanceResolver.TryParseMode("Broken", out SpectatorVoiceBalanceMode mode));
            Assert.Equal(SpectatorVoiceBalanceMode.Vanilla, mode);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, true, true)]
        public void ClassifyGroup_respects_invert(bool remoteIsDead, bool invert, bool expectedIsPriority)
        {
            SpectatorVoiceGroup group = SpectatorVoiceBalanceResolver.ClassifyGroup(remoteIsDead, invert);

            Assert.Equal(
                expectedIsPriority ? SpectatorVoiceGroup.Priority : SpectatorVoiceGroup.Other,
                group);
        }

        [Fact]
        public void ResolveTargetMultiplier_priority_group_is_always_full()
        {
            float multiplier = SpectatorVoiceBalanceResolver.ResolveTargetMultiplier(
                SpectatorVoiceBalanceMode.StaticAttenuation,
                SpectatorVoiceGroup.Priority,
                priorityGroupSpeakingContinuously: true,
                attenuation: 0.8f,
                duckLevel: 0.2f);

            Assert.Equal(1f, multiplier);
        }

        [Fact]
        public void ResolveTargetMultiplier_static_attenuation_uses_fraction()
        {
            float multiplier = SpectatorVoiceBalanceResolver.ResolveTargetMultiplier(
                SpectatorVoiceBalanceMode.StaticAttenuation,
                SpectatorVoiceGroup.Other,
                priorityGroupSpeakingContinuously: false,
                attenuation: 0.8f,
                duckLevel: 0.2f);

            Assert.Equal(0.8f, multiplier);
        }

        [Theory]
        [InlineData(false, 1f)]
        [InlineData(true, 0.2f)]
        public void ResolveTargetMultiplier_speech_ducking_depends_on_priority_speech(
            bool prioritySpeaking,
            float expected)
        {
            float multiplier = SpectatorVoiceBalanceResolver.ResolveTargetMultiplier(
                SpectatorVoiceBalanceMode.SpeechDucking,
                SpectatorVoiceGroup.Other,
                prioritySpeaking,
                attenuation: 0.8f,
                duckLevel: 0.2f);

            Assert.Equal(expected, multiplier);
        }
    }
}
