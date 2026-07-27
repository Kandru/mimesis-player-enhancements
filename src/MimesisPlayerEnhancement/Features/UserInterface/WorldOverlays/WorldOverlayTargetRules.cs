using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayTargetRules
    {
        internal static bool IsEligibleWorldDamageTarget(
            bool dead,
            bool isAvatar,
            bool isPlayer,
            ActorType actorType)
        {
            if (dead || isAvatar)
            {
                return false;
            }

            return isPlayer || actorType == ActorType.Monster;
        }
    }
}
