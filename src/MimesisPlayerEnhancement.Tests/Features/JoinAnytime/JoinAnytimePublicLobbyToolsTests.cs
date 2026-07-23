using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimePublicLobbyToolsTests
    {
        [Fact]
        public void CoercePublicRoomWriteFlag_transpiler_target_resolves_two_arg_overload()
        {
            MethodInfo method = AccessTools.Method(
                typeof(JoinAnytimePublicLobbyTools),
                nameof(JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag),
                [typeof(bool), typeof(bool)]);

            Assert.NotNull(method);
            Assert.Equal(2, method.GetParameters().Length);
        }

        [Theory]
        [InlineData(false, false, true, true)]
        [InlineData(true, false, false, false)]
        public void CoercePublicRoomWriteFlag_passthrough_when_feature_disabled(
            bool hostWantsPublic,
            bool isPublicRequested,
            bool toggleOrFallbackFlag,
            bool expected)
        {
            bool actual = JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag(
                featureEnabled: false,
                hostWantsPublic,
                isPublicRequested,
                toggleOrFallbackFlag);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(true, false, false, true)]
        [InlineData(false, true, false, true)]
        [InlineData(true, true, false, true)]
        public void CoercePublicRoomWriteFlag_forces_true_when_public_requested_or_host_wants_public(
            bool hostWantsPublic,
            bool isPublicRequested,
            bool toggleOrFallbackFlag,
            bool expected)
        {
            bool actual = JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag(
                featureEnabled: true,
                hostWantsPublic,
                isPublicRequested,
                toggleOrFallbackFlag);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(false, false, true, true)]
        [InlineData(false, false, false, false)]
        public void CoercePublicRoomWriteFlag_passthrough_when_feature_enabled_without_public_intent(
            bool hostWantsPublic,
            bool isPublicRequested,
            bool toggleOrFallbackFlag,
            bool expected)
        {
            bool actual = JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag(
                featureEnabled: true,
                hostWantsPublic,
                isPublicRequested,
                toggleOrFallbackFlag);

            Assert.Equal(expected, actual);
        }
    }
}
