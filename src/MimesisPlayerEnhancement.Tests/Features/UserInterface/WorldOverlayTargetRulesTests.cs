using MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays;
using ReluProtocol.Enum;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class WorldOverlayTargetRulesTests
    {
        [Theory]
        [InlineData(false, false, true, ActorType.Player, true)]
        [InlineData(false, false, false, ActorType.Monster, true)]
        [InlineData(true, false, true, ActorType.Player, false)]
        [InlineData(false, true, true, ActorType.Player, false)]
        [InlineData(false, false, false, ActorType.NPC, false)]
        public void IsEligibleWorldDamageTarget_filters_dead_avatar_and_non_targets(
            bool dead,
            bool isAvatar,
            bool isPlayer,
            ActorType actorType,
            bool expected)
        {
            Assert.Equal(
                expected,
                WorldOverlayTargetRules.IsEligibleWorldDamageTarget(
                    dead,
                    isAvatar,
                    isPlayer,
                    actorType));
        }
    }
}
